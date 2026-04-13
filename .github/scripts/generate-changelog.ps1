[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$CurrentTag,

    [string]$ChangeLogPath = "CHANGELOG.md",

    [string]$ReleaseNotesPath = ".github/release-notes.md"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path "scripts/generate-changelog.mjs")) {
    throw "scripts/generate-changelog.mjs was not found."
}

node ./scripts/generate-changelog.mjs --tag $CurrentTag --changelog $ChangeLogPath --release-notes $ReleaseNotesPath

if ($LASTEXITCODE -ne 0) {
    throw "Changelog generation failed."
}
