# Contributing to AutoThemeSolarEngine

This file is a convenience guide. Canonical repository doctrine lives in `docs/adr/**/*.md`.

## Before you change anything

1. Read `AGENTS.md`.
2. Read every applicable file under `/.cursor/rules/**/*.mdc`.
3. Read the ADRs cited by those rules.

## Local setup

```powershell
.\scripts\setup-hooks.ps1
pnpm install --frozen-lockfile
```

## Core validation

```powershell
pnpm lint
dotnet restore .\SolarEngine.slnx
dotnet format .\SolarEngine.slnx --verify-no-changes --severity error --no-restore
dotnet build .\SolarEngine.slnx --configuration Release --no-restore
dotnet test .\tests\SolarEngine.Tests\SolarEngine.Tests.csproj --configuration Release --no-build
```

For commit policy, versioning, release behavior, platform constraints, and contributor language rules, use ADR as the source of truth.
