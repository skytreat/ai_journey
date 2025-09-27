# IPAM Integration Tests - Comprehensive Test Runner
# This script runs all Phase 1 & 2 enhancement validation tests

param(
    [switch]$SkipPerformance,
    [switch]$SkipCoverage,
    [string]$Filter = "",
    [switch]$Verbose
)

Write-Host "🚀 IPAM Integration Tests - Phase 1 & 2 Validation" -ForegroundColor Green
Write-Host "=========================================" -ForegroundColor Green

# Check prerequisites
Write-Host "Checking prerequisites..." -ForegroundColor Yellow

# Check .NET SDK
try {
    $dotnetVersion = dotnet --version
    Write-Host "✅ .NET SDK: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ .NET SDK not found. Please install .NET 8 SDK." -ForegroundColor Red
    exit 1
}

# Check if solution builds
Write-Host "Building solution..." -ForegroundColor Yellow
$buildResult = dotnet build ../../IPAM.sln --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed. Please fix build errors first." -ForegroundColor Red
    exit 1
}
Write-Host "✅ Solution built successfully" -ForegroundColor Green

# Setup test environment
Write-Host "Setting up test environment..." -ForegroundColor Yellow

# Check for Azure Storage Emulator / Azurite
$azuriteRunning = $false
try {
    $response = Invoke-WebRequest -Uri "http://127.0.0.1:10000" -TimeoutSec 2 -ErrorAction SilentlyContinue
    $azuriteRunning = $true
    Write-Host "✅ Azure Storage Emulator/Azurite is running" -ForegroundColor Green
} catch {
    Write-Host "⚠️  Azure Storage Emulator/Azurite not detected" -ForegroundColor Yellow
    Write-Host "   Tests will use development storage settings" -ForegroundColor Yellow
}

# Configure test settings
$env:ASPNETCORE_ENVIRONMENT = "Testing"
$env:ConnectionStrings__AzureTableStorage = "UseDevelopmentStorage=true"
$env:Caching__Enabled = "true"
$env:Caching__DurationMinutes = "1"

Write-Host "✅ Test environment configured" -ForegroundColor Green

# Prepare test execution
$testProject = "tests/Ipam.IntegrationTests/Ipam.IntegrationTests.csproj"
$verbosityLevel = if ($Verbose) { "detailed" } else { "normal" }
$testCategories = @(
    @{ Name = "Infrastructure"; Filter = "ApiGateway|HealthChecks"; Description = "API Gateway and Health Checks" },
    @{ Name = "Application"; Filter = "EnhancedController"; Description = "Enhanced Controllers and Validation" },
    @{ Name = "Caching"; Filter = "Caching"; Description = "Distributed Caching and Redis" },
    @{ Name = "Logging"; Filter = "Logging"; Description = "Structured Logging and Correlation" },
    @{ Name = "API Features"; Filter = "ApiVersioning"; Description = "Versioning, Compression, Security" },
    @{ Name = "Performance"; Filter = "Performance"; Description = "Load Testing and Benchmarks" }
)

$totalTests = 0
$passedTests = 0
$failedTests = 0
$testResults = @()

