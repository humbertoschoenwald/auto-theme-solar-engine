# ADR-0007: Toolchain and Package Management

- Status: Accepted
- Date: 2026-04-16

## Context

The repository combines a Windows desktop app written in modern .NET with a small JavaScript tooling surface for linting, spell-checking, commit validation, and changelog generation. The baseline already uses preview .NET and C# features, central NuGet management, and a Node-based toolchain.

The JavaScript tooling had been managed with npm. The repository now standardizes on pnpm to keep the package-manager story explicit and deterministic.

## Decision

- The .NET baseline is .NET 11 preview with C# 15 preview.
- The build baseline includes:
  - nullable reference types enabled,
  - implicit usings enabled,
  - warnings treated as errors,
  - documentation generation enabled,
  - central NuGet package management.
- NuGet lockfiles remain checked in for deterministic dependency resolution.
- Node.js 25 is the repository baseline for JavaScript tooling.
- pnpm is the only supported JavaScript package manager for this repository.
- `package.json` must declare a `packageManager` field so automation and contributors resolve the same pnpm version.
- Repository hooks, local scripts, and GitHub Actions must use pnpm instead of npm for install and script execution.
- Workspace editor settings may disable telemetry, automatic updates, and indentation guesswork when those defaults reduce repository drift and make saved-file execution deterministic.
- Windows release publishing remains focused on one x64 self-contained Native
  AOT executable.
- Local machines that run the Native AOT publish lane must have the Windows
  linker prerequisites installed, including the Visual Studio Desktop
  Development with C++ workload or an equivalent supported MSVC toolchain.

## Consequences

- Contributors need a working pnpm installation or a workflow that can honor the `packageManager` field.
- The repository keeps two dependency ecosystems, but each one is pinned
  through a single declared toolchain.
- Package-manager drift moves from convention to explicit policy.
- Native AOT publish validation now depends on a Windows-native toolchain in
  addition to the .NET SDK.
