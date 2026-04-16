# ADR-0011: Workspace Config Projection

**Status:** Accepted
**Date:** 2026-04-16

## Context

The repository currently contains duplicated editor and task files under both
`.cursor` and `.vscode`. That duplication creates drift and leaves stale launch
profiles in the repository. The operational doctrine already states that
`.cursor/rules` is the binding agent layer, but it does not yet define where
workspace editor, launch, and task projections belong.

## Decision

- `.cursor` is reserved for Cursor-specific agent behavior, primarily
  `.cursor/rules/**/*.mdc`.
- Repository editor settings, debug profiles, and task definitions belong in
  `.vscode`.
- Duplicate workspace projections across `.cursor` and `.vscode` are not
  allowed.
- `.vscode/launch.json` and `.vscode/tasks.json` may exist only when they point
  at valid repository entrypoints and remain useful for contributors.
- Stale editor convenience files must be removed instead of preserved as dead
  compatibility baggage.

## Alternatives Considered

| Option                                                               | Pros                                            | Cons                                               |
| -------------------------------------------------------------------- | ----------------------------------------------- | -------------------------------------------------- |
| Keep duplicate files in both folders                                 | Zero migration effort                           | Drift, stale launch paths, and ambiguous ownership |
| Keep everything under `.cursor`                                      | Cursor-centric                                  | VS Code tooling ignores that location              |
| Use `.vscode` for workspace projections and `.cursor` only for rules | Matches tool expectations and keeps roles clear | Requires deleting old duplicates                   |

## Consequences

- **Positive:** Workspace behavior has one location and one owner.
- **Positive:** Cursor rules stay focused on agent doctrine instead of editor
  projections.
- **Negative:** Any stale personal expectation around `.cursor/tasks.json` or
  `.cursor/launch.json` stops working once those files are removed.
- **Risks:** Broken `.vscode` launch or task paths would hurt contributor
  workflow, so they must stay validated against real project paths.
