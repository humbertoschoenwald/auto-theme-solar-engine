# ADR-0019: Changelog Lifecycle and Release Notes

**Status:** Accepted
**Date:** 2026-04-16

## Context

The repository already generates release notes from commit history, but the
desired workflow is more explicit: after a release, new commits should appear as
an `Unreleased` section until the next release promotes them into a tagged
section. Release notes should come from that same changelog source so commit
history is not summarized twice in different ways.

## Decision

- `CHANGELOG.md` is generated from Conventional Commits and remains the single
  release-history artifact in the repository.
- The top changelog section is always `Unreleased` and contains commits since
  the latest release tag.
- When a release is created, the current `Unreleased` section is promoted into a
  tagged section for that CalVer release and a fresh `Unreleased` section is
  created.
- GitHub release notes are derived directly from the tagged changelog section
  for that release.
- Auto-generated changelog maintenance commits must be excluded from future
  changelog sections and marked to skip recursive CI and release loops.

## Alternatives Considered

| Option                                                   | Pros                     | Cons                                             |
| -------------------------------------------------------- | ------------------------ | ------------------------------------------------ |
| Hand-edit `CHANGELOG.md`                                 | Flexible                 | Drifts from commit history and is easy to forget |
| Generate only release-time notes                         | Minimal repository churn | No visible unreleased history on `main`          |
| Keep generated `Unreleased` plus tagged release sections | One clear history model  | Requires careful workflow ordering               |

## Consequences

- **Positive:** Release notes and repository history come from one source.
- **Positive:** `main` always shows what has changed since the last release.
- **Negative:** Automation needs to preserve manual notes such as `YANKED`
  annotations when present.
- **Risks:** Poorly filtered automation commits could pollute changelog history
  unless the workflow excludes them deliberately.
