# Project Instructions

This repository uses Cursor Project Rules as the primary source of agent behavior. Keep instructions in `.cursor/rules` focused, composable, and file-scoped where possible.

Before making changes:

1. Read the relevant ADRs in `docs/adr/`.
2. Read the standards docs in `docs/standards/`.
3. Follow atomic Conventional Commits.
4. Do not mix unrelated changes in the same commit.
5. Do not bypass tests, formatting, or CI checks.

The current baseline is:

- C# 15
- .NET 11
- Modular Monolith by default
- Vertical Slice architecture
- `Result<T>` for expected errors
- Minimal APIs
- EF Core + Dapper hybrid
- Very strict, highly granular tests
- CALVER tags in `vYY.MM.PATCH` format
