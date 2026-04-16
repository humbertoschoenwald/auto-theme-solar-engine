# ADR-0006: Localization and Spell-Check Policy

- Status: Accepted
- Date: 2026-04-16

## Context

The repository needs a strict English editorial baseline for doctrine and contributor workflow, while still shipping Spanish user-facing documentation and UI text. The current codebase already embeds English and Spanish JSON resources and keeps repository spell-checking strict through curated dictionaries under `.cspell/`.

## Decision

- English is the default language for repository doctrine, rules, configs, commit messages, and contributor-facing documentation.
- Spanish is allowed only in explicit user-facing artifacts, currently including `LÉEME.md` and localization resource files.
- Localization resources are stored as embedded JSON with English fallback and Spanish selected from `CurrentUICulture` when applicable.
- CSpell uses an English baseline plus three repository-curated dictionaries:
  - `Spanish`,
  - `NamedEntities`,
  - `Technical`.
- The Spanish dictionary is a manual whitelist, not a general Spanish language pack.
- Proper nouns and technical jargon must stay isolated in their own dictionaries so they do not weaken the general editorial baseline.

## Consequences

- English typos are less likely to be masked by a broad secondary-language dictionary.
- Spanish content remains intentional and reviewable instead of spreading informally across contributor-facing files.
- Localization and spell-check configuration must evolve together.
