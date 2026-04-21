# ADR-0013: Self-Update and Install Model

**Status:** Accepted
**Date:** 2026-04-16

## Context

The product now needs silent in-place updates from GitHub Releases while the app
is already running. Windows cannot overwrite an executable that is still in use.
The release pipeline already emits versioned self-contained and
framework-dependent executables, and the runtime must never switch a user to the
wrong delivery flavor by accident.

## Decision

- Automatic updates are enabled by default and can be disabled from the
  `Updates` tab.
- Update checks use GitHub Releases and look only for tags newer than the
  current CalVer tag.
- Update availability checks run automatically at startup and then on a bounded
  background cadence of four hours so the settings UI reflects current release
  state without waiting for a manual action.
- The automatic-install toggle controls the silent apply step, not whether the
  app refreshes release availability metadata.
- After a completed startup or background check, the `Updates` tab should show
  the latest known matching version and current status instead of remaining in
  a perpetual `not checked yet` state.
- The preferred install mode is a per-user `LocalAppData` install because it
  allows silent updates without elevation.
- Direct download-and-run installs are supported, but the documented
  LocalAppData model normalizes the installed executable target to
  `AutoThemeSolarEngine.exe` in the install directory instead of preserving the
  downloaded versioned asset name as the long-term executable path.
- The updater must match the installed delivery flavor:
  - self-contained installs update only from self-contained assets,
  - framework-dependent installs update only from framework-dependent assets.
- The self-contained release lane publishes a Native AOT executable. The
  framework-dependent lane remains a normal CoreCLR executable so machines with
  an existing desktop runtime still have a lighter download option.
- The authoritative install target is the path the user chose. The updater must
  keep replacing the executable recorded by the current install metadata and may
  not silently migrate the app to a different directory.
- For the documented LocalAppData install flow, the authoritative executable
  target is `%LocalAppData%\AutoThemeSolarEngine\AutoThemeSolarEngine.exe`.
- Versioned release asset names remain intact only for downloaded release
  artifacts and staging inputs. They are not the preferred long-term installed
  executable target in the documented LocalAppData layout.
- For the documented install flow, the chosen directory is
  `%LocalAppData%\AutoThemeSolarEngine`.
- The documented install directory keeps `AutoThemeSolarEngine.exe`,
  `config.json`, `installation.json`, `AutoThemeSolarEngine.log`,
  `Apply-SolarEngine-Update.ps1`, and `Launch-SolarEngine-After-Update.ps1`
  together.
- Versioned release asset names remain intact on GitHub Releases. The updater
  downloads the newer executable by its release asset name, stages it beside the
  install, and then replaces the stable installed executable target after the
  current process exits.
- The preferred staging location is the current install directory so the next
  executable lands beside the currently installed one. If that location is not
  writable, the updater may stage under the app-owned LocalAppData update area
  and let the helper perform the final move.
- Applying an update uses a deferred helper step after the current process
  exits. The helper is responsible for:
  - stopping the running app if needed,
  - waiting briefly after process termination so Windows fully releases the file
    lock before replacement,
  - replacing the stable installed executable target with the newly downloaded
    version,
  - updating startup registration when the executable path changes,
  - migrating legacy startup value names when runtime branding changes,
  - migrating legacy versioned LocalAppData installs to
    `AutoThemeSolarEngine.exe`,
  - cleaning up stale superseded executables left in the install directory,
  - launching the new executable.
- Existing LocalAppData installs that still point at versioned executable names
  are migrated automatically during installation metadata normalization and the
  next silent-update apply cycle.
- The updater must write its deferred update request before it launches any
  watcher or helper process so relaunch and replacement observe a complete
  request file.
- Helper and relaunch processes must resolve PowerShell through an absolute
  executable path rather than assuming `pwsh` is available on the user PATH.
- GitHub Releases may be marked as `YANKED` for defective published versions.
  YANKED releases remain visible and downloadable, but the updater must not
  select them as valid automatic-update candidates.
- The silent update flow should preserve the effective privilege boundary of the
  currently running app when it relaunches. When a protected install location
  requires elevation for file replacement, the design must separate the
  privileged swap step from the normal-user relaunch step.
- The install directory must contain enough metadata for the updater to resolve
  the current install flavor and executable target deterministically.
- The repository ships PowerShell-based install entrypoints only for the
  recommended per-user install under LocalAppData.
- Existing installs outside the documented LocalAppData path remain a
  compatibility concern for update matching, but repository install guidance no
  longer advertises a separate machine-oriented entrypoint.
- Silent updates are a first-class requirement. If an install location requires
  extra privileges, that requirement must be handled by the install model rather
  than surfaced as ad hoc runtime prompts.

## Alternatives Considered

| Option                                                                   | Pros                                                         | Cons                                                    |
| ------------------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------- |
| Overwrite the running executable in place                                | Simple conceptually                                          | Windows file locking makes it unreliable                |
| Download a differently named executable and relaunch it through a helper | Compatible with versioned release assets and Windows locking | Needs helper orchestration and install metadata         |
| Introduce a full separate installer technology                           | Rich lifecycle support                                       | Heavier than the current single-file distribution model |

## Consequences

- **Positive:** The updater can move between release versions without violating
  Windows executable locking rules.
- **Positive:** Users stay on the same runtime flavor they originally installed.
- **Positive:** The documented LocalAppData install path converges on a stable
  executable target that is easier to relaunch and migrate.
- **Positive:** The updater has a documented fallback when the preferred staging
  directory is not writable.
- **Positive:** Update status stays informative even when automatic install is
  disabled.
- **Negative:** Update orchestration now spans runtime code, install scripts,
  release assets, and startup registration.
- **Risks:** Compatibility for pre-existing installs outside the documented
  LocalAppData path still depends on the updater preserving the recorded
  executable target.
