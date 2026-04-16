# ADR-0009: Coding and Testing Standards

- Status: Accepted
- Date: 2026-04-16

## Context

The current codebase shows a consistent implementation style: small feature-oriented folders, explicit result types for expected failures, internal and sealed types by default, heavy use of async orchestration, and deterministic tests with regression coverage. The repository also needs a clean split between doctrine and implementation comments.

## Decision

- Organize product code by feature and domain boundaries rather than generic `Services`, `Managers`, or `Controllers` folders.
- Represent expected failures with `Result` and `Result<T>` plus explicit `Error` values. Exceptions remain for infrastructure failures and invalid boundary states.
- Prefer internal and sealed types by default. Expand the public surface only when external consumption requires it.
- Keep code modern and explicit:
  - file-scoped namespaces,
  - explicit accessibility,
  - primary constructors when they reduce wiring noise,
  - cancellation-token propagation across async boundaries.
- Allow sync-over-async only at the native application entry boundary where the process must bridge into a Win32 message loop.
- Keep code comments sparse. Repository-wide rationale belongs in ADR. Inline comments are reserved for local hazards such as lifetime races, shutdown behavior, resource ownership, or similarly non-obvious mechanics.
- Public XML documentation, when public APIs exist, should describe behavior. It should not duplicate repository policy.
- Tests must be deterministic, granular, regression-oriented, and free of network dependence.
- Time-dependent behavior should use `TimeProvider` so tests can control time explicitly.

## Consequences

- The repository favors explicit orchestration and explicit failure channels over framework magic.
- Tests become more numerous and targeted, but they also localize regressions better.
- Code stays smaller because doctrine leaves the implementation layer.
