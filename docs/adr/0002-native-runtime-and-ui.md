# ADR-0002: Native Runtime and UI

- Status: Accepted
- Date: 2026-04-16

## Context

An earlier UI approach based on WPF delivered a polished interface but imposed an idle memory cost that conflicted with the product goal of being a lightweight tray utility. The current codebase instead builds the runtime and settings experience directly on Win32 interop.

The code shows this decision in `NativeApplication`, `TrayIconHost`, `SettingsWindow`, and `NativeInterop`.

## Decision

- The repository uses raw Win32 interop instead of WPF, WinForms, MAUI, or another UI framework.
- `NativeApplication` is the native composition root. It owns:
  - single-instance enforcement through a named `Mutex`,
  - `ServiceProvider` creation with validation,
  - lifetime cancellation,
  - tray host creation,
  - settings window creation,
  - the Win32 message loop.
- The visible UI consists of:
  - a hidden tray host window for shell integration,
  - a native settings window built from Win32 controls,
  - a tray icon context menu.
- Interop helpers live in a dedicated `NativeInterop` boundary. Add new Win32 calls there instead of scattering platform calls throughout feature code.
- Lightweight runtime behavior is a product requirement. When the native settings window is hidden, the app may schedule one best-effort GC compaction to reclaim short-lived UI allocations without making explicit collection part of the normal hot path.
- Native window styling should stay minimal and stable. Lightweight shell integration takes priority over decorative framework features.

## Consequences

- The repository remains Windows-specific by design.
- UI changes require manual Win32 control layout and lifecycle handling.
- Memory use and startup cost stay aligned with the tray-app goal, but the code accepts additional interop complexity.
