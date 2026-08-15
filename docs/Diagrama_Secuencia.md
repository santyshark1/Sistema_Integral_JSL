# Diagrama de Secuencia — JSL SentinelPro (corregido)

> Reemplaza la **Figura 21** del documento. Refleja la arquitectura **real**:
> aplicación de **escritorio C#/.NET WPF** con patrón **MVVM**, servicios en proceso,
> APIs nativas de Windows y base de datos **SQLite local**.
> **No hay** navegador, **no hay** backend Python/Node, **no hay** servidor remoto.

---

## 1. Flujo principal — Diagnóstico de hardware

```mermaid
sequenceDiagram
    actor Usuario
    participant V as DiagnosticoView<br/>(WPF)
    participant VM as DiagnosticoViewModel
    participant HM as HardwareMonitor / TemperatureMonitor<br/>(Core)
    participant WIN as APIs nativas Windows<br/>(WMI · PerformanceCounter · LibreHardwareMonitorLib)
    participant DB as DatabaseService<br/>(SQLite local)

    Usuario->>V: Clic "Iniciar escaneo"
    V->>VM: StartScanCommand
    activate VM
    VM-->>V: IsScanning = true (barra de progreso)

    loop Por cada componente (CPU, RAM, disco, temperatura, red…)
        VM->>HM: GetCpuInfo() / GetMemoryInfo() / GetDiskInfo()
        HM->>WIN: Consulta WMI / lee sensores
        WIN-->>HM: Datos reales del equipo
        HM-->>VM: CpuInfo, MemoryInfo, DiskInfo, Temperaturas
        VM-->>V: Actualiza progreso y resultados
    end

    VM->>VM: GenerateRecommendations() (umbrales)
    VM->>DB: SaveHardwareScanAsync(scan)
    DB-->>VM: OK (fila insertada)
    VM->>DB: LoadHistory()
    DB-->>VM: Historial de escaneos
    VM-->>V: Resultados + estado "Completado"
    deactivate VM
    V-->>Usuario: Muestra diagnóstico e historial
```

---

## 2. Flujo — Detección de amenazas (antivirus)

```mermaid
sequenceDiagram
    actor Usuario
    participant V as CiberseguridadView<br/>(WPF)
    participant VM as CiberseguridadViewModel
    participant AV as AntivirusEngine<br/>(Core)
    participant DEF as Windows Defender<br/>(MpCmdRun.exe · MSFT_MpComputerStatus)
    participant DB as DatabaseService<br/>(SQLite local)

    Usuario->>V: Clic "Escanear amenazas"
    V->>VM: StartScanCommand
    activate VM
    VM->>AV: GetProtectionStatus()
    AV->>DEF: Consulta estado (WMI ROOT\Microsoft\Windows\Defender)
    DEF-->>AV: Estado de protección
    VM->>AV: RunScan()
    AV->>DEF: Ejecuta MpCmdRun.exe -Scan
    DEF-->>AV: Amenazas detectadas
    AV-->>VM: Lista de ThreatScanResult
    VM->>DB: Guarda ThreatDetections
    DB-->>VM: OK
    VM-->>V: Amenazas activas / neutralizadas
    deactivate VM
    V-->>Usuario: Muestra resultados del escaneo
```

---

## 3. Flujo — Generar reporte PDF

```mermaid
sequenceDiagram
    actor Usuario
    participant VM as ViewModel (Diagnostico / Reportes)
    participant RS as HardwareReportService
    participant DB as DatabaseService<br/>(SQLite)
    participant PDF as PdfDocumentWriter

    Usuario->>VM: Clic "Generar reporte"
    VM->>RS: GeneratePcRecognitionPdfAsync()
    RS->>DB: Lee escaneos e historial
    DB-->>RS: Datos
    RS->>PDF: Escribe documento
    PDF-->>RS: Ruta del archivo .pdf
    RS-->>VM: Ruta del PDF
    VM-->>Usuario: "Reporte generado en <ruta>"
```

---

## 4. Qué se corrigió respecto a la Figura 21 original

| Error en la Figura 21 | Corrección aquí |
|---|---|
| Rótulo **"Backend (Python)"** | No existe backend Python. Los servicios (`HardwareMonitor`, `AntivirusEngine`, `DatabaseService`) son clases **C#/.NET en proceso**. |
| Flujo web **navegador → backend** | Es **escritorio WPF**: `View → ViewModel → Servicio Core → API nativa/SQLite`. |
| **"Enviar amenazas detectadas"** salía de la **Base de Datos** | Las amenazas las detecta el **AntivirusEngine (Windows Defender)**, no la BD. La BD solo **persiste** el resultado. |
| Servidor/BD remotos (MySQL/Railway) | **SQLite local**, sin servidor. |
| Lectura de hardware desde el navegador | El navegador no puede leer CPU/RAM/temperatura; aquí se lee con **WMI + LibreHardwareMonitorLib** directamente en el equipo. |
| "Tiempo real" prometido | La barra de progreso es **retroalimentación de UI**; el diagnóstico es una operación puntual bajo demanda (no streaming continuo). |

> **Nombres de participantes** = clases reales del código (`DiagnosticoViewModel`,
> `HardwareMonitor`, `AntivirusEngine`, `DatabaseService`, `PdfDocumentWriter`),
> para que el diagrama sea trazable al software entregado.
