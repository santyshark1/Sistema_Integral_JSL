# Especificación — JSL SentinelPro
**Práctica IV · Universidad Central · Ingeniería de Sistemas**
Fecha: 2026-08-15 · Autores: Saray, David, Juan David, Alejandro

---

## 1. Qué es el proyecto (cómo está hoy)

**JSL SentinelPro** es una **aplicación de escritorio para Windows** de diagnóstico
de hardware, mantenimiento preventivo y ciberseguridad. El usuario inicia sesión,
la app lee sensores reales del equipo (CPU, RAM, disco, temperatura, red),
escanea amenazas con Windows Defender, ejecuta limpiezas/optimizaciones y genera
reportes en PDF.

Está **construido y funcional** en C# / .NET 8 (WPF), no es un prototipo vacío:
- **63 archivos C#** (~7.800 líneas), **17 vistas XAML**, arquitectura **MVVM**.
- Base de datos **SQLite** local con 8 tablas ya creadas.
- Integraciones **nativas reales**: WMI, PerformanceCounter, LibreHardwareMonitorLib
  y Windows Defender (`MpCmdRun.exe`).

### Estado por módulo
| Módulo | Vista | Estado |
|---|---|---|
| Autenticación (login, registro, reset por token) | LoginView, RegisterView, PasswordReset* | Implementado (BCrypt) |
| Dashboard de rendimiento | DashboardView | Implementado (gráficos LiveCharts) |
| Diagnóstico de hardware | DiagnosticoView | Implementado (WMI + LibreHardware) |
| Ciberseguridad / antivirus | CiberseguridadView | Implementado (Windows Defender) |
| Mantenimiento (limpieza, optimización) | MantenimientoView | Implementado |
| Reportes PDF (hardware, seguridad, mantenimiento) | ReportesView | Implementado |
| Empresas / centros asociados y citas | EmpresasView | Implementado |
| Gestión de usuarios | UsuariosView | Implementado |
| Configuración (SMTP, etc.) | ConfiguracionView | Implementado |
| Ayuda / manual | AyudaView | Implementado |

### Stack real
| Componente | Tecnología |
|---|---|
| Lenguaje / Framework | **C# · .NET 8 · WPF** (`net8.0-windows`) |
| Arquitectura | MVVM |
| Hardware | LibreHardwareMonitorLib + WMI + PerformanceCounter |
| Antivirus | Windows Defender API (`MpCmdRun.exe`, `MSFT_MpComputerStatus`) |
| Base de datos | SQLite (`Microsoft.Data.Sqlite`) |
| Gráficos | LiveChartsCore.SkiaSharpView.WPF |
| Contraseñas | BCrypt.Net-Next |
| Correo | SMTP (Gmail) vía `System.Net.Mail` |
| Distribución | `.exe` self-contained, `win-x64`, requiere ejecutar como Administrador |

---

## 2. ⚠️ El problema central: el código NO coincide con el documento

Esto es lo más importante de esta especificación.

- El **documento académico** (Práctica III) describe el stack como **React + Node.js
  + MySQL** en unas secciones y **Python + Flask** en otras — y se vende como
  **aplicación web** desplegada en Vercel/Railway (ver `historial_chat.txt`).
- El **software real de esta carpeta** es una **app de escritorio C#/.NET WPF** con
  **SQLite**. No es web, no es Node, no es Python.

