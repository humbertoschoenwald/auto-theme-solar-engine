# ADR-0008: Quality Gates, Versioning, and Automation

- Status: Accepted
- Date: 2026-04-16

## Context

The repository already enforces commit quality, spelling, formatting, build health, vulnerability checks, tests, and release automation through local hooks and GitHub Actions. Those workflows need a single doctrinal home.

## Decision

- Conventional Commits are mandatory for repository history.
- The local pre-push gate must run:
  - repository linting,
  - `dotnet restore`,
  - `dotnet format --verify-no-changes`,
  - release build,
  - automated tests,
  - coverage generation to `artifacts/coverage/coverage.xml`,
  - heavier publish-style validation that simulates the release environment,
  - a Native AOT self-contained publish smoke,
  - an updater rehearsal that proves LocalAppData legacy-layout migration and
    automatic relaunch behavior.
- Remote CI on push and pull request must validate:
  - commit messages,
  - repository linting,
  - restore,
  - build,
  - format and analyzer gate,
  - dependency vulnerability scan,
  - the light automated-test lane.
- The local gate is the full-fidelity superset of remote CI. Remote CI may run
  only the light test lane when heavier validation remains enforced locally
  before push.
- The canonical local coverage artifact is `artifacts/coverage/coverage.xml`
  so editor tooling such as Coverage Gutters can bind to a stable path.
- The coverage report measures authored deterministic logic. Generated files,
  native UI shell glue, dependency-injection bootstraps, and OS-bound runtime
  orchestration without stable unit seams are excluded from the line-coverage
  denominator and must instead be covered by the local heavy validation lane.
- Release automation runs only after CI succeeds for a push to `main`.
- Versioning uses CalVer in `vYY.MM.PATCH` format.
- Changelog and release notes are generated from commit history rather than hand-maintained as separate doctrine.
- Non-deliverable pushes to `main` may opt out of release publication with an explicit `[skip release]` marker in the head commit message.
- A push to `main` is allowed only when the author intentionally accepts the release side effects and the quality gates pass. Pull requests remain an optional collaboration tool, not a mandatory doctrinal workflow.

## Consequences

- `main` is a release-bearing branch, so changes there must already be validated.
- Commit hygiene and changelog quality are directly linked.
- Local and remote automation must stay intentionally aligned: local pre-push is
  stricter, and remote CI remains the lighter online confirmation lane.
- Updater regressions that depend on file replacement, shell launch, or other
  OS-managed behavior now have a defined local rehearsal lane instead of
  relying on release-day manual testing.
