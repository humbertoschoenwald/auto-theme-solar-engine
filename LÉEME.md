# Auto Theme — Solar Engine

[![Release](https://img.shields.io/github/v/release/humbertoschoenwald/auto-theme-solar-engine?display_name=tag&label=release)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest)
[![CI/CD](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/humbertoschoenwald/auto-theme-solar-engine/actions)

Una aplicación ligera para la bandeja del sistema de Windows que cambia automáticamente el tema de tu PC según la hora local de salida y puesta del sol. Sin solicitudes a la nube, sin instaladores pesados y sin servicios en segundo plano.

[Read in English (README.md)](README.md)

## ¿Por qué usarlo?

- **Sin necesidad de nube:** Los cálculos solares se ejecutan completamente en tu dispositivo.
- **La privacidad es primero:** Utiliza la ubicación de Windows de forma local o coordenadas manuales. Sin APIs externas.
- **Opción independiente:** Usa el ejecutable self-contained si no quieres instalar .NET.
- **Opción ligera:** Usa el ejecutable framework-dependent si ya tienes el runtime de escritorio de .NET o el SDK instalado.
- **Programación inteligente:** Maneja correctamente las noches polares y el sol de medianoche.
- **Bajo consumo de recursos:** Diseñado para permanecer en la bandeja del sistema usando muy poca memoria RAM.

## Cómo funciona

La aplicación se ejecuta en el área de notificación, calcula el horario solar para tu ubicación exacta y cambia el modo Claro/Oscuro de Windows en el momento adecuado.

## Instalación

La aplicación soporta dos ubicaciones de instalación y deja el ejecutable
descargado dentro de esa carpeta. Elige el flavor que quieras y ejecuta el
bloque correspondiente. Cada bloque termina abriendo automáticamente el
ejecutable descargado.

### LocalAppData (Recomendado)

`LocalAppData` es la ruta recomendada por usuario y permite updates silenciosos
sin elevación. Usa una sesión normal de PowerShell.

Self-contained:

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Set-Location "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
```

Framework-dependent:

```powershell
New-Item -ItemType Directory -Force -Path "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Set-Location "$env:LOCALAPPDATA\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
```

### Program Files (PowerShell como administrador)

`Program Files` sigue disponible para instalaciones orientadas a toda la
máquina. Abre PowerShell como administrador y ejecuta uno de estos bloques. En
el primer arranque, la app registra el task elevado para el updater
silencioso.

Self-contained:

```powershell
New-Item -ItemType Directory -Force -Path "$env:ProgramFiles\Auto Theme — Solar Engine"
Set-Location "$env:ProgramFiles\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-self-contained-v26.04.03.exe"
```

Framework-dependent:

```powershell
New-Item -ItemType Directory -Force -Path "$env:ProgramFiles\Auto Theme — Solar Engine"
Set-Location "$env:ProgramFiles\Auto Theme — Solar Engine"
Invoke-WebRequest -Uri "https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/download/v26.04.03/auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe" -OutFile ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
Start-Process ".\auto-theme-solar-engine-win-x64-framework-dependent-v26.04.03.exe"
```

Después de abrir la app, introduce tus coordenadas manualmente o permite el
acceso a la ubicación de Windows.

## Privacidad

- Tus datos de ubicación nunca salen de tu computadora.
- Las coordenadas están cifradas mediante Windows Data Protection (DPAPI).
- Sin telemetría, sin rastreo y sin necesidad de conexión a internet para las funciones principales.

## Cambios del repositorio

- Objetivo actualizado a .NET 11 preview 3 y C# 15 (preview).
- CI estricto en GitHub Actions con commitlint, cspell, compilación, analizadores, escaneo de vulnerabilidades y pruebas.
- La ventana de configuración sigue la preferencia Light/Dark de Windows.
- La interfaz soporta español e inglés mediante recursos JSON.
