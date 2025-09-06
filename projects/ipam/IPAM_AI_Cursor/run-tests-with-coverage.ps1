# PowerShell script to run tests with code coverage
param(
    [string]$Configuration = "Debug",
    [string]$OutputPath = "TestResults",
    [int]$MinimumCoverage = 90
)

Write-Host "🧪 Running IPAM System Unit Tests with Code Coverage" -ForegroundColor Green
Write-Host "Configuration: $Configuration" -ForegroundColor Yellow
Write-Host "Output Path: $OutputPath" -ForegroundColor Yellow
Write-Host "Minimum Coverage: $MinimumCoverage%" -ForegroundColor Yellow
Write-Host ""

# Ensure output directory exists
if (!(Test-Path $OutputPath)) {
    New-Item -ItemType Directory -Path $OutputPath -Force | Out-Null
    Write-Host "✅ Created output directory: $OutputPath" -ForegroundColor Green
}

# Change to src directory
Push-Location src

try {
    Write-Host "🔧 Building solution..." -ForegroundColor Cyan
    dotnet build IPAM.sln -c $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed"
    }
    Write-Host "✅ Build completed successfully" -ForegroundColor Green
    Write-Host ""

    Write-Host "🧪 Running tests with code coverage..." -ForegroundColor Cyan
    $testCommand = @(
        "test",
        "Domain.Tests/Domain.Tests.csproj",
        "-c", $Configuration,
        "--no-build",
        "--no-restore",
        "--settings", "Domain.Tests/coverlet.runsettings",
        "--collect:`"XPlat Code Coverage`"",
        "--results-directory", "../$OutputPath",
        "--logger", "trx",
        "--logger", "console;verbosity=normal"
    )

    & dotnet @testCommand
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed"
    }

    Write-Host "✅ Tests completed successfully" -ForegroundColor Green
    Write-Host ""

    # Find the coverage file
    $coverageFiles = Get-ChildItem -Path "../$OutputPath" -Recurse -Filter "coverage.cobertura.xml"
    if ($coverageFiles.Count -eq 0) {
        Write-Warning "⚠️  No coverage file found"
        return
    }

    $latestCoverageFile = $coverageFiles | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    Write-Host "📊 Coverage file: $($latestCoverageFile.FullName)" -ForegroundColor Cyan

    # Parse coverage percentage (simplified - would need XML parsing for accurate results)
    $coverageContent = Get-Content $latestCoverageFile.FullName -Raw
    if ($coverageContent -match 'line-rate="([0-9.]+)"') {
        $coverageRate = [double]$matches[1]
        $coveragePercentage = [math]::Round($coverageRate * 100, 2)
        
        Write-Host ""
        Write-Host "📈 Code Coverage Results:" -ForegroundColor Magenta
        Write-Host "Coverage: $coveragePercentage%" -ForegroundColor $(if ($coveragePercentage -ge $MinimumCoverage) { "Green" } else { "Red" })
        Write-Host "Target: $MinimumCoverage%" -ForegroundColor Yellow
        
        if ($coveragePercentage -ge $MinimumCoverage) {
            Write-Host "🎉 Coverage target achieved!" -ForegroundColor Green
        } else {
            Write-Host "❌ Coverage below target" -ForegroundColor Red
            $shortfall = $MinimumCoverage - $coveragePercentage
            Write-Host "Need to improve by $shortfall percentage points" -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "📁 Test Results:" -ForegroundColor Magenta
    $trxFiles = Get-ChildItem -Path "../$OutputPath" -Filter "*.trx" | Sort-Object LastWriteTime -Descending
    foreach ($trx in $trxFiles) {
        Write-Host "  📄 $($trx.Name)" -ForegroundColor Gray
    }

    $htmlReports = Get-ChildItem -Path "../$OutputPath" -Recurse -Filter "index.html"
    if ($htmlReports.Count -gt 0) {
        $latestHtmlReport = $htmlReports | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        Write-Host ""
        Write-Host "🌐 HTML Coverage Report:" -ForegroundColor Magenta
        Write-Host "  📄 $($latestHtmlReport.FullName)" -ForegroundColor Gray
        Write-Host "  Open in browser: file:///$($latestHtmlReport.FullName.Replace('\', '/'))" -ForegroundColor Cyan
    }

} catch {
    Write-Host "❌ Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
} finally {
    Pop-Location
}

Write-Host ""
Write-Host "✨ Test execution completed!" -ForegroundColor Green
