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

1. Descarga uno de los archivos Windows x64 desde la [última release](https://github.com/humbertoschoenwald/auto-theme-solar-engine/releases/latest):
   - `auto-theme-solar-engine-win-x64-self-contained-v26.04.01.exe` incluye el runtime de .NET necesario.
   - `auto-theme-solar-engine-win-x64-framework-dependent-v26.04.01.exe` requiere que el runtime de escritorio de .NET o el SDK esté instalado.
2. Ejecútalo. No requiere instalador.
3. Introduce tus coordenadas manualmente o permite el acceso a la ubicación de Windows.

## Privacidad

- Tus datos de ubicación nunca salen de tu computadora.
- Las coordenadas están cifradas mediante Windows Data Protection (DPAPI).
- Sin telemetría, sin rastreo y sin necesidad de conexión a internet para las funciones principales.

## Cambios del repositorio

- Objetivo actualizado a .NET 11 preview y C# 15.
- CI estricto en GitHub Actions con commitlint, cspell, compilación, analizadores, escaneo de vulnerabilidades y pruebas.
- La ventana de configuración sigue la preferencia Light/Dark de Windows.
- La interfaz soporta español e inglés mediante recursos JSON.
