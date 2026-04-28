# Local Tools

This folder is for local-only repository tools used while maintaining Auto
Theme Solar Engine. Tool implementations are intentionally ignored by Git.

Only this file and each direct tool directory's `README.md` are tracked. Do not
commit tool source, binaries, generated output, caches, or private policy files
from this folder unless a future ADR explicitly accepts that tool implementation
as repository code.

## Available Tools

- `GitGuard`: local Git identity, path-safety, commit, changelog, and push guard.
- `UnionTypeGuard`: advisory C# scan for type names that may be hiding closed
  alternatives or discriminated-union-style state.
