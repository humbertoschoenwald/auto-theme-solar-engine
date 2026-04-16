# ADR-0012: Native Settings Experience

**Status:** Accepted
**Date:** 2026-04-16

## Context

The native settings window currently exposes all controls in a single flat
surface, hardcodes most user-facing strings, and does not mirror the app's
runtime light or dark state. Recent product direction adds clearer information
architecture, explicit language switching, and simpler product-facing branding
inside the desktop UI while keeping the runtime fully native.

## Decision

- The repository and release assets keep the long product name
  `Auto Theme — Solar Engine`, but the running desktop app uses the short
  visible name `Solar Engine` in compact native surfaces such as window titles,
  tray captions, and message boxes.
- The native settings window uses three top-level tabs:
  - `Home`
  - `Configuration`
  - `Updates`
- `Save and apply` remains persistently visible regardless of the active tab.
- The settings UI must support English and Spanish through embedded JSON
  resources and an explicit in-app language switcher.
- The language switcher is a direct runtime control in the settings window
  rather than an external configuration file edit.
- Runtime status text should describe state, not repeat the long product name.
- The settings window should mirror the currently applied runtime theme mode.
  When the runtime mode is dark, the window uses a dark visual treatment. When
  the runtime mode is light, the window uses a light treatment.
- The `Home` tab contains Windows location status, Windows location detection,
  coordinates, precision, and today's schedule.
- The `Configuration` tab contains startup, tray, process-priority, language,
  sunset-offset, and runtime-status controls.
- The `Updates` tab contains automatic-update state and manual update checks.
- `Use Windows location` defaults to enabled only when native location access is
  actually available. If access is unavailable or denied, the option must be
  disabled rather than appearing active.
- `Use high process priority` defaults to enabled.

## Alternatives Considered

| Option                                                                             | Pros                                           | Cons                                             |
| ---------------------------------------------------------------------------------- | ---------------------------------------------- | ------------------------------------------------ |
| Keep the single-page settings window                                               | Minimal implementation effort                  | Harder to navigate and noisier for users         |
| Use a heavyweight UI framework for tabs and theming                                | Faster UI iteration                            | Violates the native lightweight runtime doctrine |
| Keep native Win32 and add explicit tabs, theme-aware colors, and JSON localization | Matches product goals and current architecture | Requires more manual layout work                 |

## Consequences

- **Positive:** The app stays lightweight while gaining clearer navigation and
  better localization control.
- **Positive:** User-facing branding can stay compact and legible in the
  desktop runtime without changing repository or release naming.
- **Negative:** Theme-aware Win32 drawing and tab state handling add more manual
  UI code.
- **Risks:** A native layout refactor can regress control focus, visibility, and
  resize behavior unless it is covered with targeted tests and runtime checks.
