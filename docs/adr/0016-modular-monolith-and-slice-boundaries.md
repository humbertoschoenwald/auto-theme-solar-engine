# ADR-0016: Modular Monolith and Slice Boundaries

**Status:** Accepted
**Date:** 2026-04-16

## Context

This repository is a single Windows-native desktop product with one deployable
runtime and a small number of tightly related capabilities: locations, solar
calculation, system host behavior, theme mutation, updates, and UI. Breaking
that into independent services or unrelated layer folders would add coordination
cost without helping the product.

## Decision

- The repository remains a modular monolith.
- Product code is organized by feature slices and domain boundaries instead of
  layer-per-folder structures such as `Services`, `Managers`, or
  `Controllers`.
- Each feature owns its application, domain, and infrastructure types close to
  the behavior it implements.
- Shared abstractions are extracted only when duplication is stable enough to be
  a real cross-feature concept.
- Cross-process decomposition, background daemons, or service-style splits
  require a new ADR.

## Alternatives Considered

| Option                                                              | Pros                                   | Cons                                            |
| ------------------------------------------------------------------- | -------------------------------------- | ----------------------------------------------- |
| Split the app into multiple services or helper processes by default | Hard runtime isolation                 | Unnecessary coordination and deployment cost    |
| Use generic layer folders                                           | Familiar at first glance               | Feature behavior gets scattered across the tree |
| Keep a modular monolith with vertical slices                        | Matches product size and runtime model | Requires discipline around local boundaries     |

## Consequences

- **Positive:** A feature can usually be reviewed, tested, and changed in one
  place.
- **Positive:** The runtime stays simple to ship and debug.
- **Negative:** Boundary discipline is a code-review responsibility, not a
  deployment boundary.
- **Risks:** Shared concerns can become muddled if slices ignore domain versus
  infrastructure ownership.
