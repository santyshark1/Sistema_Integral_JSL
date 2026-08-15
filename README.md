# JSL SentinelPro

Aplicación de escritorio **Windows** para diagnóstico de hardware, mantenimiento
preventivo y ciberseguridad. Práctica IV — Universidad Central, Ingeniería de Sistemas.

## Stack
- **C# · .NET 8 · WPF** (`net8.0-windows`), arquitectura **MVVM**
- SQLite · LibreHardwareMonitorLib + WMI · Windows Defender · BCrypt · LiveCharts

## Requisitos
- Windows 10/11 (64-bit) — **la app solo se EJECUTA en Windows**
- .NET 8 SDK
- Ejecutar como Administrador (acceso a sensores y Defender)

## Compilar
```bash
dotnet restore
dotnet build -c Release
```
> Nota: el proyecto **compila** también en Linux/macOS (gracias a
> `EnableWindowsTargeting`), útil para CI, pero **solo se ejecuta en Windows**.

## Ejecutar (en Windows)
```bash
dotnet run
```

## Documentación
- `ESPECIFICACION.md` — estado del proyecto, arquitectura, evaluación del lenguaje, roadmap
- `docs/Diagrama_Secuencia.md` — diagramas de secuencia (corregidos)
- `DOCUMENTACION.md` — documentación técnica · `ManualUsuario.html` — manual de usuario
