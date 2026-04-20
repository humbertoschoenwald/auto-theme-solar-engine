[CmdletBinding()]
param(
    [string]$SolutionPath = "SolarEngine.slnx",
    [string]$AppProject = "src/SolarEngine/SolarEngine.csproj",
    [string]$TestProject = "tests/SolarEngine.Tests/SolarEngine.Tests.csproj",
    [string]$TestResultsDirectory = "artifacts/test-results",
    [string]$CoverageOutputPath = "artifacts/coverage/coverage.xml",
    [string]$TestFilter = "",
    [switch]$SkipCoverage,
    [switch]$SkipPublishSmoke,
    [switch]$SkipNodeInstall
)

$ErrorActionPreference = "Stop"

function Invoke-Checked {
    param(
        [Parameter(Mandatory = $true)]
        [scriptblock]$Command,
        [Parameter(Mandatory = $true)]
        [string]$FailureMessage
    )

    & $Command
    if ($LASTEXITCODE -ne 0) {
        throw $FailureMessage
    }
}

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

function Resolve-PnpmCommand {
    if ($env:PNPM_BIN) {
        return $env:PNPM_BIN
    }

    $resolvedCommand = Get-Command pnpm -ErrorAction SilentlyContinue
    if ($resolvedCommand) {
        return $resolvedCommand.Source
    }

    $candidatePaths = @(
        (Join-Path $env:APPDATA "npm\pnpm.cmd"),
        (Join-Path $env:APPDATA "npm\pnpm")
    )

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path $candidatePath) {
            return $candidatePath
        }
    }

    throw "pnpm is required to validate the repository."
}

Use-RepositoryDotNet

$pnpm = Resolve-PnpmCommand

if (-not $SkipNodeInstall -and (Test-Path "package.json")) {
    Invoke-Checked { & $pnpm install --frozen-lockfile } "pnpm install --frozen-lockfile failed."
}

Invoke-Checked { & $pnpm lint } "Repository lint failed."

Invoke-Checked { dotnet restore $SolutionPath -m:1 --disable-build-servers } "dotnet restore failed."
Invoke-Checked { dotnet build $SolutionPath --configuration Release --no-restore -m:1 --disable-build-servers } "dotnet build failed."
Invoke-Checked { dotnet format $SolutionPath --verify-no-changes --severity error --no-restore } "dotnet format verification failed."

& ./.github/scripts/run-dependency-vulnerability-check.ps1 -ProjectPath $SolutionPath
if ($LASTEXITCODE -ne 0) {
    throw "Dependency vulnerability check failed."
}

$null = New-Item -ItemType Directory -Force -Path $TestResultsDirectory

if ($SkipCoverage) {
    $testArguments = @(
        "test",
        $TestProject,
        "--configuration",
        "Release",
        "--no-build",
        "--logger",
        "trx;LogFileName=SolarEngine.Tests.trx",
        "--results-directory",
        $TestResultsDirectory
    )

    if (-not [string]::IsNullOrWhiteSpace($TestFilter)) {
        $testArguments += @("--filter", $TestFilter)
    }

    $testArguments += "--disable-build-servers"
    Invoke-Checked { & dotnet @testArguments } "dotnet test failed."
}
else {
    Invoke-Checked {
        & $PSScriptRoot\run-coverage.ps1 `
            -TestProject $TestProject `
            -CoverageOutputPath $CoverageOutputPath `
            -TestFilter $TestFilter
    } "Coverage generation failed."
}

if (-not $SkipPublishSmoke) {
    Invoke-Checked {
        dotnet publish $AppProject `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output artifacts/publish-smoke/self-contained `
            --no-restore `
            -m:1 `
            --disable-build-servers
    } "Self-contained publish smoke failed."

    Invoke-Checked {
        dotnet publish $AppProject `
            --configuration Release `
            --runtime win-x64 `
            --self-contained false `
            --output artifacts/publish-smoke/framework-dependent `
            --no-restore `
            -m:1 `
            --disable-build-servers `
            /p:EnableCompressionInSingleFile=false
    } "Framework-dependent publish smoke failed."
}
