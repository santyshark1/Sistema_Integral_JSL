# Diagrama de Clases — JSL SentinelPro

> Modelo de dominio y capa de servicios **reales** del código (C#/.NET, MVVM).
> Atributos tomados de `src/Models/` y servicios de `src/Core/`.

## 1. Modelo de dominio (entidades que persisten en SQLite)

```mermaid
classDiagram
    class User {
        +int Id
        +string FullName
        +string Email
        +string Username
        +string PasswordHash
        +string AccountType
        +DateTime CreatedAt
        +DateTime? LastLogin
        +bool IsActive
    }
    class PasswordResetToken {
        +int Id
        +string Email
        +string Token
        +DateTime ExpiresAt
        +bool IsUsed
        +DateTime CreatedAt
    }
    class HardwareScan {
        +int Id
        +DateTime ScanDate
        +double CpuUsage
        +ulong RamUsedBytes
        +ulong RamTotalBytes
        +ulong DiskUsedBytes
        +double MaxTemperature
        +string Status
        +List~string~ ComponentsAnalyzed
    }
    class ThreatScanResult {
        +int Id
        +DateTime DetectionDate
        +string ThreatName
        +string ThreatType
        +string FilePath
        +string ActionTaken
        +string Severity
        +string Status
    }
    class MaintenanceLog {
        +int Id
        +DateTime ActionDate
        +string ActionType
        +long SpaceFreedBytes
        +string Details
    }
    class SystemSnapshot {
        +int Id
        +DateTime Timestamp
        +double CpuUsage
        +double RamUsedPercent
        +double DiskUsedPercent
        +double MaxTemp
        +double NetworkSpeedMbps
    }
    class CompanyPartner {
        +int Id
        +string Name
        +string Specialty
        +string City
        +double Rating
        +bool HasWarranty
    }
    class PartnerAppointment {
        +int Id
        +DateTime RequestedAt
        +string CompanyName
        +string Specialty
        +string Status
    }

    User "1" --> "0..*" HardwareScan : realiza
    User "1" --> "0..*" ThreatScanResult : registra
    User "1" --> "0..*" MaintenanceLog : ejecuta
    User "1" --> "0..*" PasswordResetToken : solicita
    CompanyPartner "1" --> "0..*" PartnerAppointment : recibe
    HardwareScan "1" ..> "0..*" SystemSnapshot : muestrea
```

## 2. Objetos de lectura de hardware (no persisten; se muestran en pantalla)

```mermaid
classDiagram
    class CpuInfo {
        +string Name
        +int CoreCount
        +int ThreadCount
        +double CurrentClockSpeed
        +double UsagePercent
    }
    class MemoryInfo {
        +double TotalGB
        +double UsedGB
    }
    class DiskInfo {
        +double TotalGB
        +double UsedGB
    }
    class GpuInfo {
        +string Name
        +double MemoryGB
    }
    class NetworkInfo {
        +string IpAddress
        +string MacAddress
        +double SpeedMbps
        +bool IsConnected
    }
    class TemperatureReading {
        +string HardwareName
        +string SensorName
        +double ValueCelsius
    }
```

## 3. Capa de servicios (MVVM)

```mermaid
classDiagram
    class BaseViewModel {
        <<abstract>>
        +SetProperty()
    }
    class DiagnosticoViewModel
    class CiberseguridadViewModel
    class MantenimientoViewModel

    class HardwareMonitor {
        +GetCpuInfo() CpuInfo
        +GetMemoryInfo() MemoryInfo
        +GetDiskInfo() List~DiskInfo~
    }
    class TemperatureMonitor {
        +GetAllTemperatures() List~TemperatureReading~
    }
    class AntivirusEngine {
        +GetProtectionStatus()
        +RunScan() List~ThreatScanResult~
    }
    class DatabaseService {
        +SaveHardwareScanAsync()
        +GetUser()
        +SaveThreatDetections()
    }
    class EmailService {
        +SendWelcomeAsync()
        +SendPasswordResetAsync()
    }

    BaseViewModel <|-- DiagnosticoViewModel
    BaseViewModel <|-- CiberseguridadViewModel
    BaseViewModel <|-- MantenimientoViewModel
    DiagnosticoViewModel --> HardwareMonitor
    DiagnosticoViewModel --> TemperatureMonitor
    DiagnosticoViewModel --> DatabaseService
    CiberseguridadViewModel --> AntivirusEngine
    CiberseguridadViewModel --> DatabaseService
    HardwareMonitor ..> CpuInfo
    AntivirusEngine ..> ThreatScanResult
    DatabaseService ..> HardwareScan
```

> **Nota (corrige el documento):** no hay clases de "backend" separado ni ORM web.
> La persistencia es **SQLite local** vía `DatabaseService`; los servicios Core son
> clases C# en el mismo proceso de la aplicación de escritorio.