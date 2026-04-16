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
- Prefer measurable modern platform guidance where it fits the codebase, such as
  frozen collections for read-heavy lookup tables, structured logging message
  templates, span-friendly parsing on hot paths, and async cancellation
  propagation.
- Tests must be deterministic, granular, regression-oriented, and free of network dependence.
- Behavior changes should land with regression tests first when the failure mode is already understood, or in the same edit when the native/runtime surface makes strict red-green sequencing impractical.
- Time-dependent behavior should use `TimeProvider` so tests can control time explicitly.

## Consequences

- The repository favors explicit orchestration and explicit failure channels over framework magic.
- Tests become more numerous and targeted, but they also localize regressions better.
- Code stays smaller because doctrine leaves the implementation layer.
- Analyzer policy now rejects a class of vague runtime exceptions before they leave a developer machine.
