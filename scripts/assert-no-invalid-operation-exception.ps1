$ErrorActionPreference = "Stop"

$workspace = Split-Path -Parent $PSScriptRoot
$roots = @(
  (Join-Path $workspace "src"),
  (Join-Path $workspace "tests")
)

$files = foreach ($root in $roots) {
  if (Test-Path -LiteralPath $root) {
    Get-ChildItem -LiteralPath $root -Recurse -Filter *.cs -File |
      Where-Object {
        $_.FullName -notmatch '\\(bin|obj)\\'
      }
  }
}

$matches = foreach ($file in $files) {
  Select-String -Path $file.FullName -Pattern '\bInvalidOperationException\b'
}

if ($matches) {
  Write-Host "InvalidOperationException is forbidden in repository source. Use Result<T>, a more specific framework exception, or UnexpectedStateException." -ForegroundColor Red
  foreach ($match in $matches) {
    Write-Host ("{0}:{1}: {2}" -f $match.Path, $match.LineNumber, $match.Line.Trim())
  }

  exit 1
}

Write-Host "No InvalidOperationException usage found in src/ or tests/."
