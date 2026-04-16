# PowerShell Install Entry Points

The repository keeps installation guidance outside the root README so product
overview stays short while the update model remains explicit and easy to
automate.

## LocalAppData Install

Use the per-user entrypoint when the app should live under the current user's
profile and update without requiring an elevated helper task. This is the
recommended install mode.

```powershell
pwsh -NoLogo -NoProfile -File ./scripts/install-local-appdata.ps1 `
  -SourceExecutablePath .\auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe `
  -LaunchAfterInstall
```

## Program Files Install

Use the machine-oriented entrypoint when the app should live under
`C:\Program Files\Auto Theme — Solar Engine`.

Run this from an elevated PowerShell session so the script can copy the
executable into `Program Files` and register the elevated on-demand update
task.

```powershell
pwsh -NoLogo -NoProfile -File ./scripts/install-program-files.ps1 `
  -SourceExecutablePath .\auto-theme-solar-engine-win-x64-self-contained-v26.04.02.exe `
  -LaunchAfterInstall
```

## Notes

- The install scripts preserve the release flavor recorded in the downloaded
  executable name.
- The installed executable is normalized to `AutoThemeSolarEngine.exe`.
- The updater uses `installation.json` in the install directory to keep future
  silent updates on the same release flavor, install mode, and installed
  executable path chosen by the user.
- When the install directory is writable, the updater stages the newer
  executable beside the current install. If it is not writable, the updater may
  stage under the app-owned LocalAppData update area before the helper performs
  the final move.
