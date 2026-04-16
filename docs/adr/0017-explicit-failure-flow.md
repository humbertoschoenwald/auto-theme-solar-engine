# ADR-0017: Explicit Failure Flow

**Status:** Accepted
**Date:** 2026-04-16

## Context

The repository already distinguishes expected operator-facing outcomes from true
runtime failures. Coordinates can be invalid, Windows location access can be
denied, and schedule calculation can reject inputs. Those are expected outcomes,
not exceptional transport failures.

## Decision

- Expected failures use `Result` or `Result<T>` with explicit `Error` values.
- Exceptions are reserved for invalid process state, platform boundaries,
  I/O failures, security boundaries, or similarly exceptional conditions.
- Repository code must not use exceptions as ordinary branching for validation,
  not-found, access-denied, or conflict-style outcomes.
- Dedicated repository exceptions may exist when the invariant matters more than
  a generic framework exception name.

## Alternatives Considered

| Option                                                              | Pros                            | Cons                                                 |
| ------------------------------------------------------------------- | ------------------------------- | ---------------------------------------------------- |
| Use exceptions for most control flow                                | Less explicit method signatures | Harder to reason about, noisier, slower on hot paths |
| Return booleans and ad hoc out parameters                           | Minimal ceremony                | Loses error detail and consistency                   |
| Use `Result` for expected outcomes and exceptions for true failures | Explicit and deterministic      | Requires a little more local plumbing                |

## Consequences

- **Positive:** Expected outcomes remain visible in signatures and tests.
- **Positive:** Platform failures stay distinct from validation and operator
  input problems.
- **Negative:** Simple flows sometimes carry more explicit result plumbing.
- **Risks:** Inconsistent use would create a mixed error model, so reviews and
  tests must keep the boundary sharp.
