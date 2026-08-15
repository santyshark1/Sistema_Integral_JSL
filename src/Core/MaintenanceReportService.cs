using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    public class MaintenanceReportService
    {
        public async Task<string> GeneratePdfAsync(
            IEnumerable<CleanupItem> cleanedItems,
            long freedBytes,
            string cleanupDetails,
            IEnumerable<OptimizationSetting> optimizedSettings,
            string optimizationDetails,
            IEnumerable<MaintenanceLog> history)
        {
            var cleaned = cleanedItems.ToList();
            var optimized = optimizedSettings.ToList();
            var lines = new List<string>
            {
                "JSL SentinelPro - Reporte de mantenimiento",
                $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}",
                "",
                "Limpieza del sistema"
            };

            if (cleaned.Any())
            {
                lines.Add($"Espacio liberado: {freedBytes / (1024.0 * 1024.0 * 1024.0):F2} GB");
                lines.Add($"Detalle tecnico: {ValueOrNone(cleanupDetails)}");
                lines.Add("");
                lines.Add("Elementos seleccionados por el usuario");
                foreach (var item in cleaned)
                    lines.Add($"- {item.Name} ({item.Category}) - {item.SizeGB:F2} GB estimados - Prioridad {item.Priority}");
                lines.Add("");
                lines.Add("Beneficios esperados");
                lines.Add("- Menos archivos temporales reducen errores de instaladores, actualizaciones y navegadores.");
                lines.Add("- Recuperar espacio mejora la estabilidad cuando el disco estaba cerca de llenarse.");
                lines.Add("- Limpiar cache vieja puede hacer que algunas aplicaciones vuelvan a cargar datos sanos.");
            }
            else
            {
                lines.Add("No se ejecuto limpieza en esta sesion.");
                lines.Add("Te recomendamos usar Limpiar Ahora cuando quieras recuperar espacio y borrar temporales que ya no aportan al sistema.");
            }

            lines.Add("");
            lines.Add("Optimizacion del sistema");
            if (optimized.Any())
            {
                lines.Add($"Detalle tecnico: {ValueOrNone(optimizationDetails)}");
                foreach (var setting in optimized)
                    lines.Add($"- {setting.Name}: {setting.Description}. Mejora estimada: {setting.EstimatedGainPercent:F0}%.");
                lines.Add("");
                lines.Add("Aspectos positivos esperados");
                lines.Add("- Inicio de Windows mas ordenado si se redujeron programas innecesarios.");
                lines.Add("- Mejor respuesta general cuando se aplican ajustes de energia y almacenamiento.");
                lines.Add("- Menos carga visual puede ayudar a equipos con procesador o memoria limitada.");
            }
            else
            {
                lines.Add("No se ejecuto optimizacion en esta sesion.");
                lines.Add("Te recomendamos usar Optimizar Ahora cuando quieras mejorar inicio, almacenamiento o rendimiento general sin borrar tus documentos.");
            }

            lines.Add("");
            lines.Add("Historial de limpiezas");
            var historyList = history.ToList();
            var cleanups = historyList.Where(h => h.ActionType == "Limpieza").ToList();
            if (cleanups.Any())
            {
                foreach (var log in cleanups.Take(10))
                    lines.Add($"{log.ActionDate:yyyy-MM-dd HH:mm} - {log.SpaceFreedGB:F2} GB - {ValueOrNone(log.Details)}");
            }
            else
            {
                lines.Add("No hay limpiezas registradas.");
            }

            lines.Add("");
            lines.Add("Historial de optimizaciones");
            var optimizations = historyList.Where(h => h.ActionType == "Optimizacion").ToList();
            if (optimizations.Any())
            {
                foreach (var log in optimizations.Take(10))
                    lines.Add($"{log.ActionDate:yyyy-MM-dd HH:mm} - {ValueOrNone(log.Details)}");
            }
            else
            {
                lines.Add("No hay optimizaciones registradas.");
            }

            lines.Add("");
            lines.Add("Tips externos para optimizar el PC");
            lines.Add("- Mantén al menos 15% de espacio libre en el disco principal.");
            lines.Add("- Reinicia el equipo despues de actualizaciones grandes o limpiezas profundas.");
            lines.Add("- Desinstala programas que no uses y evita tener varios antivirus activos al tiempo.");
            lines.Add("- Si el equipo usa HDD, cambiar a SSD suele ser la mejora mas notoria.");
            lines.Add("- Limpia fisicamente ventiladores y rejillas para evitar calor y lentitud.");

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"JSL_Mantenimiento_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await PdfDocumentWriter.WriteAsync(path, lines, "Reporte de mantenimiento");
            OpenFile(path);
            return path;
        }

        private static string ValueOrNone(string value) => string.IsNullOrWhiteSpace(value) ? "Ninguno" : value;

        private static void OpenFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { }
        }
    }
}
