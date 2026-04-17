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
  Select-String -Path $file.FullName -Pattern '\[\s*DllImport\s*\('
}

if ($matches) {
  Write-Host "DllImport is forbidden in repository source. Use LibraryImport for authored P/Invoke declarations." -ForegroundColor Red
  foreach ($match in $matches) {
    Write-Host ("{0}:{1}: {2}" -f $match.Path, $match.LineNumber, $match.Line.Trim())
  }

  exit 1
}

Write-Host "No DllImport usage found in src/ or tests/."
