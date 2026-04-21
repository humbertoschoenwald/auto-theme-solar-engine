## v26.04.04 - 2026-04-20 [YANKED]

> YANKED: Superseded by v26.04.05 because this release can reject a
> Windows-detected location during `Save and apply` and can leave silent
> updates staged without relaunching the new executable automatically.

### Refactoring

- **ui:** modernize native interop ownership (4ea8b34)
- **app:** rename branding and local install path (5b3cd46)
- **app:** normalize runtime naming boundaries (4883adf)

### Documentation

- **doctrine:** codify native interop policy (06b53c6)
- **doctrine:** codify install and coverage guidance (e240fdc)

### Tests

- **tests:** add light validation lane and coverage run (3bbfe09)

### Build

- **tooling:** enforce modern interop policy (35fff2b)
- **app:** prepare v26.04.04 package (f7b6c78)
