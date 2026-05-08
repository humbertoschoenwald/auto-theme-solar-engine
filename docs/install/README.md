# PowerShell Install Commands

The repository keeps installation guidance outside the root README so the
product overview stays short while the update model remains explicit.

## LocalAppData Install

Use the per-user entrypoint when the app should live under the current user's
profile. This is the recommended install mode. Use a normal PowerShell session.

Paste the whole block. If your PowerShell session leaves the pasted block
buffered, press Enter once to run it.

Self-contained (Recommended):

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Set-Location "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.05.02/auto-theme-solar-engine-win-x64-self-contained-v26.05.02.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.05.02.exe"
Move-Item -LiteralPath ".\auto-theme-solar-engine-win-x64-self-contained-v26.05.02.exe" -Destination ".\AutoThemeSolarEngine.exe" -Force
Start-Process ".\AutoThemeSolarEngine.exe"
```

## Notes

- The install directory is `%LocalAppData%\AutoThemeSolarEngine`.
- The same directory keeps `AutoThemeSolarEngine.exe`, `config.json`,
  `installation.json`, `AutoThemeSolarEngine.log`, and
  `Apply-SolarEngine-Update.ps1`.
- The updater uses `installation.json` in the install directory to keep future
  automatic updates on the same release flavor, install mode, and installed
  executable path chosen by the user.
- When the install directory is writable, the updater stages the newer
  executable beside the current install. If it is not writable, the updater may
  stage under the app-owned LocalAppData update area before the visible helper
  performs the final move.
