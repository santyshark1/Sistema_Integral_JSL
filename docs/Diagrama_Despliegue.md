# Diagrama de Despliegue — JSL SentinelPro

> Es el diagrama que **resuelve la confusión "web vs escritorio"** del documento.
> Todo corre en **la máquina Windows del usuario**. No hay Vercel, ni Railway, ni
> servidor remoto. La única salida a red es el envío de correos por SMTP.

```mermaid
flowchart TB
    subgraph PC["💻 Equipo del usuario — Windows 10/11 (64-bit)"]
        direction TB
        APP["<<artifact>> JSL-SentinelPro.exe<br/>App WPF .NET 8 (self-contained)<br/>ejecutada como Administrador"]
        DB[("<<device>> SQLite<br/>base de datos local<br/>(archivo .db en AppData)")]
        CFG[("config.json<br/>(credenciales SMTP en AppData)")]
        DEF["<<component>> Windows Defender<br/>MpCmdRun.exe"]
        WMI["<<component>> WMI / PerformanceCounter<br/>+ LibreHardwareMonitorLib"]
        HW["<<device>> Hardware<br/>CPU · RAM · Disco · Sensores"]

        APP -->|"Microsoft.Data.Sqlite"| DB
        APP -->|"lee/escribe"| CFG
        APP -->|"escaneo de amenazas"| DEF
        APP -->|"consulta sensores"| WMI
        WMI -->|"lee"| HW
    end

    SMTP["☁️ Servidor SMTP (Gmail)<br/>smtp.gmail.com:587"]
    APP -->|"System.Net.Mail (TLS)<br/>correos de bienvenida / reset"| SMTP

    style APP fill:#1f6feb,color:#fff
    style DB fill:#238636,color:#fff
    style SMTP fill:#8957e5,color:#fff
```

## Nodos

| Nodo | Qué es | Dónde vive |
|---|---|---|
| **JSL-SentinelPro.exe** | La aplicación WPF completa (UI + servicios) | Equipo del usuario |
| **SQLite (.db)** | Persistencia local (usuarios, escaneos, amenazas, logs) | Equipo del usuario (AppData) |
| **config.json** | Credenciales SMTP (no hardcodeadas) | Equipo del usuario (AppData) |
| **Windows Defender** | Motor antivirus real invocado por la app | Sistema operativo |
| **WMI + LibreHardwareMonitor** | Fuente de datos de hardware/sensores | Sistema operativo |
| **Servidor SMTP (Gmail)** | Único componente externo; solo para correos | Nube (Google) |

> **Requisito clave:** la app necesita **permisos de Administrador** (UAC) para
> leer sensores y ejecutar Windows Defender — por eso se distribuye con `app.manifest`
> que solicita elevación. Esto confirma que es **escritorio**, no web.