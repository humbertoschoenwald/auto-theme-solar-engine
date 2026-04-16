# ADR-0015: Preview SDK and Language Adoption

**Status:** Accepted
**Date:** 2026-04-16

## Context

The repository intentionally targets preview .NET and C# features. That only
works if the toolchain is pinned tightly enough for local development, CI, and
release automation to agree on the same compiler and analyzer behavior.

The requested baseline is .NET 11 Preview 3 and C# 15. In practice, preview
language adoption must still be constrained by what the active Roslyn compiler
and reference assemblies can compile without unsupported shims.

## Decision

- The repository pins the SDK to `11.0.100-preview.3.26207.106`.
- The repository keeps `LangVersion` on `preview` until the active compiler
  accepts the numeric `15.0` value in normal repository builds.
- Preview syntax or APIs may be adopted only after a compile probe with the
  pinned SDK succeeds in a normal SDK-style project.
- The repository may use currently supported modern syntax where it fits the
  codebase, including extension members, collection-expression arguments,
  unbound generic `nameof`, the `field` keyword, null-conditional assignment,
  and implicit span conversions.
- The repository does not use C# `union` declarations in product code yet.
  Although the preview compiler recognizes the syntax, the active toolchain does
  not provide a complete end-to-end path for repository use without custom
  runtime shims. Current repository code must continue to model variant state
  with explicit records, enums, and `Result<T>`.

## Alternatives Considered

| Option                                                         | Pros                                  | Cons                                   |
| -------------------------------------------------------------- | ------------------------------------- | -------------------------------------- |
| Follow floating preview SDK builds                             | Fast access to new features           | Local, CI, and release behavior drift  |
| Force `LangVersion` to `15.0` immediately                      | Matches desired end-state numerically | Fails with the active compiler         |
| Pin Preview 3 and adopt only features proven by compile probes | Deterministic and reviewable          | Some requested preview ideas must wait |

## Consequences

- **Positive:** Local and remote builds agree on the same compiler surface.
- **Positive:** Preview feature usage stays intentional instead of speculative.
- **Negative:** Some desired future-facing syntax remains deferred even though it
  is already publicly discussed.
- **Risks:** Preview SDK upgrades now require deliberate doctrine and validation
  work instead of casual version bumps.
