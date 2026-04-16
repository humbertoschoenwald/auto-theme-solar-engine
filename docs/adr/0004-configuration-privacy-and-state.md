# ADR-0004: Configuration Privacy and State

- Status: Accepted
- Date: 2026-04-16

## Context

The application stores user configuration locally, including coordinates that are sensitive enough to warrant protection but not so high-risk that the product needs a remote account system. The current repository already persists config under LocalAppData, protects coordinates with Windows DPAPI, trims logs, and migrates legacy plaintext state.

## Decision

- Runtime state is stored per user under `%LocalAppData%\\AutoThemeSolarEngine`.
- Configuration persists as JSON through a temp-file write followed by replace or move so writes are atomic at the file level.
- Coordinates are never written in plaintext in the current format.
- Coordinates must be reduced to a bounded decimal precision before storage.
- Reduced coordinates are protected with Windows DPAPI before they are serialized.
- Legacy plaintext coordinates may be read for compatibility, but when they are valid they must be rewritten immediately into the protected reduced-precision format.
- Runtime logs stay local, use UTF-8 without BOM, and are trimmed to a small bounded size instead of growing without limit.
- Windows location remains optional. When it is enabled, refreshed coordinates follow the same reduction and protection path as manually entered coordinates.

## Consequences

- The app favors privacy-preserving local storage over portability of raw configuration between machines.
- State corruption is limited by atomic writes and normalization on load.
- Logs are intentionally small and local, which keeps the tray process lightweight but limits historical retention.
