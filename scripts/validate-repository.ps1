[CmdletBinding()]
param(
    [string]$SolutionPath = "SolarEngine.slnx",
    [string]$TestProject = "tests/SolarEngine.Tests/SolarEngine.Tests.csproj",
    [string]$TestResultsDirectory = "artifacts/test-results",
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

Invoke-Checked { dotnet restore $SolutionPath } "dotnet restore failed."
Invoke-Checked { dotnet build $SolutionPath --configuration Release --no-restore } "dotnet build failed."
Invoke-Checked { dotnet format $SolutionPath --verify-no-changes --severity error --no-restore } "dotnet format verification failed."

& ./.github/scripts/run-dependency-vulnerability-check.ps1 -ProjectPath $SolutionPath
if ($LASTEXITCODE -ne 0) {
    throw "Dependency vulnerability check failed."
}

$null = New-Item -ItemType Directory -Force -Path $TestResultsDirectory
Invoke-Checked {
    dotnet test $TestProject `
        --configuration Release `
        --no-build `
        --logger "trx;LogFileName=SolarEngine.Tests.trx" `
        --results-directory $TestResultsDirectory
} "dotnet test failed."
