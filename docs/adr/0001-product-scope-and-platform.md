# ADR-0001: Product Scope and Platform

- Status: Accepted
- Date: 2026-04-16

## Context

The repository builds a small desktop utility, not a general framework or cross-platform product. The codebase, release workflow, and runtime assets all target Windows x64 and assume direct integration with Windows theme and location capabilities.

The user-facing README is intentionally light. Doctrine must therefore describe the true scope and platform constraints.

## Decision

- The product is a Windows-native notification-area application that switches the Windows light and dark theme from local sunrise and sunset calculations.
- The product is intentionally offline for its core behavior. It does not depend on cloud APIs, telemetry backends, or background services.
- The supported platform is Windows 10 build 19041 or later on x64.
- The application runs in the per-user desktop session. It is not a Windows service and does not require a separate background daemon.
- Release distribution provides two Windows x64 single-file executables:
  - a self-contained build that includes the required runtime,
  - a framework-dependent build for machines that already have the desktop runtime or SDK installed.
- `README.md` remains a lightweight user-facing overview. It is not a specification document.

## Consequences

- Cross-platform abstractions are unnecessary unless a later ADR expands the supported platform set.
- Release automation and local validation should continue to assume Windows execution.
- Product-facing documentation can stay short because operational doctrine lives elsewhere.
