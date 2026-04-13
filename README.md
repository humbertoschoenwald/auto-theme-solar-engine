# auto-theme-solar-engine

[![Release](https://img.shields.io/github/v/release/humbertoschoenwald/auto-theme-solar-engine?display_name=tag&label=release)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest)
[![CI](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml)

A lightweight Windows system tray app that automatically switches your PC theme based on local sunrise and sunset times. No cloud requests, no bulky installers, and no background services.

[Leer en Español (LEEME.md)](LEEME.md)

## Why use it?

- **No Cloud Required:** Solar calculations run entirely on your device.
- **Privacy First:** Uses Windows location locally or manual coordinates. No external APIs.
- **Standalone:** Distributed as a single executable. No .NET installation required.
- **Smart Scheduling:** Correctly handles polar nights and the midnight sun.
- **Low Resource Usage:** Designed to stay in the system tray while using minimal RAM.

## How it works

The app runs in the notification area, calculates the solar schedule for your exact location, and switches Windows Light/Dark mode at the right time.

## Installation

1. Download the `auto-theme-solar-engine-win-x64-26.04.00.exe` asset from the [latest release](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest).
2. Run it. No installation is required.
3. Enter your coordinates manually or allow Windows location access.

## Privacy

- Your location data never leaves your computer.
- Coordinates are encrypted using Windows Data Protection (DPAPI).
- No telemetry, no tracking, and no internet connection required for core functionality.

## Highlights in this repository

- Targets .NET 11 preview and C# 15.
- Uses strict CI on GitHub Actions with commitlint, cspell, build, analyzers, vulnerability scanning, and tests.
- Follows Windows app theme preference for the native settings window.
- Ships English and Spanish UI text from JSON localization resources.
