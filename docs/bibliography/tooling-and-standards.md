# Tooling and Standards Bibliography

This bibliography lists the primary public references for the tools and standards currently adopted by repository doctrine.

## Core Standards

- [Architecture Decision Records](https://adr.github.io/)
- [CalVer](https://calver.org/)
- [Conventional Commits](https://www.conventionalcommits.org/)
- [MIT License](https://opensource.org/license/mit)
- [EditorConfig](https://editorconfig.org/)

## Windows Platform and Runtime

- [.NET](https://learn.microsoft.com/dotnet/)
- [C#](https://learn.microsoft.com/dotnet/csharp/)
- [Win32 API](https://learn.microsoft.com/windows/win32/)
- [Notification Area](https://learn.microsoft.com/windows/win32/shell/notification-area)
- [Windows Data Protection](https://learn.microsoft.com/windows/win32/seccrypto/data-protection)
- [Windows.Devices.Geolocation](https://learn.microsoft.com/uwp/api/windows.devices.geolocation)
- [Desktop Window Manager](https://learn.microsoft.com/windows/win32/dwm/dwm-overview)

## Testing and Serialization

- [xUnit.net](https://xunit.net/)
- [System.Text.Json](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)

## JavaScript Tooling and Repository Operations

- [Node.js](https://nodejs.org/)
- [Corepack](https://nodejs.org/api/corepack.html)
- [pnpm](https://pnpm.io/)
- [ESLint](https://eslint.org/docs/latest/)
- [Prettier](https://prettier.io/docs/en/)
- [CSpell](https://cspell.org/)
- [Commitlint](https://commitlint.js.org/)

## Delivery Automation

- [GitHub Actions](https://docs.github.com/actions)
- [Workflow Syntax for GitHub Actions](https://docs.github.com/actions/reference/workflows-and-actions/workflow-syntax)
- [Events That Trigger Workflows](https://docs.github.com/actions/reference/workflows-and-actions/events-that-trigger-workflows)
- [actions/setup-node](https://github.com/actions/setup-node)
- [actions/setup-dotnet](https://github.com/actions/setup-dotnet)

## Notes

- The repository intentionally uses undocumented `uxtheme.dll` exports for theme refresh compatibility. That behavior is documented as an exception in `ADR-0005` rather than treated as a standard public reference.
