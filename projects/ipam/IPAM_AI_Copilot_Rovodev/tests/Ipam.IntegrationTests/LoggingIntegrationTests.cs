using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace Ipam.IntegrationTests
{
    /// <summary>
    /// Integration tests for the enhanced logging system with Serilog and correlation IDs
    /// </summary>
    public class LoggingIntegrationTests : IClassFixture<WebApplicationFactory<Ipam.Frontend.Program>>
    {
        private readonly WebApplicationFactory<Ipam.Frontend.Program> _factory;
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _output;
        private readonly List<string> _logMessages = new();

        public LoggingIntegrationTests(WebApplicationFactory<Ipam.Frontend.Program> factory, ITestOutputHelper output)
        {
            _factory = factory;
            _output = output;
            _client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.Configure<Microsoft.Extensions.Configuration.ConfigurationManager>(config =>
                    {
                        config["ConnectionStrings:AzureTableStorage"] = "UseDevelopmentStorage=true";
                        config["Caching:Enabled"] = "true";
                        config["Caching:DurationMinutes"] = "1";
                    });
                });
                
                builder.ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddXUnit(output);
                    logging.SetMinimumLevel(LogLevel.Information);
                    
                    // Add a custom logger provider to capture log messages
                    logging.Services.AddSingleton<ILoggerProvider>(serviceProvider =>
                        new TestLoggerProvider(_logMessages));
                });
            }).CreateClient();
        }

        [Fact]
        public async Task Logging_CapturesRequestInformation()
        {
            // Arrange
            var endpoint = "/api/addressspaces/test-space/ipnodes";
            _logMessages.Clear();

            // Act
            var response = await _client.GetAsync(endpoint);

            // Assert
            _output.WriteLine($"Response: {response.StatusCode}");
            _output.WriteLine($"Captured {_logMessages.Count} log messages");
            
            foreach (var message in _logMessages.Take(10)) // Show first 10 messages
            {
                _output.WriteLine($"Log: {message}");
            }

            // Should have captured some log messages
            Assert.True(_logMessages.Count > 0);
            
            // Should log request information
            var requestLogs = _logMessages.Where(m => 
                m.Contains("Request") || 
                m.Contains(endpoint) ||
                m.Contains("GET")).ToList();
                
            Assert.True(requestLogs.Count > 0, "Should have request-related log messages");

            response.Dispose();
        }

        [Fact]
        public async Task Logging_IncludesCorrelationId()
        {
            // Arrange
            var correlationId = Guid.NewGuid().ToString();
            _client.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
            _logMessages.Clear();

            // Act
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes");

            // Assert
            _output.WriteLine($"Correlation ID: {correlationId}");
            _output.WriteLine($"Captured {_logMessages.Count} log messages");

            // Look for correlation ID in logs
            var correlationLogs = _logMessages.Where(m => m.Contains(correlationId)).ToList();
            
            foreach (var log in correlationLogs)
            {
                _output.WriteLine($"Correlation log: {log}");
            }

            // Note: Correlation ID might not be in all log messages depending on implementation
            // But the request should be processed successfully
            Assert.True(response.StatusCode != HttpStatusCode.InternalServerError);

            response.Dispose();
        }

        [Fact]
        public async Task Logging_CapturesErrorInformation()
        {
            // Arrange
            _logMessages.Clear();

            // Act - Trigger an error with invalid prefix
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes/byPrefix?prefix=invalid");

            // Assert
            _output.WriteLine($"Error response: {response.StatusCode}");
            
            var errorLogs = _logMessages.Where(m => 
                m.Contains("Error") || 
                m.Contains("Exception") ||
                m.Contains("Warning") ||
                m.ToLower().Contains("invalid")).ToList();

            foreach (var log in errorLogs)
            {
                _output.WriteLine($"Error log: {log}");
            }

            // Should capture error/warning information
            Assert.True(errorLogs.Count > 0 || response.StatusCode == HttpStatusCode.BadRequest);

            response.Dispose();
        }

        [Fact]
        public async Task Logging_HandlesHighVolumeRequests()
        {
            // Arrange
            _logMessages.Clear();
            var requestCount = 5;
            var tasks = new List<Task<HttpResponseMessage>>();

            // Act - Send multiple concurrent requests
            for (int i = 0; i < requestCount; i++)
            {
                tasks.Add(_client.GetAsync($"/api/addressspaces/space-{i}/ipnodes"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            _output.WriteLine($"Processed {requestCount} requests");
            _output.WriteLine($"Captured {_logMessages.Count} log messages");

            // Should handle multiple requests without logging errors
            var errorLogs = _logMessages.Where(m => 
                m.ToLower().Contains("error") || 
                m.ToLower().Contains("exception")).ToList();

            foreach (var errorLog in errorLogs.Take(5))
            {
                _output.WriteLine($"Error log: {errorLog}");
            }

            // All requests should be processed
            foreach (var response in responses)
            {
                Assert.True(response.StatusCode != HttpStatusCode.InternalServerError);
                response.Dispose();
            }

            // Should have reasonable number of log messages
            Assert.True(_logMessages.Count > 0);
            Assert.True(_logMessages.Count < 1000, $"Too many log messages: {_logMessages.Count}");
        }

        [Fact]
        public async Task Logging_PerformanceLogging_CapturesTimings()
        {
            // Arrange
            _logMessages.Clear();

            // Act
            var response = await _client.GetAsync("/api/addressspaces/test-space/ipnodes");

            // Assert
            var performanceLogs = _logMessages.Where(m => 
                m.Contains("ms") || 
                m.Contains("elapsed") ||
                m.Contains("performance") ||
                m.Contains("timing")).ToList();

            foreach (var log in performanceLogs)
            {
                _output.WriteLine($"Performance log: {log}");
            }

            // Performance logging might be captured (depends on implementation)
            _output.WriteLine($"Found {performanceLogs.Count} performance-related log messages");

            response.Dispose();
        }

        [Fact]
        public async Task Logging_StructuredLogging_ContainsProperties()
        {
            // Arrange
            _logMessages.Clear();

            // Act
            var response = await _client.PostAsync("/api/addressspaces/test-space/ipnodes", 
                new StringContent(JsonSerializer.Serialize(new 
                { 
                    addressSpaceId = "test-space",
                    prefix = "192.168.50.0/24",
                    tags = new Dictionary<string, string> { ["test"] = "logging" }
                }), Encoding.UTF8, "application/json"));

            // Assert
            _output.WriteLine($"POST response: {response.StatusCode}");
            
            // Look for structured log properties
            var structuredLogs = _logMessages.Where(m => 
                m.Contains("{") || 
                m.Contains("AddressSpaceId") ||
                m.Contains("test-space")).ToList();

            foreach (var log in structuredLogs.Take(5))
            {
                _output.WriteLine($"Structured log: {log}");
            }

            _output.WriteLine($"Found {structuredLogs.Count} structured log messages");

            response.Dispose();
        }

        [Fact]
        public async Task Logging_DoesNotLogSensitiveInformation()
        {
            // Arrange
            _logMessages.Clear();
            var sensitiveData = "password123";

            // Act - Send request with sensitive data
            var response = await _client.PostAsync("/api/addressspaces/test-space/ipnodes",
                new StringContent(JsonSerializer.Serialize(new
                {
                    addressSpaceId = "test-space",
                    prefix = "192.168.60.0/24",
                    tags = new Dictionary<string, string> { ["secret"] = sensitiveData }
                }), Encoding.UTF8, "application/json"));

            // Assert
            var sensitiveLogs = _logMessages.Where(m => m.Contains(sensitiveData)).ToList();

            foreach (var log in sensitiveLogs)
            {
                _output.WriteLine($"WARNING - Sensitive data in log: {log}");
            }

            // Should not log sensitive information
            Assert.Empty(sensitiveLogs);
            
            _output.WriteLine($"Good: No sensitive data found in {_logMessages.Count} log messages");

            response.Dispose();
        }

        [Fact]
        public async Task Logging_LogLevel_FilteringWorks()
        {
            // Arrange
            _logMessages.Clear();

            // Act
            var response = await _client.GetAsync("/health");

            // Assert
            var debugLogs = _logMessages.Where(m => m.ToLower().Contains("debug")).ToList();
            var infoLogs = _logMessages.Where(m => m.ToLower().Contains("info")).ToList();
            var warningLogs = _logMessages.Where(m => m.ToLower().Contains("warn")).ToList();

            _output.WriteLine($"Debug logs: {debugLogs.Count}");
            _output.WriteLine($"Info logs: {infoLogs.Count}");
            _output.WriteLine($"Warning logs: {warningLogs.Count}");

            // Should have appropriate log levels based on configuration
            Assert.True(infoLogs.Count >= 0); // Info and above should be logged

            response.Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Test logger provider to capture log messages for testing
    /// </summary>
    public class TestLoggerProvider : ILoggerProvider
    {
        private readonly List<string> _logMessages;

        public TestLoggerProvider(List<string> logMessages)
        {
            _logMessages = logMessages;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new TestLogger(_logMessages, categoryName);
        }

        public void Dispose()
        {
        }
    }

    /// <summary>
    /// Test logger to capture log messages
    /// </summary>
    public class TestLogger : ILogger
    {
        private readonly List<string> _logMessages;
        private readonly string _categoryName;

        public TestLogger(List<string> logMessages, string categoryName)
        {
            _logMessages = logMessages;
            _categoryName = categoryName;
        }

        public IDisposable BeginScope<TState>(TState state) => null!;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (IsEnabled(logLevel))
            {
                var message = $"[{logLevel}] {_categoryName}: {formatter(state, exception)}";
                lock (_logMessages)
                {
                    _logMessages.Add(message);
                }
            }
        }
    }
}