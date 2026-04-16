## v26.04.02 - 2026-04-16

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
- **themes:** make solar schedule timezone explicit (bee39aa)

### Refactoring

- **app:** rename source root to PascalCase (fab7727)
- **core:** use explicit invariant exceptions (2c2123e)
- **solar:** streamline NOAA schedule calculations (b3ab54e)

### Documentation

- **adr:** establish adr-first doctrine (6542643)
- **bibliography:** update pnpm bootstrap source (b3a33dd)
- **doctrine:** codify adr-first repository rules (e1a96fb)
- **updates:** prefer local appdata installs (7090b79)

### Tests

- **tests:** make sunset offset checks timezone-stable (8fce025)
- **updates:** cover silent GitHub updater flow (180c572)

### CI

- **tooling:** bootstrap pnpm with corepack (57648dc)

### Build

- **tooling:** enforce preview validation locally and in CI (790c52c)
- **app:** set assembly version to 26.04.02 (595a360)

### Chores

- **tooling:** derive repo config from doctrine (4c76db7)
- **codebase:** trim doctrinal comments (68faeaa)
