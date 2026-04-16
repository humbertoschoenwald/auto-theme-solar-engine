# ADR-0010: Bibliography and Source Policy

- Status: Accepted
- Date: 2026-04-16

## Context

Tooling, platform, and workflow decisions are easier to review when the repository keeps a curated list of public references. This repository also contains one intentional compatibility exception that depends on undocumented Windows behavior.

## Decision

- Maintain official and public supporting references in `docs/bibliography/tooling-and-standards.md`.
- Prefer official vendor documentation, standards bodies, or primary project documentation when an ADR cites external references.
- If behavior depends on unsupported or undocumented platform details, record the exception in ADR rather than pretending it is a standard reference.
- Bibliography is supporting context. ADR remains the canonical doctrinal layer.

## Consequences

- Source discovery becomes faster for future maintainers and agents.
- Unsupported behavior stays visible as an exception instead of blending into normal standards references.
