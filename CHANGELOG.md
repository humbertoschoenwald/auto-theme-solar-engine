# Changelog

## Unreleased

### Features

- **ui:** add localized native settings and silent updates (7715698)
- **scripts:** add PowerShell install entrypoints (83657a4)

### Fixes

- **tooling:** flatten lint entrypoint (2d0abee)
- **ci:** bootstrap pnpm after setup-node (2d20e90)
- **ci:** expose node runtime to workflow jobs (f23ac4f)
- **ci:** run node gates in powershell (39114b4)
- **ci:** pin pnpm to the active node toolchain (e43b172)
- **ci:** install pnpm from the active node runtime (8368929)
- **ci:** resolve pnpm from npm global prefix (6f4652f)
- **tooling:** use cspell-cli in lint scripts (45a9e7c)

### Refactoring

- **app:** rename source root to PascalCase (fab7727)
- **core:** use explicit invariant exceptions (2c2123e)
- **solar:** streamline NOAA schedule calculations (b3ab54e)

### Documentation

- **adr:** establish adr-first doctrine (6542643)
- **bibliography:** update pnpm bootstrap source (b3a33dd)
- **doctrine:** codify adr-first repository rules (e1a96fb)

### Tests

- **tests:** make sunset offset checks timezone-stable (8fce025)

### CI

- **tooling:** bootstrap pnpm with corepack (57648dc)

### Build

- **tooling:** enforce preview validation locally and in CI (790c52c)

### Chores

- **tooling:** derive repo config from doctrine (4c76db7)
- **codebase:** trim doctrinal comments (68faeaa)

## v26.04.01 - 2026-04-13

### Fixes

- **ui:** use full app name and stable settings background (0bc4e42)
- **themes:** stop standard schedule crash (42a727e)

### Documentation

- update release naming and Spanish readme (4787df5)

### CI

- **release:** publish v-prefixed dual Windows assets (68cfb55)

## 26.04.00 - 2026-04-13 [YANKED]

> YANKED: Superseded by v26.04.01 because the initial release can crash
> while rendering Standard daylight schedule status.

### Features

- **core:** add result primitives (d4d26c9)
- **infrastructure:** add app support services (efc4038)
- **locations:** add coordinate resolution (c880648)
- **solar:** add daylight schedule calculations (4ac2837)
- **system-host:** add host configuration model (df4461b)
- **themes:** add Windows theme orchestration (610f461)
- **system-host:** add lifecycle persistence (ce25cf6)
- **ui:** add native tray settings interface (6736bad)
- **app:** wire tray application host (38c4e19)

### Fixes

- **release:** omit initial changelog placeholder (08c8c68)
- **release:** preserve full calver tag output (cdd2cb2)

### Documentation

- add project documentation (838bc9b)
- **changelog:** release 26.04.00 (bce9248)

### CI

- add validation and release automation (92fcd1a)

### Chores

- **tooling:** add repository standards and toolchain (55b420f)
