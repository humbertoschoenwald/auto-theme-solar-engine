# ADR-0006: Localization and Spell-Check Policy

- Status: Accepted
- Date: 2026-04-16

## Context

The repository needs a strict English editorial baseline for doctrine and contributor workflow, while still shipping Spanish desktop UI text. The current codebase embeds English and Spanish JSON resources, but the repository no longer carries a separate Spanish README or a dedicated Spanish CSpell dictionary.

## Decision

- English is the default language for repository doctrine, rules, configs, commit messages, and contributor-facing documentation.
- Spanish is allowed only in explicit user-facing desktop localization resources.
- Localization resources are stored as embedded JSON with English fallback and Spanish selected from `CurrentUICulture` when applicable.
- CSpell uses an English baseline plus two repository-curated dictionaries:
  - `NamedEntities`,
  - `Technical`.
- The Spanish runtime localization file is excluded from CSpell instead of being covered by a dedicated Spanish dictionary.
- Proper nouns and technical jargon must stay isolated in their own dictionaries so they do not weaken the general editorial baseline.

## Consequences

- English typos are less likely to be masked by a broad secondary-language dictionary.
- Spanish content remains limited to the desktop runtime instead of spreading informally across repository-facing files.
- Localization and spell-check configuration must evolve together.
