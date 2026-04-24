# ADR-0020: EditorConfig and Analyzer Projection

- Status: Accepted
- Date: 2026-04-23

## Context

The repository already enforces a strict .NET and C# baseline, but the exact
machine-enforced editor and analyzer projection had been split between
`.editorconfig`, build properties, and inline TODO comments. The requested
baseline strengthens that projection further: repository-root editor defaults,
path-specific indentation rules, a binary analyzer severity doctrine, and a
documented answer for future closed-alternative enforcement.

Those changes need an ADR because this repository is ADR-first, and some of the
requested rules interact with existing doctrine:

- `ADR-0009` keeps analyzer policy limited to rules the active SDK and
  `dotnet format` can enforce reliably.
- `ADR-0014` keeps repository-specific static policy source-only where Roslyn
  cannot scope generated code cleanly.
- `ADR-0015` pins the preview SDK and defers product use of C# `union`
  declarations until the toolchain is repository-ready.

The repository also needs to avoid false-positive policy. If a rule cannot be
scoped precisely in stock `.editorconfig`, it must not be projected as an
over-broad repository-wide diagnostic just to approximate the intent.

## Decision

- `.editorconfig` is the machine-grade projection for repository formatting,
  naming, and analyzer severity doctrine. It is a derived artifact, not an
  independent policy source.
- Repository text defaults are:
  - UTF-8 encoding,
  - LF line endings,
  - final newline required,
  - trailing whitespace trimmed by default,
  - 100-character line target,
  - hard tabs at width 8 as the general fallback for file types that do not
    carry a language-specific override.
- Language and file-type overrides are path-scoped:
  - Python uses four spaces.
  - Markdown uses two spaces, keeps trailing whitespace available for explicit
    hard line breaks, and does not carry a max-line rule.
  - Shell, JavaScript module scripts, JSON-family files, YAML, TOML, XML, MSBuild
    files, solution files, and repository rule files use two spaces.
  - PowerShell uses four spaces.
  - Authored C# uses four spaces, forbids tabs for indentation, requires the
    repository file header, and keeps the 100-character line target.
- Workspace settings may disable indentation detection and other drift-prone
  editor behavior, but they must not silently contradict `.editorconfig`.
  `.vscode` remains a workspace projection layer under `ADR-0011`, while the
  formatting doctrine itself stays derived from `.editorconfig`.
- Analyzer severity doctrine is binary:
  - `error` is the default for generally applicable enabled diagnostics.
  - `none` is allowed only for explicit path-scoped exceptions with a local
    rationale comment.
  - Repository configs must not introduce `warning`, `suggestion`, `silent`,
    `info`, or `default` severities.
- The `.NET` build projection in `Directory.Build.props` must explicitly carry
  the companion properties that `.editorconfig` cannot express, including:
  - preview language mode supported by the pinned SDK,
  - nullable enabled,
  - implicit usings enabled,
  - warnings treated as errors,
  - .NET analyzers enabled,
  - documentation file generation,
  - category analysis modes set to `All`.
- Project files may keep narrower operating-system-specific target frameworks
  such as `net11.0-windows10.0.19041.0`. The common build projection must not
  erase those OS-specific product requirements just to restate the shared
  `.NET 11` baseline.
- Closed state, result, and message alternatives must remain explicit and
  exhaustively handled. While the pinned toolchain still does not provide a
  repository-ready end-to-end path for authored C# `union` declarations, the
  repository keeps modeling those alternatives with explicit records, enums,
  and exhaustive switch handling as required by `ADR-0015`.
- Diagnostic ID `ASE0001` is reserved for a future repository analyzer that
  will enforce approved union declarations once the pinned SDK and runtime make
  that path repository-ready. The repository must not claim that this rule is
  active before the analyzer exists.
- Semantic naming rules that stock `.editorconfig` cannot scope precisely, such
  as "only handler types must end with `Handler`", must not be projected as
  over-broad all-class rules. Those rules stay doctrinal until a repository
  analyzer can enforce them without false positives.

## Alternatives Considered

| Option | Pros | Cons |
| ------ | ---- | ---- |
| Keep the old `.editorconfig` and leave TODO comments in place | Minimal immediate churn | Doctrine stays split and TODOs remain policy-shaped gaps |
| Project every requested rule literally even when stock `.editorconfig` cannot scope it safely | Matches the request text verbatim | Creates false positives and unstable builds |
| Record the stronger projection doctrine in ADR, reserve future analyzer IDs, and project only enforceable rules today | ADR-first, reviewable, and mechanically sound | Requires both doctrine and config edits before the code cleanup |

## Consequences

- **Positive:** The repository now has a doctrinal home for its strict
  `.editorconfig` and analyzer projection.
- **Positive:** Deferred rules such as union enforcement no longer live as
  orphaned TODO comments in a config file.
- **Positive:** Workspace settings and build properties have a clear authority
  relationship with `.editorconfig`.
- **Negative:** A stricter analyzer baseline can surface substantial existing
  cleanup work when projected onto current source.
- **Risks:** If the repository starts reserving future analyzer IDs without
  documenting the activation conditions, contributors could mistake inert config
  keys for active enforcement.
