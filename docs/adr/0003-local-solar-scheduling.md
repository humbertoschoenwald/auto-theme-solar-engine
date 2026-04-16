# ADR-0003: Local Solar Scheduling

- Status: Accepted
- Date: 2026-04-16

## Context

The application must determine theme transitions without introducing cloud dependencies, privacy leaks, or brittle API contracts. The current `SolarPositionEngine` computes solar events from date and observer coordinates, and the scheduler handles both ordinary and extreme daylight conditions.

## Decision

- Core scheduling uses local astronomical calculation, not external weather or sunrise APIs.
- Solar event calculation is derived from date plus observer coordinates and must remain deterministic.
- The scheduler recognizes three daylight conditions:
  - `Standard`,
  - `PolarNight`,
  - `MidnightSun`.
- The calculation pipeline must preserve local-date correctness across time zones, UTC offsets, and daylight-saving transitions.
- Theme reevaluation is performed:
  - on startup,
  - on a clamped recurring interval,
  - after events such as clock changes, resume from sleep, and session activation.
- `TimeProvider` is the standard time abstraction for scheduler logic and tests whenever time affects behavior.

## Consequences

- The product remains fully functional without network access.
- The repository owns its mathematical edge cases, including polar-day and polar-night behavior.
- Scheduler and test code must treat time-zone and date-boundary correctness as first-class requirements.
