using FundRecommendationAPI.Extensions;
using FundRecommendationAPI.Models;
using FundRecommendationAPI.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FundRecommendationAPI.Tests
{
    public class TestStartup
    {
        public IConfiguration Configuration { get; }

        public TestStartup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMemoryCache();
            services.AddLogging(builder => builder.AddConsole().AddDebug());
            
            // 注册数据库上下文
            services.AddDbContext<FundDbContext>(options =>
                options.UseInMemoryDatabase("TestDb"));
            
            // 注册OpenAPI和路由
            services.AddOpenApi();
            services.AddRouting();
            
            // 注册限流（使用内存实现）
            services.AddRateLimiter(options =>
            {
                options.AddFixedWindowLimiter("api", opt =>
                {
                    opt.PermitLimit = 100;
                    opt.Window = TimeSpan.FromMinutes(1);
                    opt.QueueLimit = 10;
                });
            });
            
            // 注册仓库服务
            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            
            // 注册系统服务
            services.AddScoped<ISystemService, SystemService>();
            
            // 注册基金分析服务
            services.AddScoped<IFundAnalysisService, FundAnalysisService>();
            
            // 注册基金数据服务
            services.AddScoped<IFundDataService, FundDataService>();
            
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseRouting();
            
            var serviceProvider = app.ApplicationServices;
            
            app.UseEndpoints(endpoints =>
            {
                var api = endpoints.MapGroup("/api");
                
                var db = serviceProvider.GetRequiredService<FundDbContext>();
                var systemService = serviceProvider.GetRequiredService<ISystemService>();
                
                var fundsApi = api.MapGroup("/funds");
                fundsApi.MapGet("", async (int page = 1, int pageSize = 10, string? fundType = null, string? riskLevel = null) =>
                {
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
                        .OrderBy(f => f.Code)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .ToListAsync();
                    
                    return Results.Ok(new { total, page, pageSize, funds });
                });
                
                fundsApi.MapGet("/{code}", async (string code) =>
                {
                    var fund = await db.FundBasicInfo.FirstOrDefaultAsync(f => f.Code == code);
                    if (fund == null)
                    {
                        return Results.NotFound(new { error = "基金不存在" });
                    }
                    return Results.Ok(fund);
                });
                
                var analysisApi = api.MapGroup("/analysis");
                analysisApi.MapGet("/ranking", async (string period = "month", int limit = 10, string order = "desc") =>
                {
                    var funds = await db.FundBasicInfo
                        .OrderByDescending(f => f.Code)
                        .Take(limit)
                        .ToListAsync();
                    
                    return Results.Ok(new { period, limit, order, rankings = funds });
                });
                
                analysisApi.MapGet("/change", async (string period = "month", int limit = 10, string type = "absolute") =>
                {
                    var funds = await db.FundBasicInfo
                        .OrderByDescending(f => f.Code)
                        .Take(limit)
                        .ToListAsync();
                    
                    return Results.Ok(new { period, limit, type, rankings = funds });
                });
                
                analysisApi.MapGet("/consistency", async (DateOnly? startDate = null, DateOnly? endDate = null, int limit = 10) =>
                {
                    var funds = await db.FundBasicInfo
                        .OrderByDescending(f => f.Code)
                        .Take(limit)
                        .ToListAsync();
                    
                    return Results.Ok(new { startDate, endDate, limit, funds });
                });
                
                analysisApi.MapGet("/multifactor", async (int limit = 10, string[]? factors = null) =>
                {
                    var funds = await db.FundBasicInfo
                        .OrderByDescending(f => f.Code)
                        .Take(limit)
                        .ToListAsync();
                    
                    return Results.Ok(new { limit, factors, funds });
                });
                
                analysisApi.MapPost("/compare", async (string[] fundIds) =>
                {
                    var funds = await db.FundBasicInfo
                        .Where(f => fundIds.Contains(f.Code))
                        .ToListAsync();
                    
                    return Results.Ok(new { funds });
                });
                
                var favoritesApi = api.MapGroup("/favorites");
                favoritesApi.MapGet("", async () =>
                {
                    var favorites = await db.UserFavoriteFunds.ToListAsync();
                    return Results.Ok(new { favorites });
                });
                
                favoritesApi.MapPost("", async (UserFavoriteFunds favorite) =>
                {
                    db.UserFavoriteFunds.Add(favorite);
                    await db.SaveChangesAsync();
                    return Results.Ok(new { success = true });
                });
                
                favoritesApi.MapDelete("/{code}", async (string code) =>
                {
                    var favorite = await db.UserFavoriteFunds.FirstOrDefaultAsync(f => f.FundCode == code);
                    if (favorite != null)
                    {
                        db.UserFavoriteFunds.Remove(favorite);
                        await db.SaveChangesAsync();
                    }
                    return Results.Ok(new { success = true });
                });
                
                var queryApi = api.MapGroup("/query");
                queryApi.MapGet("/history", async () =>
                {
                    return Results.Ok(new { history = Array.Empty<object>() });
                });
                
                queryApi.MapGet("/templates", async () =>
                {
                    return Results.Ok(new { templates = Array.Empty<object>() });
                });
                
                var metaApi = api.MapGroup("/meta");
                metaApi.MapGet("/fund-types", () =>
                {
                    return Results.Ok(new[] {
                        new { value = "混合型", label = "混合型" },
                        new { value = "股票型", label = "股票型" },
                        new { value = "债券型", label = "债券型" },
                        new { value = "货币型", label = "货币型" },
                        new { value = "指数型", label = "指数型" }
                    });
                });
                
                metaApi.MapGet("/managers", async () =>
                {
                    var managers = await db.FundManager
                        .Take(10)
                        .ToListAsync();
                    
                    return Results.Ok(managers);
                });
                
                var systemApi = api.MapGroup("/system");
                systemApi.MapGet("/status", async () =>
                {
                    var totalFunds = await db.FundBasicInfo.CountAsync();
                    var totalManagers = await db.FundManager.CountAsync();
                    
                    return Results.Ok(new { 
                        status = "running", 
                        timestamp = DateTime.Now, 
                        version = "1.0.0", 
                        lastUpdate = (DateTime?)null, 
                        statistics = new { totalFunds, totalManagers } 
                    });
                });
                
                systemApi.MapPost("/update", async (string updateType = "incremental") =>
                {
                    return Results.Ok(new { success = true, message = "数据更新已触发", updateId = "update_test", timestamp = DateTime.Now });
                });
                
                systemApi.MapGet("/update-history", async () =>
                {
                    return Results.Ok(new { total = 0, limit = 10, history = Array.Empty<object>() });
                });
            });
        }
    }
}
