# AGENTS.md

Read this file first. Then read every applicable file under `/.cursor/rules/**/*.mdc`.

Repository doctrine does not live here. Repository doctrine lives in `docs/adr/**/*.md`, and the Cursor rules are the operational layer derived from those ADRs.

Mandatory execution order for any agent:

1. Read `/AGENTS.md`.
2. Read every applicable file under `/.cursor/rules/**/*.mdc`.
3. Read the ADRs cited by those rules before making decisions.

Hard requirements:

- Do not introduce repository policy, stack changes, quality gates, scope changes, or workflow changes outside ADR.
- If a requested change requires new doctrine, create or update the relevant ADR first, then derive the rule, then update configs or code.
- Treat `.cursor/rules` as binding operational instructions.
- Treat repository-facing content as English-only unless an ADR explicitly allows a user-facing Spanish artifact.

This repository is documentation-driven and ADR-first.
