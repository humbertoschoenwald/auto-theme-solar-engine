# ADR-0009: Coding and Testing Standards

- Status: Accepted
- Date: 2026-04-16

## Context

The current codebase shows a consistent implementation style: small feature-oriented folders, explicit result types for expected failures, internal and sealed types by default, heavy use of async orchestration, and deterministic tests with regression coverage. The repository also needs a clean split between doctrine and implementation comments.

## Decision

- Organize product code by feature and domain boundaries rather than generic `Services`, `Managers`, or `Controllers` folders.
- Represent expected failures with `Result` and `Result<T>` plus explicit `Error` values. Exceptions remain for infrastructure failures and invalid boundary states.
- Keep responsibilities narrow. Prefer small types and feature slices that each own one reason to change, and extract shared logic only when the duplication is stable enough to deserve a real abstraction.
- Keep domain policy in domain code, orchestration in application code, and OS integration in infrastructure code so the repository stays aligned with Clean Architecture and DDD-style boundaries.
- Prefer internal and sealed types by default. Expand the public surface only when external consumption requires it.
- Keep code modern and explicit:
  - file-scoped namespaces,
  - explicit accessibility,
  - primary constructors when they reduce wiring noise,
  - cancellation-token propagation across async boundaries.
- C# formatting and naming are strict:
  - use Allman braces,
  - use four spaces and never tabs,
  - keep using directives at the top of the file outside namespace blocks with
    `System.*` imports first,
  - keep fields at the top of the type,
  - keep visibility explicit and first in the modifier list,
  - use `_camelCase` for private and internal instance fields, `s_` for static
    fields, and `t_` for thread-static fields,
  - use `PascalCase` for public fields, constant fields, constant locals,
    methods, and local functions,
  - prefer `readonly` where the language allows it, with `static readonly`
    ordering for static fields,
  - avoid `this.` except when disambiguation is required,
  - use C# keywords such as `int` and `string` instead of BCL aliases such as
    `Int32` and `String`,
  - prefer `nameof(...)` over repeated string literals when the symbol name is
    the contract,
  - keep at most one blank line in a row and avoid spurious whitespace,
  - encode non-ASCII source literals with `\uXXXX` escapes.
- The repository adopts the Roslyn style baseline as a strict superset. When an
  upstream guideline would permit omitted braces, `var`, target-typed `new()`,
  or other shorthand, the stricter repository rule still wins unless doctrine
  changes first.
- Never use single-line `if` statements on the same line as the condition.
  If any branch in an `if`/`else if`/`else` chain requires braces, every branch
  must use braces.
- Keep internal and private types `static` or `sealed` unless real derivation is
  required.
- Primary constructor parameters should read like parameters, not fields. When a
  type is large enough that usage is not obvious at a glance, copy the
  parameter into a repository-style field.
- If a legacy file already carries a different local naming pattern, preserve
  that file-local style until the file receives a deliberate normalization edit.
- Prefer early returns and readable branching over deep ternary chains when validating or normalizing inputs.
- Allow sync-over-async only at the native application entry boundary where the process must bridge into a Win32 message loop.
- Keep code comments sparse. Repository-wide rationale belongs in ADR. Inline comments are reserved for local hazards such as lifetime races, shutdown behavior, resource ownership, or similarly non-obvious mechanics.
- Public XML documentation, when public APIs exist, should describe behavior. It should not duplicate repository policy.
- Repository rigor may adopt preview .NET and C# features only when they are
  supported by the active SDK and remain understandable to reviewers. Forum
  snippets or proposal sketches are not doctrine by themselves.
- Analyzer and formatter policy should enforce only rules that the current SDK,
  `dotnet format`, and `.editorconfig` can actually evaluate in CI.
- Generic `System.InvalidOperationException` is banned by repository policy. Use `Result<T>`, a more specific framework exception, or a dedicated repository exception that names the broken invariant.
- Authored P/Invoke declarations must use `LibraryImport`, not `DllImport`.
- Owned native resources such as icons, menus, brushes, and fonts must not live
  in naked `nint` fields. Wrap owned lifetimes in repository-approved
  `SafeHandle` types and keep raw integers only for borrowed OS handles or
  transient message parameters.
- Prefer measurable modern platform guidance where it fits the codebase, such as
  frozen collections for read-heavy lookup tables, structured logging message
  templates, span-friendly parsing on hot paths, and async cancellation
  propagation.
- Tests must be deterministic, granular, regression-oriented, and free of network dependence.
- Tests must exercise observable behavior or invariants. Do not add
  tautological tests that merely restate the implementation without proving a
  contract.
- Do not use cardboard stubs or do-nothing doubles for repository code when a
  small behaviorally meaningful fake or a real collaborator is feasible.
- Favor real collaborators, precise fakes, and contract-level assertions over
  branch-chasing or value-mirroring tests that only inflate coverage numbers.
- Behavior changes should land with regression tests first when the failure mode is already understood, or in the same edit when the native/runtime surface makes strict red-green sequencing impractical.
- Time-dependent behavior should use `TimeProvider` so tests can control time explicitly.
- Coverage targets apply to unit-testable authored logic, not generated code or
  native shell/runtime glue that lacks deterministic seams. Those surfaces must
  still be validated through the local heavy lane instead of coverage-padding
  tests.

## Consequences

- The repository favors explicit orchestration and explicit failure channels over framework magic.
- Tests become more numerous and targeted, but they also localize regressions better.
- Test suites demand more intentional design because weak doubles and
  tautologies are no longer acceptable substitutes for behavior coverage.
- Code stays smaller because doctrine leaves the implementation layer.
- Analyzer policy now rejects a class of vague runtime exceptions before they leave a developer machine.
- Interop code becomes more explicit about ownership boundaries, which adds a
  small amount of wrapper code in exchange for safer cleanup and cleaner Native
  AOT alignment.
