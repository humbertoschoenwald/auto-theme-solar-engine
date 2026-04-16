# Auto Theme — Solar Engine

[![Release](https://img.shields.io/github/v/release/humbertoschoenwald/auto-theme-solar-engine?display_name=tag&label=release)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest)
[![CI](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml)

A lightweight Windows system tray app that automatically switches your PC theme based on local sunrise and sunset times. No cloud requests, no bulky installers, and no background services.

[Leer en Español (LÉEME.md)](LÉEME.md)

## Why use it?

- **No Cloud Required:** Solar calculations run entirely on your device.
- **Privacy First:** Uses Windows location locally or manual coordinates. No external APIs.
- **Standalone Option:** Use the self-contained executable when you do not want to install .NET.
- **Lean Option:** Use the framework-dependent executable when the .NET Desktop Runtime or SDK is already installed.
- **Smart Scheduling:** Correctly handles polar nights and the midnight sun.
- **Low Resource Usage:** Designed to stay in the system tray while using minimal RAM.

## How it works

The app runs in the notification area, calculates the solar schedule for your exact location, and switches Windows Light/Dark mode at the right time.

## Installation

1. Download one of the Windows x64 assets from the [latest release](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest):
   - `auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe` includes the required .NET runtime.
   - `auto-theme-solar-engine-win-x64-framework-dependent-v26.04.02.exe` requires the .NET Desktop Runtime or SDK to be installed.
2. Choose an install path:
   - `LocalAppData` is the recommended per-user path and updates silently without elevation.
   - `Program Files` is available for machine-oriented installs from an elevated PowerShell session.
3. Use the matching PowerShell entrypoint from [docs/install/README.md](docs/install/README.md):
   - `pwsh -NoLogo -NoProfile -File ./scripts/install-local-appdata.ps1 -SourceExecutablePath .\\auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe -LaunchAfterInstall`
   - `pwsh -NoLogo -NoProfile -File ./scripts/install-program-files.ps1 -SourceExecutablePath .\\auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe -LaunchAfterInstall`
4. Enter your coordinates manually or allow Windows location access.

## Privacy

- Your location data never leaves your computer.
- Coordinates are encrypted using Windows Data Protection (DPAPI).
- No telemetry, no tracking, and no internet connection required for core functionality.

## Highlights in this repository

- Targets .NET 11 preview and C# 15.
- Uses strict CI on GitHub Actions with commitlint, cspell, build, analyzers, vulnerability scanning, and tests.
- Follows Windows app theme preference for the native settings window.
- Ships English and Spanish UI text from JSON localization resources.
