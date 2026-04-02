using FundRecommendationAPI.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;

namespace FundRecommendationAPI.Services
{
    public class SystemService : ISystemService
    {
        private readonly IRepository<SystemUpdateHistory> _updateHistoryRepository;
        private readonly IRepository<FundBasicInfo> _fundRepository;
        private readonly IRepository<FundNavHistory> _navHistoryRepository;
        private readonly IRepository<FundPerformance> _performanceRepository;
        private readonly IRepository<FundManager> _managerRepository;
        private readonly IFundDataService _fundDataService;
        private readonly ILogger<SystemService> _logger;

        public SystemService(
            IRepository<SystemUpdateHistory> updateHistoryRepository,
            IRepository<FundBasicInfo> fundRepository,
            IRepository<FundNavHistory> navHistoryRepository,
            IRepository<FundPerformance> performanceRepository,
            IRepository<FundManager> managerRepository,
            IFundDataService fundDataService,
            ILogger<SystemService> logger)
        {
            _updateHistoryRepository = updateHistoryRepository;
            _fundRepository = fundRepository;
            _navHistoryRepository = navHistoryRepository;
            _performanceRepository = performanceRepository;
            _managerRepository = managerRepository;
            _fundDataService = fundDataService;
            _logger = logger;
        }

        public async Task<object> GetSystemStatusAsync()
        {
            var lastUpdate = await _updateHistoryRepository.Query()
                .OrderByDescending(h => h.CreatedAt)
                .FirstOrDefaultAsync();

            var totalFunds = await _fundRepository.CountAsync();
            var totalManagers = await _managerRepository.CountAsync();

            return new
            {
                status = "running",
                timestamp = DateTime.Now,
                version = "1.0.0",
                lastUpdate = lastUpdate != null ? new
                {
                    id = lastUpdate.Id,
                    type = lastUpdate.Type,
                    startTime = lastUpdate.StartTime,
                    endTime = lastUpdate.EndTime,
                    status = lastUpdate.Status,
                    recordsUpdated = lastUpdate.RecordsUpdated
                } : null,
                statistics = new
                {
                    totalFunds,
                    totalManagers
                }
            };
        }

        public async Task<object> TriggerDataUpdateAsync(string updateType = "full")
        {
            var updateId = $"update_{DateTime.Now:yyyyMMddHHmmss}";
            
            var updateHistory = new SystemUpdateHistory
            {
                Id = updateId,
                Type = updateType,
                StartTime = DateTime.Now,
                Status = "running",
                RecordsUpdated = 0
            };

            await _updateHistoryRepository.AddAsync(updateHistory);

            _logger.LogInformation($"数据更新已触发: {updateId}, 类型: {updateType}");

            try
            {
                await Task.Run(async () =>
                {
                    await RealDataUpdate(updateId, updateType);
                });

                return new
                {
                    success = true,
                    message = "数据更新已触发",
                    updateId,
                    timestamp = DateTime.Now
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"数据更新触发失败: {updateId}");
                
                var updatedHistory = await _updateHistoryRepository.GetByIdAsync(updateId);
                if (updatedHistory != null)
                {
                    updatedHistory.Status = "failed";
                    updatedHistory.ErrorMessage = ex.Message;
                    await _updateHistoryRepository.UpdateAsync(updatedHistory);
                }

                throw;
            }
        }

        public async Task<object> GetUpdateHistoryAsync(int limit = 10)
        {
            var history = await _updateHistoryRepository.Query()
                .OrderByDescending(h => h.CreatedAt)
                .Take(limit)
                .Select(h => new
                {
                    h.Id,
                    h.Type,
                    h.StartTime,
                    h.EndTime,
                    h.Status,
                    h.RecordsUpdated,
                    h.ErrorMessage
                })
                .ToListAsync();

            return new
            {
                total = history.Count,
                limit,
                history
            };
        }

