# AutoThemeSolarEngine

[![Release](https://img.shields.io/github/v/release/humbertoschoenwald/auto-theme-solar-engine?display_name=tag&label=release)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest)
[![CI](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml)

A lightweight Windows system tray app that automatically switches your PC theme based on local sunrise and sunset times. No cloud requests, no bulky installers, and no background services.

[Leer en Español (LÉEME.md)](LÉEME.md)

## Why use it?

- **No Cloud Required:** Solar calculations run entirely on your device.
- **Privacy First:** Uses Windows location locally or manual coordinates. No external APIs.
- **Standalone Option:** Use the self-contained executable with no separate .NET install.
- **Smart Scheduling:** Correctly handles polar nights and the midnight sun.
- **Low Resource Usage:** Designed to stay in the system tray while using minimal RAM.

## How it works

The app runs in the notification area, calculates the solar schedule for your exact location, and switches Windows Light/Dark mode at the right time.

## Installation

The app keeps `AutoThemeSolarEngine.exe`, `config.json`, `installation.json`,
`AutoThemeSolarEngine.log`, and the update helper scripts inside
`LocalAppData\AutoThemeSolarEngine`. Pick the release flavor you want and run
the matching block in a normal PowerShell session.

Paste the whole block. If your PowerShell session leaves the pasted block
buffered, press Enter once to run it.

### LocalAppData (Recommended)

`LocalAppData` is the recommended per-user path and updates silently without
elevation.

Self-contained (Recommended):

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Set-Location "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.05/auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe"
Move-Item -LiteralPath ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe" -Destination ".\AutoThemeSolarEngine.exe" -Force
Start-Process ".\AutoThemeSolarEngine.exe"
```

After the app opens, enter your coordinates manually or allow Windows location
access.

## Privacy

- Your location data never leaves your computer.
- Coordinates are encrypted using Windows Data Protection (DPAPI).
- No telemetry, no tracking, and no internet connection required for core functionality.

## Highlights in this repository

- Targets .NET 11 preview 3 and C# 15 (preview).
- Uses strict CI on GitHub Actions with commitlint, cspell, build, analyzers, vulnerability scanning, and tests.
- Follows Windows app theme preference for the native settings window.
- Ships English and Spanish UI text from JSON localization resources.

## Coverage

Run `pwsh -NoLogo -NoProfile -File ./scripts/run-coverage.ps1` to generate
`artifacts/coverage/coverage.xml`.

The repository measures local line and branch coverage and writes the canonical
report to `artifacts/coverage/coverage.xml`. If you use VS Code, install
Coverage Gutters and point it at `artifacts/coverage/coverage.xml`. The bundled
workspace settings already do that for you.

The coverage gate intentionally excludes generated files, native Win32 UI glue,
DI bootstraps, and OS-bound orchestration that is validated by the heavier
local lane instead.
