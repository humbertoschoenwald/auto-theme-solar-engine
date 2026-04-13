# Contributing to Auto Theme Solar Engine

## Scope and platform

Auto Theme Solar Engine is a Windows-native tray application. Build, run, and test changes on Windows.

Current repository baseline:

- .NET SDK pinned in `global.json`
- Target framework: `net11.0-windows10.0.19041.0`
- Language version: `C# 15 preview`
- Node tooling from `package.json` for commitlint, changelog generation, and cspell

## Repository workflow

- Create a short-lived branch for every change.
- Push changes to the branch, never directly to `main`.
- Open a pull request into `main`.
- Wait for `CI` to pass before merge.
- Prefer squash merges to preserve a linear and auditable history.

## Commits must be atomic

Every commit must be intentionally narrow:

- one logical change per commit
- only the exact files needed for that change
- no mixed documentation, product, CI, and refactor churn unless inseparable

Use Conventional Commits:

```text
<type>[optional scope]: <description>
```

Recommended types in this repository:

- `feat`: new user-facing behavior
- `fix`: a behavior correction
- `docs`: documentation only
- `ci`: workflow, hook, or automation changes
- `refactor`: structure change without behavior change
- `test`: automated test changes
- `perf`: measurable performance improvement
- `chore`: maintenance work without product behavior changes

Examples:

```text
fix(theme): sync settings window frame with Windows theme
feat(localization): load spanish and english UI text from json
ci(actions): run commitlint and cspell inside ci
```

## Versioning and releases

This repository uses CalVer tags in `vYY.MM.PATCH` format.

Examples:

```text
v26.04.00
v26.04.01
v26.05.00
```

Release behavior is owned by GitHub Actions:

- `ci.yml` is the required gate
- `release.yml` runs only after `CI` succeeds on a push to `main`
- the release workflow calculates the next `vYY.MM.PATCH` tag for the current month
- the release workflow updates `CHANGELOG.md` using the repository changelog script

## Local validation before opening a pull request

Run the same core checks locally when possible:

```powershell
dotnet restore .\SolarEngine.slnx
dotnet build .\SolarEngine.slnx -c Release /warnaserror
dotnet format .\SolarEngine.slnx --verify-no-changes --severity error
dotnet test .\tests\SolarEngine.Tests\SolarEngine.Tests.csproj -c Release --no-build
npm install
npm run lint:spelling
```

Also install the local Git hooks once per clone:

```powershell
.\scripts\setup-hooks.ps1
```

## Test expectations

Tests in this repository must be strict and granular.

- Prefer one behavior per test.
- Add tests for every new branch, validation rule, and regression.
- When fixing a bug, add a regression test in the same change.
- Keep tests deterministic. No sleeps, clocks without control, or network dependencies.
- Favor domain and infrastructure tests over broad, ambiguous assertions.

## CI quality gates

A pull request is not ready until all of these pass:

- Conventional Commit validation
- cspell
- restore
- build with warnings treated as errors
- `dotnet format` analyzer/style gate
- NuGet vulnerability scan
- automated tests

## Documentation rules

- Keep contributor-facing and user-facing docs in English unless the file is specifically the Spanish counterpart.
- Keep technical rationale concise and repo-specific.
- Update `README.md`, `CHANGELOG.md`, or workflow docs when behavior or delivery changes.
