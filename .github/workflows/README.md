This setup intentionally does not include a separate `commitlint.yml`.

Commit linting runs inside `ci.yml` to avoid duplicated work.
`release.yml` only runs after CI succeeds on a push to `main`.
