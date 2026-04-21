[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("local-app-data")]
    [string]$Mode,

    [Parameter(Mandatory = $true)]
    [string]$SourceExecutablePath,

    [switch]$LaunchAfterInstall
)

$ErrorActionPreference = "Stop"

function Resolve-ReleaseFlavor {
    param(
        [Parameter(Mandatory = $true)]
        [string]$FileName
    )

    if ($FileName.Contains("self-contained", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "self-contained"
    }

    throw "The executable name must include 'self-contained'."
}

function Write-InstallationManifest {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$ReleaseFlavor,

        [Parameter(Mandatory = $true)]
        [string]$InstallationMode,

        [string]$ElevatedTaskName
    )

    $manifest = @{
        InstalledExecutableName = "AutoThemeSolarEngine.exe"
        ReleaseFlavor = $ReleaseFlavor
        InstallationMode = $InstallationMode
        ElevatedTaskName = $ElevatedTaskName
    }

    $json = $manifest | ConvertTo-Json -Depth 3
    Set-Content -LiteralPath $Path -Value $json -Encoding utf8NoBOM
}

$resolvedSourceExecutable = (Resolve-Path -LiteralPath $SourceExecutablePath).Path
$sourceFileName = [System.IO.Path]::GetFileName($resolvedSourceExecutable)
$releaseFlavor = Resolve-ReleaseFlavor -FileName $sourceFileName

$installDirectory = Join-Path $env:LOCALAPPDATA "AutoThemeSolarEngine"

$installedExecutablePath = Join-Path $installDirectory "AutoThemeSolarEngine.exe"
$manifestPath = Join-Path $installDirectory "installation.json"
$elevatedTaskName = $null

if ($PSCmdlet.ShouldProcess($installDirectory, "Create install directory")) {
    New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
}

if ($PSCmdlet.ShouldProcess($installedExecutablePath, "Copy executable into the install directory")) {
    Copy-Item -LiteralPath $resolvedSourceExecutable -Destination $installedExecutablePath -Force
}

if ($PSCmdlet.ShouldProcess($manifestPath, "Write installation metadata")) {
    Write-InstallationManifest `
        -Path $manifestPath `
        -ReleaseFlavor $releaseFlavor `
        -InstallationMode $Mode `
        -ElevatedTaskName $elevatedTaskName
}

if ($LaunchAfterInstall -and $PSCmdlet.ShouldProcess($installedExecutablePath, "Launch the installed application")) {
    Start-Process -FilePath $installedExecutablePath
}
