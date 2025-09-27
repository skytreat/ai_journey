# IPAM Integration Tests

This test suite provides comprehensive integration testing for the Phase 1 and Phase 2 enhancements to the IPAM system.

## Test Coverage

### 🔧 **API Gateway Integration Tests** (`ApiGatewayIntegrationTests.cs`)
- **Resilience Patterns**: Tests retry policies and circuit breaker functionality
- **Health Checks**: Validates API Gateway health endpoint
- **Correlation IDs**: Ensures request correlation tracking works
- **Header Forwarding**: Verifies proper header management
- **Error Handling**: Tests graceful error responses with correlation IDs
- **Rate Limiting**: Validates rate limiting functionality
- **Security**: Ensures security headers are not exposed

### 🏥 **Health Checks Integration Tests** (`HealthChecksIntegrationTests.cs`)
- **Multiple Endpoints**: Tests `/health`, `/health/ready`, and `/health/live`
- **JSON Format**: Validates health check response format
- **Component Details**: Verifies individual component health reporting
- **DataAccess Health**: Tests data layer health monitoring
- **Memory Health**: Validates memory usage monitoring
- **Response Time**: Ensures health checks are fast
- **Concurrent Requests**: Tests health check stability under load

### 🎮 **Enhanced Controller Integration Tests** (`EnhancedControllerIntegrationTests.cs`)
- **Input Validation**: Tests enhanced validation for IP prefixes and tags
- **Error Handling**: Validates improved error responses
- **CIDR Format Support**: Tests various IPv4 and IPv6 CIDR formats
- **Concurrent Processing**: Ensures thread-safe request handling
- **Security Headers**: Validates security header implementation
- **Correlation ID Maintenance**: Tests request tracing

### 💾 **Caching Integration Tests** (`CachingIntegrationTests.cs`)
- **Memory Cache Configuration**: Verifies cache setup
- **Response Caching**: Tests HTTP response caching
- **High Frequency Requests**: Validates cache performance under load
- **Cache Expiration**: Tests cache invalidation (where possible)
- **Parameter-based Caching**: Ensures different parameters create separate cache entries
- **Redis Fallback**: Tests fallback to memory cache when Redis unavailable
- **Performance Impact**: Measures caching effectiveness

### 📋 **Logging Integration Tests** (`LoggingIntegrationTests.cs`)
- **Request Logging**: Validates request/response logging
- **Correlation ID Tracking**: Tests correlation ID propagation through logs
- **Error Logging**: Ensures errors are properly logged
- **High Volume Handling**: Tests logging under concurrent load
- **Performance Logging**: Validates timing capture
- **Structured Logging**: Tests log property capture
- **Security**: Ensures sensitive data is not logged
- **Log Level Filtering**: Validates log level configuration

### 📊 **API Versioning Integration Tests** (`ApiVersioningIntegrationTests.cs`)
- **Version Support**: Tests header, query string, and default versioning
- **Multiple Versions**: Validates various version format handling
- **Response Compression**: Tests Gzip/Brotli compression
- **Security Headers**: Validates OWASP recommended headers
- **CORS Support**: Tests cross-origin request handling
- **OPTIONS Requests**: Validates preflight request handling
- **Content Negotiation**: Tests media type handling

### ⚡ **Performance Integration Tests** (`PerformanceIntegrationTests.cs`)
- **Response Time**: Measures single request performance
- **Concurrent Throughput**: Tests system under concurrent load
- **Memory Usage**: Monitors memory consumption under load
- **Caching Effectiveness**: Measures cache performance impact
- **Error Handling Performance**: Tests error response speed
- **Health Check Performance**: Validates health endpoint speed
- **Stability Testing**: Long-running stability under continuous load

## Running the Tests

### Prerequisites
- .NET 8 SDK
- Azure Storage Emulator (or Azurite) running for integration tests
- Visual Studio 2022 or VS Code with C# extension

### Command Line
```bash
# Run all integration tests
dotnet test tests/Ipam.IntegrationTests/

# Run specific test class
dotnet test tests/Ipam.IntegrationTests/ --filter "FullyQualifiedName~ApiGatewayIntegrationTests"

# Run with detailed output
dotnet test tests/Ipam.IntegrationTests/ --logger "console;verbosity=detailed"

# Run with coverage
dotnet test tests/Ipam.IntegrationTests/ --collect:"XPlat Code Coverage"
```

### Visual Studio
1. Open the IPAM.sln solution
2. Build the solution (Ctrl+Shift+B)
3. Open Test Explorer (Test → Test Explorer)
4. Click "Run All Tests" or run individual test classes

## Test Configuration

### Environment Variables
Tests use these configuration values:
- `ConnectionStrings:AzureTableStorage`: "UseDevelopmentStorage=true"
- `Caching:Enabled`: "true"
- `Caching:DurationMinutes`: "1" (short duration for testing)

### Test Dependencies
- **Microsoft.AspNetCore.Mvc.Testing**: For integration testing
- **Microsoft.Extensions.Logging.Testing**: For log verification
- **xUnit**: Test framework
- **Moq**: Mocking framework
- **FluentAssertions**: Assertion library

## Performance Benchmarks

### Expected Performance Targets
- **Single Request**: < 5 seconds response time
- **Concurrent Requests**: Handle 10+ concurrent requests
- **Health Checks**: < 2 seconds average response time
- **Memory Usage**: < 100MB increase under load
- **Error Rate**: < 10% under stress conditions
- **Throughput**: Reasonable requests/second based on system capacity

### Performance Test Results
The performance tests provide metrics on:
- Average response times
- Throughput (requests/second)
- Memory consumption
- Error rates
- Cache effectiveness
- System stability over time

## Troubleshooting

### Common Issues

1. **Azure Storage Emulator Not Running**
   ```
   Error: Connection refused to storage emulator
   Solution: Start Azurite or Azure Storage Emulator
   ```

2. **Port Conflicts**
   ```
   Error: Port already in use
   Solution: Check for conflicting services on ports 5000-5001
   ```

3. **Memory Issues in Performance Tests**
   ```
   Error: OutOfMemoryException
   Solution: Reduce concurrent request count or test duration
   ```

### Debug Tips
- Use `--logger "console;verbosity=detailed"` for detailed test output
- Check test output for correlation IDs to trace specific requests
- Monitor Windows Performance Counters during performance tests
- Use Azure Storage Explorer to verify test data

## Contributing

When adding new integration tests:

1. **Follow Naming Convention**: `[Feature]IntegrationTests.cs`
2. **Use ITestOutputHelper**: For detailed test logging
3. **Clean Up Resources**: Dispose HttpClient and responses
4. **Assert Meaningful Values**: Don't just check for non-error status codes
5. **Add Performance Metrics**: Include timing measurements where relevant
6. **Document Expected Behavior**: Add comments explaining test scenarios

## Test Categories

Tests are organized by functional area:
- 🔧 **Infrastructure**: API Gateway, Health Checks
- 🎮 **Application**: Controllers, Business Logic
- 💾 **Data**: Caching, Storage
- 📋 **Observability**: Logging, Monitoring
- 📊 **API Features**: Versioning, Compression, Security
- ⚡ **Performance**: Load, Stress, Stability

Each test class focuses on specific Phase 1 or Phase 2 enhancements to ensure comprehensive coverage of the improved functionality.