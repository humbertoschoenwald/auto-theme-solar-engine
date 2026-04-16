[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [string]$SourceExecutablePath,

    [switch]$LaunchAfterInstall
)

$installScriptPath = Join-Path $PSScriptRoot "Install-SolarEngine.ps1"

& $installScriptPath `
    -Mode "program-files" `
    -SourceExecutablePath $SourceExecutablePath `
    -LaunchAfterInstall:$LaunchAfterInstall `
    -WhatIf:$WhatIfPreference
