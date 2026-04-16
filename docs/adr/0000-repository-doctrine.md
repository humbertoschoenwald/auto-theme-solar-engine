# ADR-0000: Repository Doctrine

- Status: Accepted
- Date: 2026-04-16

## Context

Repository doctrine had been spread across `AGENTS.md`, Cursor rules, contributor docs, config comments, and implementation notes. That duplication made the repository harder to maintain and forced agents to learn policy from multiple inconsistent places.

This repository needs a single canonical doctrine layer that remains stable even when prompts, configs, or scaffolding change.

## Decision

- `docs/adr/**/*.md` is the canonical source of repository doctrine.
- `docs/bibliography/**/*.md` stores supporting public references used by ADR but does not override ADR.
- `.cursor/rules/**/*.mdc` is an operational projection of ADR and must cite the ADR files it derives from.
- High-impact repository configs and convenience docs are derived artifacts and must not introduce independent policy.
- `AGENTS.md` is a bootstrap document only. It routes agents into the doctrine pipeline and does not define stack, workflow, or quality policy.
- Repository-facing content is English only unless another ADR explicitly allows a Spanish user-facing artifact.
- Repository policy changes must follow this sequence: ADR, then `.cursor/rules`, then configs, then code or scaffolding.
- Repository-wide rationale belongs in ADR. Code comments should remain local to implementation hazards, races, or non-obvious mechanics. Public XML documentation, when present, should describe behavior rather than restating repository policy.

## Consequences

- Repository doctrine becomes traceable and reviewable in one place.
- Prompt instructions and config comments stay smaller because they reference ADR instead of restating it.
- When a requested change needs a new workflow, stack, or quality decision, the repository must gain or update an ADR before any code or config change lands.
