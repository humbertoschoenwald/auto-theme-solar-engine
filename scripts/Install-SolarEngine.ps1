[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("local-app-data", "program-files")]
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

    if ($FileName.Contains("framework-dependent", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "framework-dependent"
    }

    if ($FileName.Contains("self-contained", [System.StringComparison]::OrdinalIgnoreCase)) {
        return "self-contained"
    }

    throw "The executable name must include either 'self-contained' or 'framework-dependent'."
}

function Resolve-ShellPath {
    $shellCommand = Get-Command pwsh -ErrorAction SilentlyContinue
    if ($shellCommand) {
        return $shellCommand.Source
    }

    $powershellCommand = Get-Command powershell.exe -ErrorAction SilentlyContinue
    if ($powershellCommand) {
        return $powershellCommand.Source
    }

    throw "No PowerShell executable is available to register the update task."
}

function Assert-ProgramFilesInstallAllowed {
    $principal = [System.Security.Principal.WindowsPrincipal]::new(
        [System.Security.Principal.WindowsIdentity]::GetCurrent())

    if (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
        throw "Program Files installation requires an elevated PowerShell session."
    }
}

function Register-ElevatedUpdateTask {
    param(
        [Parameter(Mandatory = $true)]
        [string]$TaskName,

        [Parameter(Mandatory = $true)]
        [string]$HelperScriptPath
    )

    $shellPath = Resolve-ShellPath
    $userId = [System.Security.Principal.WindowsIdentity]::GetCurrent().Name
    $arguments = @(
        "-NoLogo",
        "-NoProfile",
        "-NonInteractive",
        "-ExecutionPolicy", "Bypass",
        "-WindowStyle", "Hidden",
        "-File", ('"{0}"' -f $HelperScriptPath)
    ) -join " "

    $action = New-ScheduledTaskAction -Execute $shellPath -Argument $arguments
    $principal = New-ScheduledTaskPrincipal -UserId $userId -LogonType Interactive -RunLevel Highest
    $settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable
    $task = New-ScheduledTask -Action $action -Principal $principal -Settings $settings

    Register-ScheduledTask -TaskName $TaskName -InputObject $task -Force | Out-Null
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

$installDirectory = if ($Mode -eq "program-files") {
    Join-Path ${env:ProgramFiles} "Auto Theme — Solar Engine"
}
else {
    Join-Path $env:LOCALAPPDATA "Auto Theme — Solar Engine"
}

$installedExecutablePath = Join-Path $installDirectory "AutoThemeSolarEngine.exe"
$manifestPath = Join-Path $installDirectory "installation.json"
$helperScriptPath = Join-Path $env:LOCALAPPDATA "AutoThemeSolarEngine\Apply-SolarEngine-Update.ps1"
$elevatedTaskName = if ($Mode -eq "program-files") {
    "Auto Theme Solar Engine Silent Update"
}
else {
    $null
}

if ($Mode -eq "program-files" -and -not $WhatIfPreference) {
    Assert-ProgramFilesInstallAllowed
}

if ($PSCmdlet.ShouldProcess($installDirectory, "Create install directory")) {
    New-Item -ItemType Directory -Path $installDirectory -Force | Out-Null
}

if ($PSCmdlet.ShouldProcess($installedExecutablePath, "Copy executable into the install directory")) {
    Copy-Item -LiteralPath $resolvedSourceExecutable -Destination $installedExecutablePath -Force
}

if ($Mode -eq "program-files" -and $PSCmdlet.ShouldProcess($elevatedTaskName, "Register elevated update task")) {
    Register-ElevatedUpdateTask -TaskName $elevatedTaskName -HelperScriptPath $helperScriptPath
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
