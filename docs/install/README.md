# PowerShell Install Commands

The repository keeps installation guidance outside the root README so the
product overview stays short while the update model remains explicit.

## LocalAppData Install

Use the per-user entrypoint when the app should live under the current user's
profile and update without requiring elevation. This is the recommended install
mode. Use a normal PowerShell session. Each block launches the downloaded
executable automatically.

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Set-Location "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
```

Framework-dependent:

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Set-Location "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
```

## Program Files Install

Use the machine-oriented entrypoint when the app should live under
`C:\Program Files\Auto Theme — Solar Engine`.

Open PowerShell as Administrator and run one of these blocks. On the first
launch, the app bootstraps the elevated silent-update task for that install.

```powershell
New-Item -ItemType Directory -Force -Path "$env:ProgramFiles\Auto Theme — Solar Engine"
Set-Location "$env:ProgramFiles\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
```

Framework-dependent:

```powershell
New-Item -ItemType Directory -Force -Path "$env:ProgramFiles\Auto Theme — Solar Engine"
Set-Location "$env:ProgramFiles\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
```

## Notes

- The downloaded executable stays in the chosen install directory under its
  release asset name.
- The updater uses `installation.json` in the install directory to keep future
  silent updates on the same release flavor, install mode, and installed
  executable path chosen by the user.
- When the install directory is writable, the updater stages the newer
  executable beside the current install. If it is not writable, the updater may
  stage under the app-owned LocalAppData update area before the helper performs
  the final move.
