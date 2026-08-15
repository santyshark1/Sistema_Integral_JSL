# JSL SentinelPro - Sistema Integral de Diagnostico, Mantenimiento Preventivo y Ciberseguridad

## Documentacion Tecnica

### Requisitos del Sistema
- **Sistema Operativo:** Windows 10/11 (64-bit)
- **Permisos:** Ejecutar como Administrador (UAC elevation obligatorio)
- **Runtime:** .NET 8 Desktop Runtime (incluido en publicacion self-contained)
- **Hardware:** 4GB RAM minimo, 500MB espacio disco
- **Red:** Conexion internet para correos y actualizaciones

### Stack Tecnologico
| Componente | Tecnologia |
|------------|-----------|
| Framework UI | WPF .NET 8 |
| Lenguaje | C# |
| Hardware real | LibreHardwareMonitorLib + WMI |
| Antivirus real | Windows Defender API (MpCmdRun.exe) |
| Base de datos | SQLite (Microsoft.Data.Sqlite) |
| Graficos | LiveChartsCore.SkiaSharpView.WPF |
| Hash contrasenas | BCrypt.Net-Next |
| Correos | System.Net.Mail (SMTP Gmail) |
| Arquitectura | MVVM |

### Paquetes NuGet
```
LibreHardwareMonitorLib (0.9.4)
Microsoft.Data.Sqlite (8.0.10)
LiveChartsCore.SkiaSharpView.WPF (2.0.0-rc2)
BCrypt.Net-Next (4.0.3)
SkiaSharp (2.88.8)
System.Management (8.0.0)
```

### Estructura de Base de Datos SQLite
- **Users:** Usuarios del sistema con autenticacion BCrypt
- **PasswordResets:** Tokens de recuperacion de contrasena (expiran en 1 hora)
- **HardwareScans:** Registro de escaneos de hardware
- **ThreatDetections:** Amenazas detectadas por Windows Defender
- **MaintenanceLogs:** Registro de limpiezas y optimizaciones
- **SystemSnapshots:** Capturas de rendimiento cada 5 minutos
- **CompanyPartners:** Centros tecnicos asociados

### APIs Nativas Utilizadas
1. **WMI (Windows Management Instrumentation):**
   - Win32_Processor: Informacion del CPU
   - Win32_OperatingSystem: Memoria y tiempo de actividad
   - Win32_LogicalDisk: Informacion de discos
   - Win32_PerfFormattedData_Tcpip_NetworkInterface: Velocidad de red
   - ROOT\Microsoft\Windows\Defender: Estado de Windows Defender

2. **PerformanceCounter:**
   - Processor % Processor Time: Uso de CPU en tiempo real

3. **LibreHardwareMonitorLib:**
   - Sensores de temperatura, voltaje, ventiladores y carga

4. **Windows Defender (MpCmdRun.exe):**
   - Escaneo de archivos y sistema completo
   - Consulta de estado de proteccion

### Configuracion de Correo SMTP
Para habilitar el envio de correos de bienvenida y recuperacion:
1. Cree una cuenta Gmail dedicada
2. Genere una App Password en configuracion de seguridad de Google
3. Vaya a Configuracion > Configuracion de Correo SMTP en la aplicacion
4. Ingrese servidor: smtp.gmail.com, puerto: 587, usuario y App Password
5. Guarde y use "Probar Correo" para verificar

### Instalacion y Compilacion
1. Abra `JSL-SentinelPro.sln` en Visual Studio 2022 Community
2. Restaure los paquetes NuGet (automatico)
3. Compile en Release x64
4. Publique como self-contained para distribucion sin runtime

### Consideraciones de Seguridad
- La aplicacion requiere ejecucion como administrador para acceder a sensores de hardware y ejecutar Windows Defender
- Las contrasenas se almacenan con BCrypt (cost factor 11+)
- Los tokens de recuperacion expiran en 1 hora y son de un solo uso
- Las credenciales SMTP no se hardcodean; se almacenan en config.json en AppData

### Solucion de Problemas
- **No se muestran temperaturas:** Ejecute como administrador y verifique LibreHardwareMonitorLib.dll
- **Error SMTP:** Verifique App Password (no la contrasena regular de Gmail) y SSL habilitado
- **Windows Defender no disponible:** Verifique que MpCmdRun.exe exista en Program Files\Windows Defender
- **Base de datos bloqueada:** Cierre otras instancias de la aplicacion

---
**Version:** 1.0.0  
**Autor:** SentinelPro Technologies  
**Licencia:** Propietaria - Evaluacion 30 dias