        private async Task<string[]> GetFundListFromAkshare()
        {
            try
            {
                var pythonScriptPath = Path.Combine(Directory.GetCurrentDirectory(), "Services", "fund_data_collector.py");
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"{pythonScriptPath} get_fund_list",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (var process = Process.Start(processStartInfo))
                {
                    if (process == null)
                    {
                        _logger.LogError("Failed to start Python process for getting fund list");
                        return new string[] { "000001", "000002", "000003", "000004", "000005" };
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (process.ExitCode != 0)
                    {
                        _logger.LogError($"Python script failed with error: {error}");
                        return new string[] { "000001", "000002", "000003", "000004", "000005" };
                    }

                    var fundCodes = JsonSerializer.Deserialize<string[]>(output);
                    if (fundCodes == null || fundCodes.Length == 0)
                    {
                        _logger.LogWarning("No fund codes returned from Akshare, using default list");
                        return new string[] { "000001", "000002", "000003", "000004", "000005" };
                    }

                    _logger.LogInformation($"Successfully got {fundCodes.Length} fund codes from Akshare");
                    return fundCodes;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting fund list from Akshare");
                return new string[] { "000001", "000002", "000003", "000004", "000005" };
            }
        }

        private async Task RealDataUpdate(string updateId, string updateType = "full", CancellationToken cancellationToken = default)
        {
            try
            {
                var updateHistory = await _updateHistoryRepository.GetByIdAsync(updateId);
                if (updateHistory == null) return;

                var recordsUpdated = 0;

                // 从Akshare获取最新的基金列表
                var fundCodes = await GetFundListFromAkshare();
                
                // 不限制基金数量，处理所有基金代码
                // if (fundCodes.Length > 20) // 最多更新20个基金
                // {
                //     fundCodes = fundCodes.Take(20).ToArray();
                //     _logger.LogInformation($"限制更新基金数量为 {fundCodes.Length} 个");
                // }
                
                _logger.LogInformation($"准备更新 {fundCodes.Length} 个基金的数据");
                // 并行处理多个基金的数据更新
                var updateTasks = fundCodes.Select(async fundCode =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    try
                    {
                        // 更新基金基本信息
                        await _fundDataService.UpdateFundBasicInfo(fundCode);

                        // 根据更新类型确定时间范围
                        var endDate = DateTime.Now.ToString("yyyy-MM-dd");
                        var startDate = updateType == "incremental"
                            ? DateTime.Now.AddDays(-7).ToString("yyyy-MM-dd")
                            : DateTime.Now.AddYears(-10).ToString("yyyy-MM-dd");

                        // 更新基金净值历史（只插入新记录）
                        await _fundDataService.UpdateFundNavHistory(fundCode, startDate, endDate);

                        // 更新基金经理数据（只插入新记录）
                        await _fundDataService.UpdateFundManagers(fundCode);

                        // 更新基金资产规模数据（只插入新记录）
                        await _fundDataService.UpdateFundAssetScale(fundCode, startDate, endDate);

                        // 更新基金申购状态数据（只插入新记录）
                        await _fundDataService.UpdateFundPurchaseStatus(fundCode);

                        // 更新基金赎回状态数据（只插入新记录）
                        await _fundDataService.UpdateFundRedemptionStatus(fundCode);

                        // 更新基金公司行为数据（只插入新记录）
                        await _fundDataService.UpdateFundCorporateActions(fundCode);

                        // 检查是否有新的公司行为记录
                        var hasNewCorporateActions = await _fundDataService.HasNewCorporateActionsAsync(fundCode, startDate);

                        // 根据是否有新的公司行为记录来决定如何更新业绩数据
                        if (hasNewCorporateActions)
                        {
                            // 有新的公司行为记录时，更新所有业绩数据
                            await _fundDataService.UpdateFundPerformance(fundCode);
                        }
                        else
                        {
                            // 没有新的公司行为记录时，只更新周期类型为短期（如成立以来/今年以来/近1周等）的记录
                            await _fundDataService.UpdateRecentPerformanceAsync(fundCode);
                        }

                        // 避免请求过于频繁，添加适当延迟
                        await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);

                        return 1; // 只在成功时返回1
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"更新基金 {fundCode} 数据失败");
                        return 0;
                    }
                });

                // 等待所有基金更新完成
                var results = await Task.WhenAll(updateTasks);
                recordsUpdated = results.Sum();

                updateHistory.EndTime = DateTime.Now;
                updateHistory.Status = "completed";
                updateHistory.RecordsUpdated = recordsUpdated;

                await _updateHistoryRepository.UpdateAsync(updateHistory);

                _logger.LogInformation($"数据更新完成: {updateId}, 更新记录数: {recordsUpdated}, 更新类型: {updateType}");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation($"数据更新已取消: {updateId}");
                
                var updateHistory = await _updateHistoryRepository.GetByIdAsync(updateId);
                if (updateHistory != null)
                {
                    updateHistory.EndTime = DateTime.Now;
                    updateHistory.Status = "cancelled";
                    await _updateHistoryRepository.UpdateAsync(updateHistory);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"数据更新执行失败: {updateId}");
                
                var updateHistory = await _updateHistoryRepository.GetByIdAsync(updateId);
                if (updateHistory != null)
                {
                    updateHistory.EndTime = DateTime.Now;
                    updateHistory.Status = "failed";
                    updateHistory.ErrorMessage = ex.Message;
                    await _updateHistoryRepository.UpdateAsync(updateHistory);
                }
            }
        }

        public async Task RealDataUpdateAsync(string updateType = "full", CancellationToken cancellationToken = default)
        {
            var updateId = $"update_{DateTime.Now:yyyyMMddHHmmss}";
            
            var updateHistory = new SystemUpdateHistory
            {
                Id = updateId,
                Type = updateType,
                StartTime = DateTime.Now,
                Status = "running",
                RecordsUpdated = 0
            };

            await _updateHistoryRepository.AddAsync(updateHistory);

            _logger.LogInformation($"数据更新已触发: {updateId}, 类型: {updateType}");

            await RealDataUpdate(updateId, updateType, cancellationToken);
        }
    }
}
