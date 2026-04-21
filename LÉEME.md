# AutoThemeSolarEngine

[![Release](https://img.shields.io/github/v/release/humbertoschoenwald/auto-theme-solar-engine?display_name=tag&label=release)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest)
[![CI/CD](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions)

Una aplicación ligera para la bandeja del sistema de Windows que cambia automáticamente el tema de tu PC según la hora local de salida y puesta del sol. Sin solicitudes a la nube, sin instaladores pesados y sin servicios en segundo plano.

[Read in English (README.md)](README.md)

## ¿Por qué usarlo?

- **Sin necesidad de nube:** Los cálculos solares se ejecutan completamente en tu dispositivo.
- **La privacidad es primero:** Utiliza la ubicación de Windows de forma local o coordenadas manuales. Sin APIs externas.
- **Opción independiente:** Usa el ejecutable self-contained sin instalar .NET aparte.
- **Programación inteligente:** Maneja correctamente las noches polares y el sol de medianoche.
- **Bajo consumo de recursos:** Diseñado para permanecer en la bandeja del sistema usando muy poca memoria RAM.

## Cómo funciona

La aplicación se ejecuta en el área de notificación, calcula el horario solar para tu ubicación exacta y cambia el modo Claro/Oscuro de Windows en el momento adecuado.

## Instalación

La aplicación deja `AutoThemeSolarEngine.exe`, `config.json`,
`installation.json`, `AutoThemeSolarEngine.log` y los scripts auxiliares de
update dentro de `LocalAppData\AutoThemeSolarEngine`. Elige el flavor que
quieras y ejecuta el bloque correspondiente en una sesión normal de PowerShell.

Pega todo el bloque. Si PowerShell deja el bloque pegado sin ejecutarlo,
presiona Enter una sola vez para correrlo.

### LocalAppData (Recomendado)

`LocalAppData` es la ruta recomendada por usuario y permite updates silenciosos
sin elevación.

Self-contained (Recomendado):

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Set-Location "$env:LOCALAPPDATA\AutoThemeSolarEngine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.05/auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe"
Move-Item -LiteralPath ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.05.exe" -Destination ".\AutoThemeSolarEngine.exe" -Force
Start-Process ".\AutoThemeSolarEngine.exe"
```

Después de abrir la app, introduce tus coordenadas manualmente o permite el
acceso a la ubicación de Windows.

## Privacidad

- Tus datos de ubicación nunca salen de tu computadora.
- Las coordenadas están cifradas mediante Windows Data Protection (DPAPI).
- Las coordenadas protegidas vuelven a ocultarse después de `Guardar y aplicar`.
- Sin telemetría, sin rastreo y sin necesidad de conexión a internet para las funciones principales.

## Cambios del repositorio

- Objetivo actualizado a .NET 11 preview 3 y C# 15 (preview).
- CI estricto en GitHub Actions con commitlint, cspell, compilación, analizadores, escaneo de vulnerabilidades y pruebas.
- La ventana de configuración sigue la preferencia Light/Dark de Windows.
- La interfaz soporta español e inglés mediante recursos JSON.

## Cobertura

Ejecuta `pwsh -NoLogo -NoProfile -File ./scripts/run-coverage.ps1` para generar
`artifacts/coverage/coverage.xml`.

El repositorio mide cobertura local de líneas y ramas y escribe el reporte
canónico en `artifacts/coverage/coverage.xml`. Si usas VS Code, instala
Coverage Gutters y apúntalo a `artifacts/coverage/coverage.xml`. La
configuración del workspace ya viene preparada para eso.

La métrica de cobertura excluye intencionalmente archivos generados, glue
nativo de UI Win32, bootstraps de DI y orquestación atada al sistema operativo;
esas superficies se validan en la lane local pesada.
