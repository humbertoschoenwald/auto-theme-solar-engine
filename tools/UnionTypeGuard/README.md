# Union Type Guard

Local-only advisory scanner for C# names that may represent closed alternatives
or discriminated-union-style state.

This repository currently follows ADR-0015 and ADR-0020: closed alternatives
stay explicit through records, enums, exhaustive switches, and nullability
contracts until authored C# `union` declarations are repository-ready.

Use this tool as a review aid, not as permission to bypass ADR.

The implementation files in this directory are intentionally ignored by Git.
Only this README is tracked.
