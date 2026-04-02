using FundRecommendationAPI.Extensions;
using FundRecommendationAPI.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;

namespace FundRecommendationAPI.Tests
{
    public class ProgramIntegrationTests : IAsyncLifetime
    {
        private HttpClient? _client;
        private TestServer? _server;

        public async Task InitializeAsync()
        {
            // 创建测试配置
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true)
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string, string>("ConnectionStrings:DefaultConnection", "Data Source=:memory:")
                })
                .Build();

            // 创建测试服务器，使用自定义的TestStartup
            _server = new TestServer(new WebHostBuilder()
                .UseConfiguration(configuration)
                .UseStartup<TestStartup>());

            _client = _server.CreateClient();
        }

        public async Task DisposeAsync()
        {
            if (_client != null)
            {
                _client.Dispose();
            }
            if (_server != null)
            {
                _server.Dispose();
            }
        }

        [Fact]
        public async Task ApiRoutes_ShouldReturnSuccessStatusCodes()
        {
            // 测试元数据接口
            var response = await _client.GetAsync("/api/meta/fund-types");
            response.EnsureSuccessStatusCode();

            // 测试自选基金接口
            response = await _client.GetAsync("/api/favorites");
            response.EnsureSuccessStatusCode();

            // 测试自选基金分组接口
            response = await _client.GetAsync("/api/favorites/groups");
            response.EnsureSuccessStatusCode();

            // 测试自选基金评分接口
            response = await _client.GetAsync("/api/favorites/scores");
            response.EnsureSuccessStatusCode();

            // 测试查询历史接口
            response = await _client.GetAsync("/api/query/history");
            response.EnsureSuccessStatusCode();

            // 测试查询模板接口
            response = await _client.GetAsync("/api/query/templates");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task FundsList_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/funds?page=1&pageSize=10");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task FundDetail_ShouldReturnNotFound()
        {
            var response = await _client.GetAsync("/api/funds/000001");
            // 基金不存在，应该返回404状态码
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task FundNav_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/funds/000001/nav");
            // 即使基金不存在，也应该返回成功状态码
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task FundPerformance_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/funds/000001/performance");
            // 即使基金不存在，也应该返回成功状态码
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task FundManagers_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/funds/000001/managers");
            // 即使基金不存在，也应该返回成功状态码
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task FundScale_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/funds/000001/scale");
            // 即使基金不存在，也应该返回成功状态码
            Assert.True(response.IsSuccessStatusCode);
        }

        [Fact]
        public async Task AnalysisRanking_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/analysis/ranking?period=1y&orderBy=return&order=desc");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AnalysisChange_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/analysis/change?period=1m&orderBy=change&order=desc");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AnalysisConsistency_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/analysis/consistency?periods=3m,6m,1y&orderBy=consistency&order=desc");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AnalysisMultifactor_ShouldReturnSuccess()
        {
            var response = await _client.GetAsync("/api/analysis/multifactor?period=1y&orderBy=score&order=desc");
            response.EnsureSuccessStatusCode();
        }

        [Fact]
        public async Task AnalysisCompare_ShouldReturnSuccess()
        {
            var request = new { FundIds = new[] { "000001", "000002" } };
            var response = await _client.PostAsJsonAsync("/api/analysis/compare", request);
            response.EnsureSuccessStatusCode();
        }
    }
}
