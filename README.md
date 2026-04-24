# Auto Theme Solar Engine

A lightweight Windows system tray app that automatically switches your PC theme based on local sunrise and sunset times.

## Installation

Run this in a normal PowerShell session:

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Set-Location "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.07/auto-theme-solar-engine-win-x64-self-contained-v26.04.07.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.07.exe"
Move-Item -LiteralPath ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.07.exe" -Destination ".\AutoThemeSolarEngine.exe" -Force
Start-Process ".\AutoThemeSolarEngine.exe"
```

## Privacy

- Your location data never leaves your computer.
- Coordinates are encrypted using Windows Data Protection (DPAPI).
- Saved coordinates stay hidden on the main settings surface until you focus the fields again.
- No telemetry, no tracking, and no internet connection required for core functionality.
