using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    public class HardwareReportService
    {
        private readonly HardwareMonitor _hardware;
        private readonly TemperatureMonitor _temperature;
        private readonly DatabaseService? _database;

        public HardwareReportService(HardwareMonitor hardware, TemperatureMonitor temperature, DatabaseService? database = null)
        {
            _hardware = hardware;
            _temperature = temperature;
            _database = database;
        }

        public async Task<string> GenerateHardwareScanPdfAsync(HardwareScan scan, IEnumerable<HardwareScan> history)
        {
            var cpu = _hardware.GetCpuInfo();
            var memory = _hardware.GetMemoryInfo();
            var disks = _hardware.GetDiskInfo();
            var temperatures = _temperature.GetAllTemperatures();
            var historyList = history.ToList();

            var lines = new List<string>
            {
                "JSL SentinelPro - Reporte de escaneo de hardware",
                $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}",
                "",
                "Resumen del escaneo",
                $"Estado general: {scan.Status}",
                $"Uso CPU: {scan.CpuUsage:F1}%",
                $"RAM: {BytesToGB(scan.RamUsedBytes):F1} GB usados de {BytesToGB(scan.RamTotalBytes):F1} GB",
                $"Almacenamiento principal: {BytesToGB(scan.DiskUsedBytes):F1} GB usados de {BytesToGB(scan.DiskTotalBytes):F1} GB",
                $"Temperatura maxima: {scan.MaxTemperature:F1} C",
                "",
                "Lectura rapida del estado actual",
                $"Procesador: {ValueOrUnknown(cpu.Name)} con uso actual de {cpu.UsagePercent:F1}%",
                $"Memoria disponible: {memory.FreeGB:F1} GB libres de {memory.TotalGB:F1} GB",
                $"Discos revisados: {disks.Count}",
                "",
                "Historial reciente"
            };

            if (historyList.Any())
            {
                foreach (var item in historyList.Take(10))
                    lines.Add($"{item.ScanDate:yyyy-MM-dd HH:mm} - CPU {item.CpuUsage:F1}% - Temp {item.MaxTemperature:F1} C - {item.Status}");
            }
            else
            {
                lines.Add("No hay escaneos previos registrados.");
            }

            lines.Add("");
            lines.Add("Cambios y tendencia");
            if (historyList.Count >= 2)
            {
                var newest = historyList[0];
                var previous = historyList[1];
                lines.Add($"CPU frente al escaneo anterior: {newest.CpuUsage - previous.CpuUsage:+0.0;-0.0;0.0} puntos.");
                lines.Add($"Temperatura frente al escaneo anterior: {newest.MaxTemperature - previous.MaxTemperature:+0.0;-0.0;0.0} C.");
            }
            else
            {
                lines.Add("Este reporte servira como punto base para comparar proximos escaneos.");
            }

            lines.Add("");
            lines.Add("Temperaturas relevantes");
            foreach (var temp in temperatures.OrderByDescending(t => t.ValueCelsius).Take(8))
                lines.Add($"{temp.HardwareName} / {temp.SensorName}: {temp.ValueCelsius:F1} C - {temp.Status}");
            if (!temperatures.Any())
                lines.Add("No se recibieron sensores de temperatura.");

            lines.Add("");
            lines.Add("Acciones sugeridas despues del escaneo");
            lines.AddRange(BuildScanRecommendations(scan, disks, temperatures));
            if (HasHighTemperature(scan, temperatures))
            {
                lines.Add("");
                lines.Add("Soporte recomendado por temperatura alta");
                lines.Add(BuildTemperatureSupportText(scan, temperatures));
                foreach (var partner in await GetRecommendedPartnersAsync("Refrigeracion y temperaturas", "Mantenimiento preventivo", "Reparacion de hardware"))
                    lines.Add($"- {partner.Name} ({partner.City}) - {partner.Specialty} - {partner.Phone}");
            }

            var path = GetDesktopReportPath("JSL_Escaneo_Hardware");
            await PdfDocumentWriter.WriteAsync(path, lines, "Reporte de escaneo");
            OpenFile(path);
            return path;
        }

        public async Task<string> GeneratePcRecognitionPdfAsync()
        {
            var cpu = _hardware.GetCpuInfo();
            var memory = _hardware.GetMemoryInfo();
            var disks = _hardware.GetDiskInfo();
            var gpus = _hardware.GetGpuInfo();
            var temperatures = _temperature.GetAllTemperatures();
            var network = _hardware.GetNetworkInfo();
            var system = GetComputerIdentity();
            var ageYears = GetComputerAgeYears();
            var usefulness = EstimateUsefulLife(ageYears, memory.TotalGB, disks.Any(d => d.DriveType.Contains("SSD", StringComparison.OrdinalIgnoreCase)));

            var lines = new List<string>
            {
                "JSL SentinelPro - Reconoce tu PC",
                $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}",
                "",
                "Identificacion del equipo",
                $"Fabricante: {system.Manufacturer}",
                $"Modelo: {system.Model}",
                $"Sistema operativo: {Environment.OSVersion}",
                $"Fecha base detectada: {system.ReferenceDateText}",
                $"Edad estimada: {FormatAge(ageYears)}",
                "",
                "Procesador",
                $"Nombre: {ValueOrUnknown(cpu.Name)}",
                $"Nucleos / hilos: {cpu.CoreCount} / {cpu.ThreadCount}",
                $"Arquitectura: {ValueOrUnknown(cpu.Architecture)}",
                $"Velocidad maxima: {cpu.MaxClockSpeed:F2} GHz",
                "",
                "Memoria RAM",
                $"Total: {memory.TotalGB:F1} GB",
                $"En uso: {memory.UsedGB:F1} GB ({memory.UsagePercent:F1}%)",
                $"Libre: {memory.FreeGB:F1} GB",
                "",
                "Almacenamiento"
            };

            foreach (var disk in disks)
                lines.Add($"{disk.DriveLetter} {disk.Label} - {disk.DriveType} - {disk.FreeGB:F1} GB libres de {disk.TotalGB:F1} GB - {disk.FileSystem}");

            lines.Add("");
            lines.Add("Tarjeta grafica");
            foreach (var gpu in gpus)
                lines.Add($"{gpu.Name} - VRAM {gpu.MemoryGB:F1} GB - Driver {gpu.DriverVersion} - Estado {gpu.Status}");
            lines.Add("");
            lines.Add("Red");
            lines.Add($"Adaptador: {ValueOrUnknown(network.AdapterName)}");
            lines.Add($"MAC: {ValueOrUnknown(network.MacAddress)}");
            lines.Add($"Velocidad actual: {network.SpeedMbps:F2} Mbps");
            lines.Add("");
            lines.Add("Diagnostico de vida util");
            lines.Add(usefulness);
            lines.Add("");
            lines.Add("Como alargar la vida del PC");
            lines.AddRange(BuildCareRecommendations(cpu, memory, disks, temperatures, ageYears));
            if (NeedsRamOrSsd(memory, disks))
            {
                lines.Add("");
                lines.Add("Empresas recomendadas para mejora de RAM o SSD");
                foreach (var partner in await GetRecommendedPartnersAsync("Actualizacion SSD/RAM", "Reparacion de hardware", "Mantenimiento preventivo"))
                    lines.Add($"- {partner.Name} ({partner.City}) - {partner.Specialty} - {partner.Phone}");
            }

            var path = GetDesktopReportPath("JSL_Reconoce_Tu_PC");
            await PdfDocumentWriter.WriteAsync(path, lines, "Reconoce tu PC");
            OpenFile(path);
            return path;
        }

        private static IEnumerable<string> BuildScanRecommendations(HardwareScan scan, List<DiskInfo> disks, List<TemperatureReading> temperatures)
        {
            var recs = new List<string>();
            var highTemperature = HasHighTemperature(scan, temperatures);
            var criticalTemperature = scan.MaxTemperature >= 85 || temperatures.Any(t => t.ValueCelsius >= 85);

            if (scan.CpuUsage > 85)
                recs.Add("- El uso del procesador fue alto durante el escaneo; revisa procesos abiertos antes de trabajar o jugar.");
            if (criticalTemperature)
            {
                recs.Add("- Alerta critica: la temperatura esta muy alta. Guarda tu trabajo y deja descansar el PC antes de exigirlo de nuevo.");
                recs.Add("- Apaga el PC por 10 a 15 minutos si lo sientes muy caliente.");
            }
            else if (highTemperature)
            {
                recs.Add("- Advertencia: la temperatura esta por encima de lo recomendable. Mejora la ventilacion y evita cargas pesadas prolongadas.");
            }

            if (highTemperature)
            {
                recs.Add("- Usa el equipo sobre una mesa plana, no sobre cama, sofa, cobijas o piernas, porque se tapan las salidas de aire.");
                recs.Add("- Limpia con cuidado las rejillas externas con aire suave o una brocha seca; no uses agua ni liquidos.");
                recs.Add("- Aleja el PC de paredes, polvo y sol directo. Deja espacio libre alrededor para que respire.");
                recs.Add("- Cierra juegos, navegadores con muchas pestanas o programas pesados cuando no los necesites.");
                recs.Add("- Si es portatil, usa una base elevada o base refrigerante para mejorar el flujo de aire.");
            }
            if (disks.Any(d => d.UsagePercent > 85))
                recs.Add("- Hay poco espacio disponible; libera archivos temporales y copias antiguas.");
            if (!recs.Any())
                recs.Add("- El escaneo no muestra alertas criticas. Manten el mantenimiento preventivo cada 3 meses.");
            return recs;
        }

        private static bool HasHighTemperature(HardwareScan scan, List<TemperatureReading> temperatures)
        {
            return scan.MaxTemperature >= 75 || temperatures.Any(t => t.ValueCelsius >= 75);
        }

        private static string BuildTemperatureSupportText(HardwareScan scan, List<TemperatureReading> temperatures)
        {
            var max = Math.Max(scan.MaxTemperature, temperatures.Any() ? temperatures.Max(t => t.ValueCelsius) : 0);
            return max >= 85
                ? "La temperatura esta en rango critico. Si los pasos caseros no ayudan, comunicate con empresas asociadas para limpieza interna, revision de ventiladores o diagnostico de refrigeracion."
                : "La temperatura esta alta, aunque no necesariamente critica. Si se repite en varios escaneos, conviene pedir limpieza interna o revision de ventilacion.";
        }

        private static IEnumerable<string> BuildCareRecommendations(CpuInfo cpu, MemoryInfo memory, List<DiskInfo> disks, List<TemperatureReading> temperatures, double? ageYears)
        {
            var recs = new List<string>();
            if (memory.TotalGB > 0 && memory.TotalGB < 8)
                recs.Add("- Ampliar la RAM a 8 GB o mas mejora navegacion, ofimatica y clases virtuales.");
            if (memory.UsagePercent > 80)
                recs.Add("- Cerrar programas al inicio y mantener menos pestanas abiertas para bajar el consumo de RAM.");
            if (disks.Any(d => d.DriveType.Equals("HDD", StringComparison.OrdinalIgnoreCase)))
                recs.Add("- Cambiar el disco HDD por SSD es la mejora mas notable en equipos antiguos.");
            if (disks.Any(d => d.UsagePercent > 85))
                recs.Add("- Liberar espacio hasta dejar al menos 15% disponible en cada unidad.");
            if (temperatures.Any(t => t.ValueCelsius > 80))
                recs.Add("- Hacer limpieza interna, revisar ventiladores y cambiar pasta termica.");
            if (cpu.CoreCount > 0 && cpu.CoreCount <= 2)
                recs.Add("- Evitar tareas pesadas simultaneas; este procesador es limitado para multitarea moderna.");
            if (ageYears.HasValue && ageYears.Value >= 6)
                recs.Add("- Mantener Windows y controladores livianos; valorar una instalacion limpia si el equipo esta lento.");
            recs.Add("- Realizar mantenimiento preventivo cada 3 meses y copia de seguridad semanal.");
            return recs.Distinct();
        }

        private static string EstimateUsefulLife(double? ageYears, double ramGb, bool hasSsd)
        {
            if (!ageYears.HasValue)
                return "No se pudo estimar la edad exacta, pero se puede prolongar la vida util revisando RAM, disco y temperatura.";

            var age = ageYears.Value;
            var remaining = age < 3 ? "entre 4 y 6 anos de uso normal" :
                age < 6 ? "entre 2 y 4 anos, con mantenimiento y buen almacenamiento" :
                age < 9 ? "entre 1 y 2 anos para tareas basicas" :
                "uso limitado; conviene planear reemplazo si se necesita rendimiento moderno";

            var upgrade = ramGb < 8 || !hasSsd
                ? " La vida util puede mejorar mucho con SSD y RAM suficiente."
                : " El equipo ya tiene una base razonable para seguir funcionando.";

            return $"El equipo tiene aproximadamente {age:F1} anos. Vida util estimada: {remaining}.{upgrade}";
        }

        private static double BytesToGB(ulong bytes) => bytes / (1024.0 * 1024.0 * 1024.0);

        private static string FormatAge(double? ageYears) => ageYears.HasValue ? $"{ageYears.Value:F1} anos" : "No disponible";

        private static string ValueOrUnknown(string value) => string.IsNullOrWhiteSpace(value) ? "Desconocido" : value;

        private static bool NeedsRamOrSsd(MemoryInfo memory, List<DiskInfo> disks)
        {
            return (memory.TotalGB > 0 && memory.TotalGB < 8) ||
                   disks.Any(d => d.DriveType.Equals("HDD", StringComparison.OrdinalIgnoreCase));
        }

        private async Task<List<CompanyPartner>> GetRecommendedPartnersAsync(params string[] specialties)
        {
            if (_database == null)
                return new List<CompanyPartner>();

            var partners = await _database.GetCompanyPartnersAsync();
            return partners
                .Where(p => p.IsAvailable && specialties.Any(s => p.Specialty.Contains(s, StringComparison.OrdinalIgnoreCase) || s.Contains(p.Specialty, StringComparison.OrdinalIgnoreCase)))
                .OrderByDescending(p => p.Rating)
                .Take(8)
                .ToList();
        }

        private static string GetDesktopReportPath(string prefix)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"{prefix}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
        }

        private static void OpenFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { }
        }

        private static (string Manufacturer, string Model, string ReferenceDateText) GetComputerIdentity()
        {
            var manufacturer = "Desconocido";
            var model = "Desconocido";
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    manufacturer = obj["Manufacturer"]?.ToString() ?? manufacturer;
                    model = obj["Model"]?.ToString() ?? model;
                    break;
                }
            }
            catch { }

            var date = GetReferenceDate();
            return (manufacturer, model, date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "No disponible");
        }

        private static double? GetComputerAgeYears()
        {
            var date = GetReferenceDate();
            if (!date.HasValue)
                return null;
            return Math.Max(0, (DateTime.Now - date.Value).TotalDays / 365.25);
        }

        private static DateTime? GetReferenceDate()
        {
            var dates = new List<DateTime>();
            try
            {
                using var bios = new ManagementObjectSearcher("SELECT ReleaseDate FROM Win32_BIOS");
                foreach (ManagementObject obj in bios.Get())
                {
                    var raw = obj["ReleaseDate"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(raw))
                        dates.Add(ManagementDateTimeConverter.ToDateTime(raw));
                }
            }
            catch { }

            try
            {
                using var os = new ManagementObjectSearcher("SELECT InstallDate FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in os.Get())
                {
                    var raw = obj["InstallDate"]?.ToString();
                    if (!string.IsNullOrWhiteSpace(raw))
                        dates.Add(ManagementDateTimeConverter.ToDateTime(raw));
                }
            }
            catch { }

            return dates.Where(d => d.Year > 2000 && d <= DateTime.Now).DefaultIfEmpty().Min();
        }
    }
}
