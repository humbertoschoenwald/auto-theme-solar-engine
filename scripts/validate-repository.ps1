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

function Use-RepositoryCsWinRTMetadata {
    if ($env:CsWinRTWindowsMetadata) {
        return
    }

    $repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
    $metadataRoot = Join-Path $repositoryRoot "dependencies/windows-sdk-net-ref"

    if (-not (Test-Path $metadataRoot)) {
        return
    }

    $candidate = Get-ChildItem -LiteralPath $metadataRoot -Directory |
        Sort-Object -Property Name -Descending |
        ForEach-Object { Join-Path $_.FullName "winmd" } |
        Where-Object { Test-Path $_ } |
        Select-Object -First 1

    if ($candidate) {
        $env:CsWinRTWindowsMetadata = $candidate
    }
}

function Resolve-VsWherePath {
    $candidatePaths = @()

    if (${env:ProgramFiles(x86)}) {
        $candidatePaths += Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    }

    if ($env:ProgramFiles) {
        $candidatePaths += Join-Path $env:ProgramFiles "Microsoft Visual Studio\Installer\vswhere.exe"
    }

    foreach ($candidatePath in $candidatePaths) {
        if (Test-Path $candidatePath) {
            return $candidatePath
        }
    }

    return $null
}

function Use-VisualCppBuildEnvironment {
    if (-not $IsWindows) {
        return
    }

    if (Get-Command link.exe -ErrorAction SilentlyContinue) {
        return
    }

    $vswhere = Resolve-VsWherePath
    if (-not $vswhere) {
        throw "Visual Studio Build Tools with the C++ workload is required for Native AOT publish validation."
    }

    $installationPath = & $vswhere `
        -latest `
        -products * `
        -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 `
        -property installationPath

    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($installationPath)) {
        throw "Visual Studio Build Tools with the C++ workload was not found."
    }

    $vcvarsPath = Join-Path $installationPath "VC\Auxiliary\Build\vcvars64.bat"
    if (-not (Test-Path $vcvarsPath)) {
        throw "vcvars64.bat was not found in the Visual Studio Build Tools installation."
    }

    $environmentOutput = & cmd.exe /d /s /c "`"$vcvarsPath`" >nul && set"
    if ($LASTEXITCODE -ne 0) {
        throw "Loading the Visual C++ build environment failed."
    }

    foreach ($line in $environmentOutput) {
        $separatorIndex = $line.IndexOf("=")
        if ($separatorIndex -le 0) {
            continue
        }

        $name = $line.Substring(0, $separatorIndex)
        $value = $line.Substring($separatorIndex + 1)
        [Environment]::SetEnvironmentVariable($name, $value, "Process")
    }

    if (-not (Get-Command link.exe -ErrorAction SilentlyContinue)) {
        throw "link.exe was not available after loading the Visual C++ build environment."
    }
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
Use-RepositoryCsWinRTMetadata

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
    Use-VisualCppBuildEnvironment

    Invoke-Checked {
        dotnet publish $AppProject `
            --configuration Release `
            --runtime win-x64 `
            --self-contained true `
            --output artifacts/publish-smoke/self-contained-aot `
            -m:1 `
            --disable-build-servers `
            /p:UseNativeAot=true
    } "Self-contained Native AOT publish smoke failed."
}
