[CmdletBinding()]
param(
    [string]$TestProject = "tests/SolarEngine.Tests/SolarEngine.Tests.csproj",
    [string]$CoverageOutputPath = "artifacts/coverage/coverage.xml",
    [string]$TestFilter = ""
)

$ErrorActionPreference = "Stop"

$excludedCoverageFiles = @(
    "**/obj/**",
    "**/UI/*.cs",
    "**/Program.cs",
    "**/NativeApplication.cs",
    "**/Infrastructure/Deployment/SharedKernelStack.cs",
    "**/Features/*/DependencyInjection.cs",
    "**/Features/Locations/Infrastructure/WindowsLocationProvider.cs",
    "**/Features/SystemHost/ApplicationLifecycleOrchestrator.cs",
    "**/Features/SystemHost/Infrastructure/ConfigurationRepository.cs",
    "**/Features/SystemHost/Infrastructure/GcPressureMonitor.cs",
    "**/Features/SystemHost/Infrastructure/WindowsStartupRegistrar.cs",
    "**/Features/Themes/Infrastructure/WindowsRegistryThemeMutator.cs",
    "**/Features/Themes/ThemeTransitionOrchestrator.cs",
    "**/Features/Updates/Domain/InstallationMetadata.cs",
    "**/Features/Updates/Domain/UpdateStatusSnapshot.cs",
    "**/Features/Updates/Infrastructure/GitHubReleaseFeedClient.cs",
    "**/Features/Updates/Infrastructure/InstallationMetadataRepository.cs",
    "**/Features/Updates/Infrastructure/PersistedUpdateRequest.cs",
    "**/Features/Updates/UpdateCoordinator.cs",
    "**/Features/SolarCalculations/GetSolarScheduleQueryHandler.cs"
)

function Use-RepositoryDotNet {
    $repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $repositoryDotNetRoot = Join-Path $repositoryRoot ".dotnet"
    $repositoryDotNet = Join-Path $repositoryDotNetRoot "dotnet.exe"

    if (-not (Test-Path $repositoryDotNet)) {
        return
    }

    $env:DOTNET_ROOT = $repositoryDotNetRoot
    $env:PATH = "$repositoryDotNetRoot;$env:PATH"
}

Use-RepositoryDotNet

$resolvedCoverageOutputPath = [System.IO.Path]::GetFullPath($CoverageOutputPath)
$coverageDirectory = [System.IO.Path]::GetDirectoryName($resolvedCoverageOutputPath)
$collectorResultsDirectory = Join-Path $coverageDirectory "collector-results"
$null = New-Item -ItemType Directory -Force -Path $coverageDirectory

if (Test-Path $resolvedCoverageOutputPath) {
    Remove-Item -LiteralPath $resolvedCoverageOutputPath -Force
}

if (Test-Path $collectorResultsDirectory) {
    Remove-Item -LiteralPath $collectorResultsDirectory -Recurse -Force
}

$testArguments = @(
    "test",
    $TestProject,
    "--configuration",
    "Release",
    "--disable-build-servers",
    "-m:1",
    "--collect",
    "XPlat Code Coverage",
    "--results-directory",
    $collectorResultsDirectory,
    "/p:DebugType=portable",
    "/p:DebugSymbols=true"
)

if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
    $testArguments += @("--filter", $TestFilter)
}

$testArguments += @(
    "--",
    "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura",
    "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.IncludeTestAssembly=false",
    "DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.ExcludeByFile=$([string]::Join(',', $excludedCoverageFiles))"
)

& dotnet @testArguments
if ($LASTEXITCODE -ne 0) {
    throw "Coverage test run failed."
}

$coverageReport = Get-ChildItem -Path $collectorResultsDirectory -Recurse -Filter "coverage.cobertura.xml" |
    Sort-Object LastWriteTimeUtc -Descending |
    Select-Object -First 1

if ($null -eq $coverageReport) {
    throw "Coverage collector did not produce a cobertura report."
}

Copy-Item -LiteralPath $coverageReport.FullName -Destination $resolvedCoverageOutputPath -Force

if (-not (Test-Path $resolvedCoverageOutputPath)) {
    throw "Coverage output was not generated at $resolvedCoverageOutputPath."
}

[xml]$coverage = Get-Content -Raw $resolvedCoverageOutputPath

if ($coverage.coverage.'lines-valid' -eq "0") {
    throw "Coverage output was generated but did not contain instrumented lines."
}

$lineRate = [double]$coverage.coverage.'line-rate' * 100
$branchRate = [double]$coverage.coverage.'branch-rate' * 100
$linesCovered = [int]$coverage.coverage.'lines-covered'
$linesValid = [int]$coverage.coverage.'lines-valid'

Write-Host ("Coverage summary: line {0:N2}% ({1}/{2}), branch {3:N2}%." -f $lineRate, $linesCovered, $linesValid, $branchRate)
