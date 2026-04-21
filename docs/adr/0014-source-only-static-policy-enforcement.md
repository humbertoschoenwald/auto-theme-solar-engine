# ADR-0014: Source-Only Static Policy Enforcement

- Status: Accepted
- Date: 2026-04-16

## Context

The repository needs enforceable source-only rules banning
`System.InvalidOperationException` and authored `DllImport` declarations from
repository code. A Roslyn banned-symbol analyzer looked attractive at first,
but it also flagged `System.Text.Json` source-generated files under `obj/`,
which are not authored repository code and cannot be realistically rewritten by
the project. That made the enforcement mechanism noisy and fragile even when
`src/` and `tests/` were clean.

The repository now also needs a deterministic way to reject newly introduced
magic strings and magic numbers in authored product code without policing the
descriptive literals that make tests readable.

## Decision

- Repository-only type bans must target authored source, not generated code.
- The `InvalidOperationException` ban is enforced through a repository lint
  script that scans `src/**/*.cs` and `tests/**/*.cs`, excluding `bin/` and
  `obj/`.
- The `DllImport` ban is enforced through the same source-only repository lint
  model so the repo can reject authored regressions without depending on how the
  active SDK handles generated files.
- Magic string and magic number enforcement is source-only and applies to
  authored product code under `src/**/*.cs`.
- That enforcement must allow:
  - literals inside named `const` declarations and enum members,
  - assembly or interop metadata literals where the literal is metadata rather
    than behavioral runtime code,
  - the documented solar-calculation coefficients and bounds covered by
    `ADR-0003`.
- Tests under `tests/**/*.cs` are intentionally excluded from the magic-literal
  ban so DAMP-style scenario setup remains readable.
- This source-only enforcement must run as part of the standard local tooling
  entrypoint so local and CI enforcement stay aligned.
- Generated files from SDKs, source generators, or package tooling remain outside this policy unless the repository explicitly checks them in.

## Consequences

- The policy stays strict on code the repository owns.
- The enforcement path is simpler to reason about than analyzer exceptions for generated files.
- The repository gives up symbol-level semantic enforcement for this one rule in exchange for deterministic source-only scope.
- Multiple lightweight repo-specific bans can share the same authored-source
  scan without widening policy onto generated output the repository does not own.
- Product code now pays an upfront naming cost for literals, but repository
  intent becomes easier to audit and safer to change.
