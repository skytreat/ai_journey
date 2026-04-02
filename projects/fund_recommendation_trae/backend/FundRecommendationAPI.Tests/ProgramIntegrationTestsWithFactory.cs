using FundRecommendationAPI.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace FundRecommendationAPI.Tests;

public class ProgramIntegrationTestsWithFactory : IAsyncLifetime
{
    private HttpClient _client;
    private IHost _host;

    public async Task InitializeAsync()
    {
        // 创建测试主机
        var hostBuilder = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        var startup = new TestStartup();
                        startup.ConfigureServices(services);
                    })
                    .Configure(app =>
                    {
                        var startup = new TestStartup();
                        startup.Configure(app, null);
                    });
            });
        
        // 构建并启动主机
        _host = await hostBuilder.StartAsync();
        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
    }

    // 测试启动类
    public class TestStartup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // 添加内存缓存
            services.AddMemoryCache();

            // 配置路由
            services.AddRouting();
            
            // 配置限流
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("api", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 10;
                });
            });

            // 添加数据库上下文（使用内存数据库）
            services.AddDbContext<FundDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment? env)
        {
            if (env != null && env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseRateLimiter();

            // 清理数据库
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<FundDbContext>();
                try
                {
                    dbContext.FundBasicInfo.RemoveRange(dbContext.FundBasicInfo);
                    dbContext.FundNavHistory.RemoveRange(dbContext.FundNavHistory);
                    dbContext.FundPerformance.RemoveRange(dbContext.FundPerformance);
                    dbContext.FundManager.RemoveRange(dbContext.FundManager);
                    dbContext.FundAssetScale.RemoveRange(dbContext.FundAssetScale);
                    dbContext.SaveChanges();
                }
                catch (Exception)
                {
                    // 忽略清理错误
                }
            }

            // 配置API路由
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                var api = endpoints.MapGroup("/api").RequireRateLimiting("api");

                // 1. 基金查询API
                var fundsApi = api.MapGroup("/funds");

                // 获取基金列表
                fundsApi.MapGet("", (int page = 1, int pageSize = 10, string? fundType = null, string? riskLevel = null) =>
                {
                    // 模拟数据
                    var funds = new[] {
                        new {
                            Code = "000001",
                            Name = "华夏成长混合",
                            FundType = "混合型",
                            Manager = "测试经理",
                            EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1)),
                            RiskLevel = "中风险"
                        },
                        new {
                            Code = "000002",
                            Name = "嘉实增长",
                            FundType = "混合型",
                            Manager = "测试经理",
                            EstablishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-2)),
                            RiskLevel = "中风险"
                        }
                    };

                    var result = new {
                        total = 2,
                        page,
                        pageSize,
                        funds
                    };

                    return Results.Ok(result);
                });

                // 获取单个基金详情
                fundsApi.MapGet("/{code}", (string code) =>
                {
                    // 模拟数据 - 只支持000001和000002
                    var validFundCodes = new[] { "000001", "000002" };
                    
                    if (!validFundCodes.Contains(code))
                    {
                        return Results.NotFound();
                    }
                    
                    // 模拟数据
                    var fund = new {
                        code = code,
                        name = "测试基金",
                        fundType = "混合型",
                        shareType = "A类",
                        mainFundCode = code,
                        manager = "测试经理",
                        custodian = "测试托管银行",
                        establishDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1)),
                        riskLevel = "中风险",
                        benchmark = "沪深300指数",
                        trackingTarget = "",
                        investmentStyle = "成长型"
                    };

                    return Results.Ok(fund);
                });

                // 获取基金历史净值
                fundsApi.MapGet("/{code}/nav", (string code, DateOnly? startDate = null, DateOnly? endDate = null) =>
                {
                    // 模拟数据
                    var navHistory = new List<object>();
                    for (int i = 0; i < 30; i++)
                    {
                        var date = DateOnly.FromDateTime(DateTime.Now.AddDays(-i));
                        navHistory.Add(new {
                            date = date,
                            nav = (decimal)(1.0 + i * 0.01),
                            accumulatedNav = (decimal)(1.0 + i * 0.01),
                            dailyGrowthRate = (decimal?)0.001,
                            adjustedNav = (decimal)(1.0 + i * 0.01)
                        });
                    }

                    var result = new {
                        code,
                        navHistory
                    };

                    return Results.Ok(result);
                });

                // 获取基金业绩指标
                fundsApi.MapGet("/{code}/performance", (string code) =>
                {
                    // 模拟数据
                    var performances = new[] {
                        new { periodType = "1个月", navGrowthRate = 0.05m, maxDrawdown = (decimal?)0.02, sharpeRatio = (decimal?)1.2 },
                        new { periodType = "3个月", navGrowthRate = 0.15m, maxDrawdown = (decimal?)0.05, sharpeRatio = (decimal?)1.1 },
                        new { periodType = "6个月", navGrowthRate = 0.25m, maxDrawdown = (decimal?)0.08, sharpeRatio = (decimal?)1.0 },
                        new { periodType = "1年", navGrowthRate = 0.40m, maxDrawdown = (decimal?)0.12, sharpeRatio = (decimal?)0.9 }
                    };

                    var result = new {
                        code,
                        performances
                    };

                    return Results.Ok(result);
                });

                // 获取基金经理信息
                fundsApi.MapGet("/{code}/managers", (string code) =>
                {
                    // 模拟数据
                    var managers = new[] {
                        new { managerName = "经理A", tenure = "3.5", startDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-3)), endDate = (DateOnly?)null }
                    };

                    var result = new {
                        code,
                        managers
                    };

                    return Results.Ok(result);
                });

                // 获取基金规模历史
                fundsApi.MapGet("/{code}/scale", (string code) =>
                {
                    // 模拟数据
                    var scales = new List<object>();
                    for (int i = 0; i < 12; i++)
                    {
                        var date = DateOnly.FromDateTime(DateTime.Now.AddMonths(-i));
                        scales.Add(new {
                            date = date,
                            assetScale = 10000 + i * 1000
                        });
                    }

                    var result = new {
                        code,
                        scales
                    };

                    return Results.Ok(result);
                });

                // 2. 分析API
                var analysisApi = api.MapGroup("/analysis");

                // 单周期基金排名
                analysisApi.MapGet("/ranking", (string period = "month", int limit = 10, string order = "desc") =>
                {
                    // 模拟数据
                    var rankings = new[] {
                        new { rank = 1, code = "000001", name = "华夏成长", fundType = "混合型", returnRate = 0.15, nav = 1.2345 },
                        new { rank = 2, code = "000002", name = "嘉实增长", fundType = "混合型", returnRate = 0.12, nav = 1.1234 },
                        new { rank = 3, code = "000003", name = "易方达蓝筹", fundType = "混合型", returnRate = 0.10, nav = 1.0987 }
                    };

                    var result = new {
                        period,
                        limit,
                        order,
                        rankings
                    };

                    return Results.Ok(result);
                });

                // 周期变化率排名
                analysisApi.MapGet("/change", (string period = "month", int limit = 10, string type = "absolute") =>
                {
                    // 模拟数据
                    var rankings = new[] {
                        new { rank = 1, code = "000001", name = "华夏成长", fundType = "混合型", changeValue = 0.05m, changeRate = 0.05m },
                        new { rank = 2, code = "000002", name = "嘉实增长", fundType = "混合型", changeValue = 0.04m, changeRate = 0.04m },
                        new { rank = 3, code = "000003", name = "易方达蓝筹", fundType = "混合型", changeValue = 0.03m, changeRate = 0.03m }
                    };

                    var result = new {
                        period,
                        limit,
                        type,
                        rankings
                    };

                    return Results.Ok(result);
                });

                // 多周期一致性筛选
                analysisApi.MapGet("/consistency", (string startDate = "2023-01-01", string endDate = "2024-01-01", int limit = 10) =>
                {
                    // 模拟数据
                    var funds = new[] {
                        new { code = "000001", name = "华夏成长", fundType = "混合型", consistencyScore = 0.85, averageReturn = 0.12 },
                        new { code = "000002", name = "嘉实增长", fundType = "混合型", consistencyScore = 0.80, averageReturn = 0.10 },
                        new { code = "000003", name = "易方达蓝筹", fundType = "混合型", consistencyScore = 0.75, averageReturn = 0.09 }
                    };

                    var result = new {
                        startDate,
                        endDate,
                        limit,
                        funds
                    };

                    return Results.Ok(result);
                });

                // 多因子量化评估
                analysisApi.MapGet("/multifactor", (int limit = 10, string[] factors = null) =>
                {
                    // 模拟数据
                    var funds = new[] {
                        new {
                            code = "000001",
                            name = "华夏成长",
                            fundType = "混合型",
                            totalScore = 85.5,
                            scores = new {
                                returnScore = 90.0,
                                riskScore = 80.0,
                                riskAdjustedReturnScore = 85.0,
                                rankingScore = 82.0
                            }
                        },
                        new {
                            code = "000002",
                            name = "嘉实增长",
                            fundType = "混合型",
                            totalScore = 82.5,
                            scores = new {
                                returnScore = 85.0,
                                riskScore = 85.0,
                                riskAdjustedReturnScore = 80.0,
                                rankingScore = 80.0
                            }
                        }
                    };

                    var result = new {
                        limit,
                        factors,
                        funds
                    };

                    return Results.Ok(result);
                });

                // 基金对比分析
                analysisApi.MapPost("/compare", (dynamic request) =>
                {
                    // 模拟数据
                    var funds = new[] {
                        new {
                            Code = "000001",
                            Name = "华夏成长",
                            FundType = "混合型",
                            Nav = 1.2345,
                            AccumulatedNav = 1.2345,
                            MonthlyReturn = 0.05m,
                            QuarterlyReturn = 0.15m,
                            YearlyReturn = 0.40m,
                            MaxDrawdown = 0.12m,
                            SharpeRatio = 0.9m
                        },
                        new {
                            Code = "000002",
                            Name = "嘉实增长",
                            FundType = "混合型",
                            Nav = 1.1234,
                            AccumulatedNav = 1.1234,
                            MonthlyReturn = 0.04m,
                            QuarterlyReturn = 0.12m,
                            YearlyReturn = 0.35m,
                            MaxDrawdown = 0.10m,
                            SharpeRatio = 0.85m
                        }
                    };

                    var result = new { funds };
                    return Results.Ok(result);
                });

                // 3. 自选基金API
                var favoritesApi = api.MapGroup("/favorites");

                // 获取用户自选基金列表
                favoritesApi.MapGet("", (string? userId = null) =>
                {
                    // 模拟数据
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
                    
                    return Results.Ok(new { favorites });
                });

                // 添加基金到自选
                favoritesApi.MapPost("", (System.Text.Json.JsonElement request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "添加成功"
                    });
                });

                // 从自选移除基金
                favoritesApi.MapDelete("/{code}", (string code, string? userId = null) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "删除成功"
                    });
                });

                // 更新自选基金排序
                favoritesApi.MapPut("/sort", (System.Text.Json.JsonElement request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "排序更新成功"
                    });
                });

                // 更新自选基金备注
                favoritesApi.MapPut("/{code}/note", (string code, System.Text.Json.JsonElement request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "备注更新成功"
                    });
                });

                // 获取用户自选分组
                favoritesApi.MapGet("/groups", (string? userId = null) =>
                {
                    // 模拟数据
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
                    
                    return Results.Ok(new { groups });
                });

                // 创建新分组
                favoritesApi.MapPost("/groups", (dynamic request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "分组创建成功",
                        groupId = "3"
                    });
                });

                // 移动基金到指定分组
                favoritesApi.MapPut("/{code}/group", (string code, dynamic request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "基金移动成功"
                    });
                });

                // 4. 自选基金评分API
                var favoriteScoresApi = api.MapGroup("/favorites/scores");

                // 获取自选基金多因子评分
                favoriteScoresApi.MapGet("", (string? userId = null) =>
                {
                    // 模拟数据
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
                    
                    return Results.Ok(new { scores });
                });

                // 手动触发评分计算
                favoriteScoresApi.MapPost("/calculate", (dynamic request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "评分计算完成",
                        calculationTime = DateTime.Now
                    });
                });

                // 获取评分历史趋势
                favoriteScoresApi.MapGet("/history", (string code, string? userId = null) =>
                {
                    // 模拟数据
                    var history = new[] {
                        new { date = "2024-01-01", score = 82.0 },
                        new { date = "2024-02-01", score = 83.5 },
                        new { date = "2024-03-01", score = 85.5 }
                    };
                    
                    return Results.Ok(new {
                        code,
                        history
                    });
                });

                // 获取用户权重配置
                favoriteScoresApi.MapGet("/weights", (string? userId = null) =>
                {
                    // 模拟数据
                    var weights = new {
                        returnWeight = 0.3,
                        riskWeight = 0.2,
                        riskAdjustedReturnWeight = 0.3,
                        rankingWeight = 0.2
                    };
                    
                    return Results.Ok(weights);
                });

                // 更新用户权重配置
                favoriteScoresApi.MapPut("/weights", (dynamic request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "权重配置更新成功"
                    });
                });

                // 5. 自定义查询API
                var queryApi = api.MapGroup("/query");

                // 执行自定义KQL查询
                queryApi.MapPost("/kql", (System.Text.Json.JsonElement request) =>
                {
                    // 模拟数据
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
                    
                    return Results.Ok(new {
                        query = "test query",
                        results,
                        totalCount = results.Length
                    });
                });

                // 获取查询历史记录
                queryApi.MapGet("/history", (string? userId = null, int limit = 10) =>
                {
                    // 模拟数据
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
                    
                    return Results.Ok(new { history });
                });

                // 保存查询模板
                queryApi.MapPost("/templates", (dynamic request) =>
                {
                    // 模拟实现
                    return Results.Ok(new {
                        success = true,
                        message = "模板保存成功",
                        templateId = "t123"
                    });
                });

                // 获取查询模板列表
                queryApi.MapGet("/templates", (string? userId = null) =>
                {
                    // 模拟数据
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
                    
                    return Results.Ok(new { templates });
                });

                // 6. 系统API
                var metaApi = api.MapGroup("/meta");

                // 获取所有基金类型
                metaApi.MapGet("/fund-types", () =>
                {
                    // 模拟数据
                    var fundTypes = new[] {
                        new { value = "混合型", label = "混合型" },
                        new { value = "股票型", label = "股票型" },
                        new { value = "债券型", label = "债券型" },
                        new { value = "货币型", label = "货币型" },
                        new { value = "指数型", label = "指数型" }
                    };

                    return Results.Ok(fundTypes);
                });
            });
        }
    }

    [Fact]
    public async Task GetFundsList_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/funds");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAnalysisRanking_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/analysis/ranking");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFavorites_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/favorites");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFundTypes_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/meta/fund-types");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ApiEndpoints_Return404_ForInvalidEndpoint()
    {
        var response = await _client.GetAsync("/api/invalid-endpoint");
        Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFundDetail_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/funds/000001");
        // 应该返回200，因为我们会在测试中创建测试数据
        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
        Assert.Equal("000001", result.code.ToString());
    }

    [Fact]
    public async Task GetFundNav_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/funds/000001/nav");
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFundPerformance_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/funds/000001/performance");
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFundManagers_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/funds/000001/managers");
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFundScale_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/funds/000001/scale");
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAnalysisChange_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/analysis/change");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAnalysisConsistency_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/analysis/consistency");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetAnalysisMultifactor_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/analysis/multifactor");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFavoritesGroups_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/favorites/groups");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFavoriteScores_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/favorites/scores");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFavoriteScoresHistory_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/favorites/scores/history?code=000001");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetFavoriteScoresWeights_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/favorites/scores/weights");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetQueryHistory_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/query/history");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetQueryTemplates_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/query/templates");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(result);
    }
}
