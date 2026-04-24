# ADR-0013: Self-Update and Install Model

**Status:** Accepted
**Date:** 2026-04-16

## Context

The product now needs automatic in-place updates from GitHub Releases while the
app is already running. Windows cannot overwrite an executable that is still in
use. The release pipeline now emits one versioned self-contained executable,
and the runtime still needs a deterministic update model that can migrate
legacy installation metadata without breaking automatic replacement.

Recent product feedback also makes the apply step intentionally visible. The
actual executable swap should still happen automatically, but the user should
see the PowerShell helper window that waits for the old process to close,
replaces the installed executable, launches the updated build, and then exits.

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
  allows automatic updates without elevation.
- Direct download-and-run installs are supported, but the documented
  LocalAppData model normalizes the installed executable target to
  `AutoThemeSolarEngine.exe` in the install directory instead of preserving the
  downloaded versioned asset name as the long-term executable path.
- The release lane publishes one self-contained Native AOT executable.
- Installation metadata should normalize to the self-contained delivery flavor,
  including legacy manifests that still mention a removed framework-dependent
  flavor.
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
  `config.json`, `installation.json`, `AutoThemeSolarEngine.log`, and
  `Apply-SolarEngine-Update.ps1` together.
- Versioned release asset names remain intact on GitHub Releases. The updater
  downloads the newer executable by its release asset name, stages it beside the
  install, and then replaces the stable installed executable target after the
  current process exits.
- The preferred staging location is the current install directory so the next
  executable lands beside the currently installed one. If that location is not
  writable, the updater may stage under the app-owned LocalAppData update area
  and let the helper perform the final move.
- Applying an update uses one deferred PowerShell helper step after the current
  process exits. The helper window stays visible while it is running and is
  responsible for:
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
  - launching the new executable,
  - exiting after the relaunch attempt completes.
- Existing LocalAppData installs that still point at versioned executable names
  are migrated automatically during installation metadata normalization and the
  next automatic apply cycle.
- Legacy manifests that still declare a framework-dependent release flavor are
  normalized to self-contained so the updater can continue to find supported
  release assets.
- The updater must write its deferred update request before it launches the
  helper process so replacement and relaunch observe a complete request file.
- The helper process must resolve PowerShell through an absolute executable
  path rather than assuming `pwsh` is available on the user PATH.
- GitHub Releases may be marked as `YANKED` for defective published versions.
  YANKED releases remain visible and downloadable, but the updater must not
  select them as valid automatic-update candidates.
- The visible helper update flow should preserve the effective privilege
  boundary of the currently running app when it relaunches. When a protected
  install location requires elevation for file replacement, the design must
  separate the privileged swap step from the normal-user relaunch step.
- The install directory must contain enough metadata for the updater to resolve
  the current install flavor and executable target deterministically.
- The repository ships PowerShell-based install entrypoints only for the
  recommended per-user install under LocalAppData.
- Existing installs outside the documented LocalAppData path remain a
  compatibility concern for update matching, but repository install guidance no
  longer advertises a separate machine-oriented entrypoint.
- Automatic updates are a first-class requirement. If an install location
  requires extra privileges, that requirement must be handled by the install
  model rather than surfaced as ad hoc runtime prompts.

## Alternatives Considered

| Option                                                                   | Pros                                                         | Cons                                                    |
| ------------------------------------------------------------------------ | ------------------------------------------------------------ | ------------------------------------------------------- |
| Overwrite the running executable in place                                | Simple conceptually                                          | Windows file locking makes it unreliable                |
| Download a differently named executable and relaunch it through a helper | Compatible with versioned release assets and Windows locking | Needs helper orchestration and install metadata         |
| Introduce a full separate installer technology                           | Rich lifecycle support                                       | Heavier than the current single-file distribution model |

## Consequences

- **Positive:** The updater can move between release versions without violating
  Windows executable locking rules.
- **Positive:** The release and update model is simpler because the repository
  now ships one supported runtime flavor.
- **Positive:** The documented LocalAppData install path converges on a stable
  executable target that is easier to relaunch and migrate.
- **Positive:** The apply step is observable because the user sees the visible
  PowerShell helper while replacement is in progress.
- **Positive:** The updater has a documented fallback when the preferred staging
  directory is not writable.
- **Positive:** Update status stays informative even when automatic install is
  disabled.
- **Negative:** Update orchestration now spans runtime code, install scripts,
  release assets, and startup registration.
- **Negative:** Automatic apply now briefly opens a PowerShell window during the
  replacement step.
- **Risks:** Compatibility for pre-existing installs outside the documented
  LocalAppData path still depends on the updater preserving the recorded
  executable target.
