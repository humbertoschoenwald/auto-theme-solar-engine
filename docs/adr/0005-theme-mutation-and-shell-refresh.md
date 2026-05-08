# ADR-0005: Theme Mutation and Shell Refresh

- Status: Accepted
- Date: 2026-04-16

## Context

Changing the Windows theme cleanly requires more than flipping a single registry value. The repository updates shell theme keys, tracks a small amount of app-owned taskbar preference state, and then refreshes shell windows to avoid restart-heavy or flicker-prone behavior.

The implementation also uses undocumented `uxtheme.dll` exports for better refresh behavior. That is a deliberate compatibility exception and must be recorded centrally.

## Decision

- Theme application is performed through the current-user Personalize registry values for app and system light-theme state.
- Theme application may reassert the current transparency preference as a
  refresh pulse when the shell taskbar keeps stale accent surfaces after a
  light/dark transition. The final transparency value must match the user's
  original preference.
- Theme application may also synchronize the current theme metadata file when
  Windows reports an unsaved custom theme even though the app and system mode
  registry values are already light or dark. That synchronization must preserve
  the user's current theme content where possible, update only the current
  theme mode metadata needed to make the active mode observable, and store any
  generated theme file under app-owned LocalAppData.
- The repository is allowed to keep a small application-owned registry subkey for taskbar appearance bookkeeping required to round-trip the user's dark-mode taskbar preference.
- After registry mutation, the app must broadcast shell setting and
  theme-change messages, directly notify enumerated top-level desktop windows,
  and redraw the relevant shell windows so the change becomes visible without
  requiring a restart.
- The repository accepts a narrowly scoped compatibility exception for undocumented `uxtheme.dll` exports `#104` and `#136` to improve seamless theme refresh behavior.
- Undocumented export usage is best-effort. If those exports are unavailable, the app must continue with the documented broadcast and redraw path.

## Consequences

- Major Windows updates can regress theme refresh behavior, so this area deserves focused regression testing and manual verification.
- The repository documents the compatibility risk in ADR instead of pretending the behavior is covered by official public API guarantees.
- The shell-refresh path stays Windows-specific by design.
