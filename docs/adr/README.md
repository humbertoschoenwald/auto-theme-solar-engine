# ADR Workflow

This repository is documentation-driven and ADR-first.

## Authority Model

The authority model is intentionally split into layers:

1. `docs/adr/**/*.md` is the canonical source of repository doctrine.
2. `docs/bibliography/**/*.md` stores supporting public references used by ADR.
3. `.cursor/rules/**/*.mdc` is the operational layer derived from ADR.
4. Repository configs and convenience docs are derived from `.cursor/rules` and therefore indirectly from ADR.
5. Code, scaffolding, and generated artifacts are downstream outputs.

`AGENTS.md` is only a bootstrap file. It exists to route any agent into the doctrine pipeline. It is not the repository's technical source of truth.

## Required Workflow

Every material repository decision must follow this order:

1. Create or update the relevant ADR.
2. Derive or update the matching `.cursor/rules` file.
3. Update repository configs that operationalize that rule.
4. Only then update scaffolding, automation, or code.

If a config file or automation requires a new policy that is not already stated in ADR, stop and add the ADR first.

## Status Definitions

- `Accepted`: active doctrine for the repository.
- `Draft`: under active design and not yet binding.
- `Superseded`: retained for history but replaced by a newer ADR.

## Traceability Requirement

Every `.cursor/rules/*.mdc` file must cite at least one ADR.

Every high-impact config file should reference the ADR that justifies it in a comment when the file format permits comments.

Current high-impact derived files include:

- `AGENTS.md`
- `.editorconfig`
- `.gitattributes`
- `.vscode/settings.json`
- `cspell.jsonc`
- `package.json`
- `commitlint.config.cjs`
- `.github/actions/setup-dotnet-env/action.yml`
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `.githooks/pre-push`
- `CONTRIBUTING.md`

## Current ADR Set

- `0000-repository-doctrine.md`
- `0001-product-scope-and-platform.md`
- `0002-native-runtime-and-ui.md`
- `0003-local-solar-scheduling.md`
- `0004-configuration-privacy-and-state.md`
- `0005-theme-mutation-and-shell-refresh.md`
- `0006-localization-and-spellcheck-policy.md`
- `0007-toolchain-and-package-management.md`
- `0008-quality-gates-versioning-and-automation.md`
- `0009-coding-and-testing-standards.md`
- `0010-bibliography-and-source-policy.md`
