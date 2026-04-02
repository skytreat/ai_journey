using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Swashbuckle.AspNetCore.SwaggerUI;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace FundRecommendationAPI.Extensions
{
    public static class RouteConfigurator
    {
        private static readonly Regex FundCodeRegex = new(@"^\d{6}$", RegexOptions.Compiled);
        private static readonly string[] ValidFundTypes = { "股票型", "债券型", "混合型", "货币型", "指数型", "QDII", "FOF", "商品型", "其他" };
        private static readonly string[] ValidPeriods = { "week", "month", "quarter", "year" };

        public static WebApplication ConfigureRoutes(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseRateLimiter();

            var api = app.MapGroup("/api").RequireRateLimiting("api");

            ConfigureFundRoutes(api);
            ConfigureAnalysisRoutes(api);
            ConfigureFavoriteRoutes(api);
            ConfigureFavoriteScoreRoutes(api);
            ConfigureQueryRoutes(api);
            ConfigureMetaRoutes(api);
            ConfigureSystemRoutes(api);

            return app;
        }

        private static bool IsValidFundCode(string code)
        {
            return !string.IsNullOrWhiteSpace(code) && FundCodeRegex.IsMatch(code);
        }

        private static bool IsValidFundType(string? fundType)
        {
            if (string.IsNullOrWhiteSpace(fundType)) return true;
            return ValidFundTypes.Contains(fundType);
        }

        private static bool IsValidPeriod(string period)
        {
            if (string.IsNullOrWhiteSpace(period)) return true;
            return ValidPeriods.Contains(period.ToLower());
        }

        private static bool IsValidPageParams(int page, int pageSize)
        {
            return page >= 1 && pageSize >= 1 && pageSize <= 100;
        }

        private static ApiResponse<object> ValidationError(string message)
        {
            return new ApiResponse<object>
            {
                Code = ErrorCodes.BadRequest,
                Message = "请求参数错误",
                Data = null,
                Timestamp = DateTime.UtcNow
            };
        }

        private static ApiResponse<object> NotFoundError(string message)
        {
            return new ApiResponse<object>
            {
                Code = ErrorCodes.NotFound,
                Message = message,
                Data = null,
                Timestamp = DateTime.UtcNow
            };
        }

        private static ApiResponse<object> InternalError(string message)
        {
            return new ApiResponse<object>
            {
                Code = ErrorCodes.InternalServerError,
                Message = message,
                Data = null,
                Timestamp = DateTime.UtcNow
            };
        }

        private static void ConfigureFundRoutes(RouteGroupBuilder api)
        {
            var fundsApi = api.MapGroup("/funds");

            fundsApi.MapGet("", async (FundDbContext db, IMemoryCache cache, int page = 1, int pageSize = 10, string? fundType = null, string? riskLevel = null) =>
            {
                if (!IsValidPageParams(page, pageSize))
                {
                    return Results.BadRequest(ValidationError("分页参数无效，page >= 1 且 pageSize <= 100"));
                }

                if (!IsValidFundType(fundType))
                {
                    return Results.BadRequest(ValidationError($"无效的基金类型: {fundType}"));
                }

                var cacheKey = $"funds_list_{page}_{pageSize}_{fundType ?? "all"}_{riskLevel ?? "all"}";
                
                if (cache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return Results.Ok(ApiResponse<object>.Success(cachedResult));
                }
                
                var query = db.FundBasicInfo.AsQueryable();
                
                if (!string.IsNullOrEmpty(fundType))
                {
                    query = query.Where(f => f.FundType == fundType);
                }
                
                if (!string.IsNullOrEmpty(riskLevel))
                {
                    query = query.Where(f => f.RiskLevel == riskLevel);
                }
                
                var total = await query.CountAsync();
                var funds = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(f => new {
                        f.Code,
                        f.Name,
                        f.FundType,
                        f.Manager,
                        f.EstablishDate,
                        f.RiskLevel
                    })
                    .ToListAsync();
                
                var result = new {
                    total,
                    page,
                    pageSize,
                    funds
                };
                
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
                
                return Results.Ok(ApiResponse<object>.Success(result));
            });

            fundsApi.MapGet("/{code}", async (FundDbContext db, IMemoryCache cache, string code) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                var cacheKey = $"fund_detail_{code}";
                
                if (cache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return Results.Ok(ApiResponse<object>.Success(cachedResult));
                }
                
                var fund = await db.FundBasicInfo
                    .Where(f => f.Code == code)
                    .FirstOrDefaultAsync();
                
                if (fund == null)
                {
                    return Results.NotFound(NotFoundError($"基金代码不存在: {code}"));
                }
                
                cache.Set(cacheKey, fund, TimeSpan.FromMinutes(10));
                
                return Results.Ok(ApiResponse<object>.Success(fund));
            });

            fundsApi.MapGet("/{code}/nav", async (FundDbContext db, IMemoryCache cache, string code, DateOnly? startDate = null, DateOnly? endDate = null) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                if (startDate.HasValue && endDate.HasValue && startDate > endDate)
                {
                    return Results.BadRequest(ValidationError("开始日期不能大于结束日期"));
                }

                var cacheKey = $"fund_nav_{code}_{startDate?.ToString() ?? "all"}_{endDate?.ToString() ?? "all"}";
                
                if (cache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return Results.Ok(ApiResponse<object>.Success(cachedResult));
                }
                
                var query = db.FundNavHistory.Where(n => n.Code == code);
                
                if (startDate.HasValue)
                {
                    query = query.Where(n => n.Date >= startDate.Value);
                }
                
                if (endDate.HasValue)
                {
                    query = query.Where(n => n.Date <= endDate.Value);
                }
                
                var navHistoryList = await query
                    .OrderBy(n => n.Date)
                    .ToListAsync();

                if (!navHistoryList.Any())
                {
                    return Results.Ok(ApiResponse<object>.Success(new { code, navHistory = new List<object>() }));
                }
                
                // 计算复权净值
                decimal? firstAccumulatedNav = null;
                decimal? firstNav = null;
                
                var navHistory = new List<object>();
                
                foreach (var item in navHistoryList)
                {
                    if (firstAccumulatedNav == null || firstNav == null)
                    {
                        firstAccumulatedNav = item.AccumulatedNav;
                        firstNav = item.Nav;
                    }
                    
                    decimal? adjustedNav = null;
                    if (firstAccumulatedNav > 0)
                    {
                        adjustedNav = item.AccumulatedNav / firstAccumulatedNav * firstNav;
                    }
                    
                    navHistory.Add(new {
                        item.Date,
                        item.Nav,
                        item.AccumulatedNav,
                        item.DailyGrowthRate,
                        AdjustedNav = adjustedNav
                    });
                }
                
                var result = new {
                    code,
                    navHistory
                };
                
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(15));
                
                return Results.Ok(ApiResponse<object>.Success(result));
            });

            fundsApi.MapGet("/{code}/performance", async (FundDbContext db, IMemoryCache cache, string code) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                var cacheKey = $"fund_performance_{code}";
                
                if (cache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return Results.Ok(ApiResponse<object>.Success(cachedResult));
                }
                
                var performances = await db.FundPerformance
                    .Where(p => p.Code == code)
                    .Select(p => new {
                        p.PeriodType,
                        p.NavGrowthRate,
                        p.MaxDrawdown,
                        p.SharpeRatio
                    })
                    .ToListAsync();
                
                var result = new {
                    code,
                    performances
                };
                
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
                
                return Results.Ok(ApiResponse<object>.Success(result));
            });

            fundsApi.MapGet("/{code}/managers", async (FundDbContext db, IMemoryCache cache, string code) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                var cacheKey = $"fund_managers_{code}";
                
                if (cache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return Results.Ok(ApiResponse<object>.Success(cachedResult));
                }
                
                var managers = await db.FundManager
                    .Where(m => m.Code == code)
                    .Select(m => new {
                        m.ManagerName,
                        m.Tenure,
                        m.StartDate,
                        m.EndDate
                    })
                    .ToListAsync();
                
                var result = new {
                    code,
                    managers
                };
                
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(60));
                
                return Results.Ok(ApiResponse<object>.Success(result));
            });

            fundsApi.MapGet("/{code}/scale", async (FundDbContext db, IMemoryCache cache, string code) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                var cacheKey = $"fund_scale_{code}";
                
                if (cache.TryGetValue(cacheKey, out var cachedResult))
                {
                    return Results.Ok(ApiResponse<object>.Success(cachedResult));
                }
                
                var scales = await db.FundAssetScale
                    .Where(s => s.Code == code)
                    .OrderBy(s => s.Date)
                    .Select(s => new {
                        s.Date,
                        s.AssetScale
                    })
                    .ToListAsync();
                
                var result = new {
                    code,
                    scales
                };
                
                cache.Set(cacheKey, result, TimeSpan.FromMinutes(60));
                
                return Results.Ok(ApiResponse<object>.Success(result));
            });
        }

        private static void ConfigureAnalysisRoutes(RouteGroupBuilder api)
        {
            var analysisApi = api.MapGroup("/analysis");

            analysisApi.MapGet("/ranking", async (IFundAnalysisService analysisService, string period = "month", int limit = 10, string order = "desc") =>
            {
                if (!IsValidPeriod(period))
                {
                    return Results.BadRequest(ValidationError($"无效的周期: {period}，应为 week/month/quarter/year"));
                }

                if (limit < 1 || limit > 100)
                {
                    return Results.BadRequest(ValidationError("排名数量 limit 应在 1-100 之间"));
                }

                if (order != "asc" && order != "desc")
                {
                    return Results.BadRequest(ValidationError("排序方式 order 应为 asc 或 desc"));
                }

                var rankings = await analysisService.GetFundRanking(period, limit, order);
                
                var result = rankings.Select(r => new {
                    rank = r.Rank,
                    code = r.Code,
                    name = r.Name,
                    fundType = r.FundType,
                    returnRate = r.ReturnRate,
                    nav = r.Nav
                }).ToList();
                
                return Results.Ok(ApiResponse<object>.Success(new {
                    period,
                    limit,
                    order,
                    rankings = result
                }));
            });

            analysisApi.MapGet("/change", async (IFundAnalysisService analysisService, string period = "month", int limit = 10, string type = "absolute") =>
            {
                if (!IsValidPeriod(period))
                {
                    return Results.BadRequest(ValidationError($"无效的周期: {period}，应为 week/month/quarter/year"));
                }

                if (limit < 1 || limit > 100)
                {
                    return Results.BadRequest(ValidationError("排名数量 limit 应在 1-100 之间"));
                }

                if (type != "absolute" && type != "relative")
                {
                    return Results.BadRequest(ValidationError("变化类型 type 应为 absolute 或 relative"));
                }

                var rankings = await analysisService.GetFundChangeRanking(period, limit, type);
                
                var result = rankings.Select(r => new {
                    rank = r.Rank,
                    code = r.Code,
                    name = r.Name,
                    fundType = r.FundType,
                    changeValue = r.ChangeValue,
                    changeRate = r.ChangeRate
                }).ToList();
                
                return Results.Ok(ApiResponse<object>.Success(new {
                    period,
                    limit,
                    type,
                    rankings = result
                }));
            });

            analysisApi.MapGet("/consistency", async (IFundAnalysisService analysisService, string startDate = "2023-01-01", string endDate = "2024-01-01", int limit = 10) =>
            {
                if (!DateOnly.TryParse(startDate, out _))
                {
                    return Results.BadRequest(ValidationError($"无效的开始日期: {startDate}"));
                }

                if (!DateOnly.TryParse(endDate, out _))
                {
                    return Results.BadRequest(ValidationError($"无效的结束日期: {endDate}"));
                }

                if (DateOnly.Parse(startDate) > DateOnly.Parse(endDate))
                {
                    return Results.BadRequest(ValidationError("开始日期不能大于结束日期"));
                }

                if (limit < 1 || limit > 100)
                {
                    return Results.BadRequest(ValidationError("排名数量 limit 应在 1-100 之间"));
                }

                var funds = await analysisService.GetFundConsistency(startDate, endDate, limit);
                
                var result = funds.Select(f => new {
                    code = f.Code,
                    name = f.Name,
                    fundType = f.FundType,
                    consistencyScore = f.ConsistencyScore,
                    averageReturn = f.AverageReturn
                }).ToList();
                
                return Results.Ok(ApiResponse<object>.Success(new {
                    startDate,
                    endDate,
                    limit,
                    funds = result
                }));
            });

            analysisApi.MapGet("/multifactor", async (IFundAnalysisService analysisService, int limit = 10, string[]? factors = null) =>
            {
                if (limit < 1 || limit > 100)
                {
                    return Results.BadRequest(ValidationError("排名数量 limit 应在 1-100 之间"));
                }

                var funds = await analysisService.GetFundMultiFactorScore(limit, factors);
                
                var result = funds.Select(f => new {
                    code = f.Code,
                    name = f.Name,
                    fundType = f.FundType,
                    totalScore = f.TotalScore,
                    scores = new {
                        returnScore = f.Scores.ReturnScore,
                        riskScore = f.Scores.RiskScore,
                        riskAdjustedReturnScore = f.Scores.RiskAdjustedReturnScore,
                        rankingScore = f.Scores.RankingScore
                    }
                }).ToList();
                
                return Results.Ok(ApiResponse<object>.Success(new {
                    limit,
                    factors,
                    funds = result
                }));
            });

            analysisApi.MapPost("/compare", async (IFundAnalysisService analysisService, CompareFundsRequest request) =>
            {
                if (request.FundIds == null || request.FundIds.Length == 0)
                {
                    return Results.BadRequest(ValidationError("基金代码列表不能为空"));
                }

                if (request.FundIds.Length > 10)
                {
                    return Results.BadRequest(ValidationError("最多只能对比10只基金"));
                }

                foreach (var fundId in request.FundIds)
                {
                    if (!IsValidFundCode(fundId))
                    {
                        return Results.BadRequest(ValidationError($"无效的基金代码: {fundId}，应为6位数字"));
                    }
                }

                var funds = await analysisService.CompareFunds(request.FundIds);
                
                var result = funds.Select(f => new {
                    fundId = f.FundId,
                    fundName = f.FundName,
                    fundType = f.FundType,
                    nav = f.Nav,
                    accumulatedNav = f.AccumulatedNav,
                    monthlyReturn = f.MonthlyReturn,
                    quarterlyReturn = f.QuarterlyReturn,
                    yearlyReturn = f.YearlyReturn,
                    maxDrawdown = f.MaxDrawdown,
                    sharpeRatio = f.SharpeRatio
                }).ToList();
                
                return Results.Ok(ApiResponse<object>.Success(new { funds = result }));
            });
        }

        private static void ConfigureFavoriteRoutes(RouteGroupBuilder api)
        {
            var favoritesApi = api.MapGroup("/favorites");

            favoritesApi.MapGet("", (string? userId = null) =>
            {
                var favorites = new[] {
                    new {
                        code = "000001",
                        name = "华夏成长混合",
                        fundType = "混合型",
                        nav = 1.2345,
                        dailyGrowthRate = 0.005,
                        note = "长期持有"
                    }
                };
                
                return Results.Ok(ApiResponse<object>.Success(new { favorites }));
            });

            favoritesApi.MapPost("", (FundDbContext db, AddFavoriteRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request.Code))
                {
                    return Results.BadRequest(ValidationError("基金代码不能为空"));
                }

                if (!IsValidFundCode(request.Code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {request.Code}，应为6位数字"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "添加成功"
                }));
            });

            favoritesApi.MapDelete("/{code}", (string code, string? userId = null) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "删除成功"
                }));
            });

            favoritesApi.MapPut("/sort", (UpdateFavoritesSortRequest request) =>
            {
                if (request.FundCodes == null || request.FundCodes.Length == 0)
                {
                    return Results.BadRequest(ValidationError("基金代码列表不能为空"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "排序更新成功"
                }));
            });

            favoritesApi.MapPut("/{code}/note", (string code, UpdateFavoriteNoteRequest request) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "备注更新成功"
                }));
            });

            favoritesApi.MapGet("/groups", (string? userId = null) =>
            {
                var groups = new[] {
                    new {
                        id = "1",
                        name = "我的基金",
                        fundCount = 5
                    },
                    new {
                        id = "2",
                        name = "高收益基金",
                        fundCount = 3
                    }
                };
                
                return Results.Ok(ApiResponse<object>.Success(new { groups }));
            });

            favoritesApi.MapPost("/groups", (CreateFavoriteGroupRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.BadRequest(ValidationError("分组名称不能为空"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "分组创建成功",
                    groupId = "3"
                }));
            });

            favoritesApi.MapPut("/{code}/group", (string code, MoveFundToGroupRequest request) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "基金移动成功"
                }));
            });
        }

        private static void ConfigureFavoriteScoreRoutes(RouteGroupBuilder api)
        {
            var favoriteScoresApi = api.MapGroup("/favorites/scores");

            favoriteScoresApi.MapGet("", (string? userId = null) =>
            {
                var scores = new[] {
                    new {
                        code = "000001",
                        name = "华夏成长混合",
                        totalScore = 85.5,
                        scores = new {
                            returnScore = 90.0,
                            riskScore = 80.0,
                            riskAdjustedReturnScore = 85.0,
                            rankingScore = 82.0
                        },
                        lastUpdated = DateTime.Now
                    }
                };
                
                return Results.Ok(ApiResponse<object>.Success(new { scores }));
            });

            favoriteScoresApi.MapPost("/calculate", (CalculateScoresRequest request) =>
            {
                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "评分计算完成",
                    calculationTime = DateTime.Now
                }));
            });

            favoriteScoresApi.MapGet("/history", (string code, string? userId = null) =>
            {
                if (!IsValidFundCode(code))
                {
                    return Results.BadRequest(ValidationError($"无效的基金代码: {code}，应为6位数字"));
                }

                var history = new[] {
                    new {
                        date = "2024-01-01",
                        score = 82.0
                    },
                    new {
                        date = "2024-02-01",
                        score = 83.5
                    },
                    new {
                        date = "2024-03-01",
                        score = 85.5
                    }
                };
                
                return Results.Ok(ApiResponse<object>.Success(new {
                    code,
                    history
                }));
            });

            favoriteScoresApi.MapGet("/weights", (string? userId = null) =>
            {
                var weights = new {
                    returnWeight = 0.3,
                    riskWeight = 0.2,
                    riskAdjustedReturnWeight = 0.3,
                    rankingWeight = 0.2
                };
                
                return Results.Ok(ApiResponse<object>.Success(weights));
            });

            favoriteScoresApi.MapPut("/weights", (UpdateWeightsRequest request) =>
            {
                if (request.ReturnWeight < 0 || request.RiskWeight < 0 || 
                    request.RiskAdjustedReturnWeight < 0 || request.RankingWeight < 0)
                {
                    return Results.BadRequest(ValidationError("权重值不能为负数"));
                }

                var totalWeight = request.ReturnWeight + request.RiskWeight + 
                                  request.RiskAdjustedReturnWeight + request.RankingWeight;
                if (Math.Abs(totalWeight - 1.0) > 0.01)
                {
                    return Results.BadRequest(ValidationError("权重总和必须等于1"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "权重配置更新成功"
                }));
            });
        }

        private static void ConfigureQueryRoutes(RouteGroupBuilder api)
        {
            var queryApi = api.MapGroup("/query");

            queryApi.MapPost("/kql", (ExecuteKqlQueryRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(ValidationError("查询语句不能为空"));
                }

                if (request.Query.Length > 500)
                {
                    return Results.BadRequest(ValidationError("查询语句长度不能超过500个字符"));
                }

                var results = new[] {
                    new {
                        code = "000001",
                        name = "华夏成长混合",
                        fundType = "混合型",
                        returnRate = 0.15,
                        riskLevel = "中风险",
                        nav = 1.2345
                    }
                };
                
                return Results.Ok(ApiResponse<object>.Success(new {
                    query = request.Query,
                    results,
                    totalCount = results.Length
                }));
            });

            queryApi.MapGet("/history", (string? userId = null, int limit = 10) =>
            {
                if (limit < 1 || limit > 50)
                {
                    return Results.BadRequest(ValidationError("查询历史数量 limit 应在 1-50 之间"));
                }

                var history = new[] {
                    new {
                        id = "1",
                        query = "fundType='混合型' and returnRate > 0.1",
                        executedAt = DateTime.Now.AddDays(-1),
                        resultCount = 5
                    },
                    new {
                        id = "2",
                        query = "riskLevel='低风险'",
                        executedAt = DateTime.Now.AddDays(-2),
                        resultCount = 3
                    }
                };
                
                return Results.Ok(ApiResponse<object>.Success(new { history }));
            });

            queryApi.MapPost("/templates", (SaveQueryTemplateRequest request) =>
            {
                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return Results.BadRequest(ValidationError("模板名称不能为空"));
                }

                if (string.IsNullOrWhiteSpace(request.Query))
                {
                    return Results.BadRequest(ValidationError("查询语句不能为空"));
                }

                return Results.Ok(ApiResponse<object>.Success(new {
                    success = true,
                    message = "模板保存成功",
                    templateId = "t123"
                }));
            });

            queryApi.MapGet("/templates", (string? userId = null) =>
            {
                var templates = new[] {
                    new {
                        id = "t123",
                        name = "高收益混合型基金",
                        query = "fundType='混合型' and returnRate > 0.1",
                        createdAt = DateTime.Now.AddDays(-7)
                    },
                    new {
                        id = "t124",
                        name = "低风险基金",
                        query = "riskLevel='低风险'",
                        createdAt = DateTime.Now.AddDays(-14)
                    }
                };
                
                return Results.Ok(ApiResponse<object>.Success(new { templates }));
            });
        }

        private static void ConfigureMetaRoutes(RouteGroupBuilder api)
        {
            var metaApi = api.MapGroup("/meta");

            metaApi.MapGet("/fund-types", (FundDbContext db) =>
            {
                var fundTypes = new[] {
                    new { value = "混合型", label = "混合型" },
                    new { value = "股票型", label = "股票型" },
                    new { value = "债券型", label = "债券型" },
                    new { value = "货币型", label = "货币型" },
                    new { value = "指数型", label = "指数型" }
                };

                return Results.Ok(ApiResponse<object>.Success(fundTypes));
            });

            metaApi.MapGet("/managers", (FundDbContext db) =>
            {
                var managers = db.FundManager
                    .Select(m => new { m.Code, m.ManagerName, m.Tenure })
                    .Distinct()
                    .OrderBy(m => m.ManagerName)
                    .ToList();

                return Results.Ok(ApiResponse<object>.Success(managers));
            });
        }

        private static void ConfigureSystemRoutes(RouteGroupBuilder api)
        {
            var systemApi = api.MapGroup("/system");

            systemApi.MapGet("/status", async (ISystemService systemService) =>
                {
                    var result = await systemService.GetSystemStatusAsync();
                    return Results.Ok(ApiResponse<object>.Success(result));
                });

            systemApi.MapPost("/update", async (ISystemService systemService, string? type = "full") =>
                {
                    if (type != "full" && type != "incremental")
                    {
                        return Results.BadRequest(ValidationError("更新类型 type 应为 full 或 incremental"));
                    }

                    var result = await systemService.TriggerDataUpdateAsync(type);
                    return Results.Ok(ApiResponse<object>.Success(result));
                });

            systemApi.MapGet("/update-history", async (ISystemService systemService, int limit = 10) =>
                {
                    if (limit < 1 || limit > 100)
                    {
                        return Results.BadRequest(ValidationError("历史记录数量 limit 应在 1-100 之间"));
                    }

                    var result = await systemService.GetUpdateHistoryAsync(limit);
                    return Results.Ok(ApiResponse<object>.Success(result));
                });
        }
    }

    public class CompareFundsRequest
    {
        public string[] FundIds { get; set; }
    }

    public class AddFavoriteRequest
    {
        public string Code { get; set; }
        public string? UserId { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateFavoritesSortRequest
    {
        public string[] FundCodes { get; set; }
    }

    public class UpdateFavoriteNoteRequest
    {
        public string Note { get; set; }
    }

    public class CreateFavoriteGroupRequest
    {
        public string Name { get; set; }
    }

    public class MoveFundToGroupRequest
    {
        public string? GroupId { get; set; }
    }

    public class CalculateScoresRequest
    {
        public string[]? FundCodes { get; set; }
    }

    public class UpdateWeightsRequest
    {
        public double ReturnWeight { get; set; }
        public double RiskWeight { get; set; }
        public double RiskAdjustedReturnWeight { get; set; }
        public double RankingWeight { get; set; }
    }

    public class ExecuteKqlQueryRequest
    {
        public string? Query { get; set; }
    }

    public class SaveQueryTemplateRequest
    {
        public string? Name { get; set; }
        public string? Query { get; set; }
    }
}