Es decir: hay **tres stacks distintos** flotando entre documento y código. Antes de
noviembre hay que decidir **una sola narrativa** y que documento + software digan
lo mismo. Recomendación técnica: **adoptar en el documento el stack que YA está
construido** (C#/.NET WPF de escritorio + SQLite), porque el software ya funciona y
reescribirlo en web sería tirar el trabajo hecho. Eso obliga a corregir en el
documento: quitar React/Node/Python/Flask/MySQL/Vercel, rehacer el diagrama de
secuencia (flujo de escritorio, no "navegador → backend"), y borrar los vestigios
"SentinelPro" / Java / C.

> Nota: el nombre interno del producto en el código es **"SentinelPro"**, mientras
> el documento lo llama **"JSL"**. Unificar.

---

## 3. Evaluación del lenguaje (¿hay algo "más eficiente"?)

Premisa a revisar: *"según yo hay lenguajes más eficientes que este."*

**Conclusión corta: para ESTE dominio, C#/.NET es la elección correcta y de las más
eficientes disponibles. Cambiarlo empeoraría el proyecto, no lo mejoraría.**

Por qué C#/.NET es el acierto aquí:
- El programa vive de **APIs nativas de Windows** (WMI, PerformanceCounter, Windows
  Defender, sensores). .NET habla con ellas de forma directa; cualquier otro lenguaje
  tendría que envolverlas con más fricción.
- **LibreHardwareMonitorLib** — la librería central para temperaturas/voltajes/fans —
  **está escrita en .NET**. Usarla desde C# es nativo; desde Python/Node sería a
  través de puentes frágiles.
- Compila a **código nativo eficiente** y produce un **`.exe` self-contained** que el
  usuario ejecuta y ya. Es rápido y no necesita servidor.

Comparación honesta con las alternativas que suelen sonar:

| Lenguaje | ¿Más eficiente que C# aquí? | Realidad |
|---|---|---|
| **Python** | No | Más lento, y el acceso a sensores/Defender es indirecto y frágil. Bueno para prototipar, malo para este producto. |
| **Node.js/JS** | No | Es para servidores/web. No tiene acceso natural al hardware de Windows. |
| **Rust / C++** | En CPU puro, sí (marginal) | Más veloces y con menos memoria, **pero** construir la interfaz gráfica (equivalente a WPF) cuesta muchísimo más tiempo. No se justifica para una app de escritorio con UI rica. |
| **C#/.NET (actual)** | — | Mejor equilibrio rendimiento / velocidad de desarrollo / acceso nativo. **Quédense aquí.** |

**Recomendación:** no cambien de lenguaje. El "más eficiente" real para un
diagnóstico de hardware en Windows con esta UI es el que ya tienen. La energía debe
ir a **alinear el documento con el código**, no a reescribir el código.

*(Micro-optimizaciones dentro de C#, si algún día se necesitan: liberar `IDisposable`
de WMI/SQLite correctamente, cachear consultas WMI y no leer sensores en el hilo de
UI. No es urgente.)*

---

## 4. Estructura de la carpeta (ya organizada)

```
Pracica IV/
├── JSL-SentinelPro/                 ← el proyecto (único, ya sin anidamiento)
│   ├── JSL-SentinelPro.sln
│   ├── JSL-SentinelPro.csproj
│   ├── app.manifest                 (pide permisos de Administrador)
│   ├── App.xaml(.cs) / MainWindow.xaml(.cs)
│   ├── DOCUMENTACION.md / ManualUsuario.html / ESPECIFICACION.md
│   ├── .gitignore                   (evita que vuelva la basura)
│   ├── Resources/
│   └── src/
│       ├── Models/      (23 modelos de dominio)
│       ├── Core/        (13 servicios: Antivirus, Database, Hardware, Email, PDF…)
│       ├── Native/      (wrapper de LibreHardwareMonitor)
│       └── UI/          (Views, ViewModels, Converters — MVVM)
├── historial_chat.txt               (conversación del roadmap)
├── Roadmap_Practica_III.docx        (roadmap en Word)
└── Copia de JSL-SentinelPro-Completo.zip  (respaldo completo)
```

**Limpieza realizada (2026-08-15):** la carpeta pasó de **2,3 GB a 579 MB**.
Se eliminó (todo regenerable con `dotnet restore` / recompilar, o recuperable del ZIP):
- Anidamiento cuádruple `JSL-SentinelPro-Completo/…` (4 niveles) → aplanado a 1.
- **112 carpetas de paquetes NuGet sueltas** (~356 MB) — no las referencia nadie.
- **`TEMP_LIB/`** (~417 MB) y **`Librerias/`** — copias de paquetes/DLLs no usadas.
- **`bin/`** (~921 MB) y **`obj/`** (~23 MB) — salidas de compilación.
- **`.vs/`**, `lib/` vacía y el `*_wpftmp.csproj` temporal.

---

## 5. A dónde vamos (roadmap resumido)

Según `historial_chat.txt` / `Roadmap_Practica_III.docx`, 17 sábados del 08-ago al
28-nov-2026. Roles: **S**=Saray (frontend/forma), **D**=David (arquitectura/UML),
**J**=Juan David (backend/lógica), **A**=Alejandro (frontend/APA).

| Fase | Cuándo | Objetivo |
|---|---|---|
| 1 | Agosto | **Cerrar la incoherencia del stack** y alcance realista (Ada Test, malware). |
| 2 | Septiembre | Rehacer diagramas UML (casos de uso, secuencia real, despliegue, clases, DER). |
| 3 | Octubre | Software funcional alineado al documento. **Punto crítico.** |
| 4 | Noviembre | Pulido, forma, referencias APA, ensayo de sustentación. Colchón final. |

### Acción inmediata recomendada
1. **Decidir el stack oficial = C#/.NET WPF de escritorio** (lo que ya existe) y
   propagarlo al documento.
2. Corregir el **diagrama de secuencia** al flujo real de escritorio (sin "tiempo
   real" si no lo hay; sin navegador→backend).
3. Renombrar **"SentinelPro" → "JSL"** en código y documento.
4. Bajar afirmaciones sobre **Ada Test** (troyanos de hardware está fuera de alcance)
   y **detección por comportamiento** (no hay sandbox).