Write-Host ""
Write-Host "🧪 Running Integration Tests" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Run tests by category
foreach ($category in $testCategories) {
    if ($SkipPerformance -and $category.Name -eq "Performance") {
        Write-Host "⏭️  Skipping Performance tests (--SkipPerformance flag)" -ForegroundColor Yellow
        continue
    }

    if ($Filter -and $category.Filter -notmatch $Filter) {
        continue
    }

    Write-Host ""
    Write-Host "🔍 Running $($category.Name) Tests: $($category.Description)" -ForegroundColor Blue
    Write-Host "Filter: $($category.Filter)" -ForegroundColor Gray

    $testFilter = "--filter `"FullyQualifiedName~$($category.Filter)`""
    $loggerOption = "--logger `"console;verbosity=$verbosityLevel`""
    
    $startTime = Get-Date
    
    if ($SkipCoverage) {
        $result = cmd /c "dotnet test $testProject $testFilter $loggerOption 2>&1"
    } else {
        $result = cmd /c "dotnet test $testProject $testFilter $loggerOption --collect:`"XPlat Code Coverage`" 2>&1"
    }
    
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalSeconds

    $categoryResult = @{
        Category = $category.Name
        Duration = $duration
        Success = $LASTEXITCODE -eq 0
        Output = $result
    }

    $testResults += $categoryResult

    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ $($category.Name) tests passed in $([math]::Round($duration, 2))s" -ForegroundColor Green
        $passedTests++
    } else {
        Write-Host "❌ $($category.Name) tests failed in $([math]::Round($duration, 2))s" -ForegroundColor Red
        $failedTests++
        
        # Show failure details
        Write-Host "Failure details:" -ForegroundColor Red
        $result | Where-Object { $_ -match "Failed|Error|Exception" } | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Red
        }
    }
    
    $totalTests++
}

# Generate summary report
Write-Host ""
Write-Host "📊 Test Execution Summary" -ForegroundColor Magenta
Write-Host "=========================" -ForegroundColor Magenta

$totalDuration = ($testResults | Measure-Object -Property Duration -Sum).Sum

Write-Host "Total Categories: $totalTests"
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor $(if ($failedTests -gt 0) { "Red" } else { "Green" })
Write-Host "Total Duration: $([math]::Round($totalDuration, 2)) seconds"
Write-Host "Success Rate: $([math]::Round(($passedTests / $totalTests) * 100, 1))%"

# Detailed results
Write-Host ""
Write-Host "📋 Detailed Results" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan

foreach ($result in $testResults) {
    $status = if ($result.Success) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($result.Success) { "Green" } else { "Red" }
    
    Write-Host "$status $($result.Category) ($([math]::Round($result.Duration, 2))s)" -ForegroundColor $color
}

# Performance metrics (if performance tests were run)
$performanceResult = $testResults | Where-Object { $_.Category -eq "Performance" }
if ($performanceResult -and $performanceResult.Success) {
    Write-Host ""
    Write-Host "⚡ Performance Highlights" -ForegroundColor Yellow
    Write-Host "========================" -ForegroundColor Yellow
    
    # Extract performance metrics from test output
    $performanceOutput = $performanceResult.Output | Where-Object { 
        $_ -match "response time|throughput|memory|requests/second" 
    }
    
    if ($performanceOutput) {
        $performanceOutput | ForEach-Object {
            Write-Host "  $_" -ForegroundColor Yellow
        }
    } else {
        Write-Host "  Performance tests completed successfully" -ForegroundColor Yellow
    }
}

# Coverage report (if enabled)
if (-not $SkipCoverage -and $passedTests -gt 0) {
    Write-Host ""
    Write-Host "📈 Code Coverage" -ForegroundColor Blue
    Write-Host "===============" -ForegroundColor Blue
    
    $coverageFiles = Get-ChildItem -Path "tests/Ipam.IntegrationTests/TestResults" -Filter "coverage.cobertura.xml" -Recurse -ErrorAction SilentlyContinue
    
    if ($coverageFiles.Count -gt 0) {
        Write-Host "Coverage files generated: $($coverageFiles.Count)"
        Write-Host "Location: $($coverageFiles[0].DirectoryName)"
        Write-Host "Use reportgenerator tool for detailed HTML reports"
    } else {
        Write-Host "No coverage files found"
    }
}

# Final status
Write-Host ""
if ($failedTests -eq 0) {
    Write-Host "🎉 All Integration Tests Passed!" -ForegroundColor Green
    Write-Host "✅ Phase 1 & 2 enhancements are working correctly" -ForegroundColor Green
    Write-Host "🚀 System is ready for deployment" -ForegroundColor Green
    exit 0
} else {
    Write-Host "❌ Some Integration Tests Failed" -ForegroundColor Red
    Write-Host "⚠️  Please review and fix failing tests before deployment" -ForegroundColor Yellow
    exit 1
}