using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    public class SecurityReportService
    {
        public async Task<string> GenerateSecurityPdfAsync(
            bool protectionActive,
            bool firewallActive,
            string signatureVersion,
            string riskLevel,
            IEnumerable<ThreatScanResult> activeThreats,
            IEnumerable<ThreatScanResult> history,
            IEnumerable<string> scanLogs)
        {
            var realActiveThreats = activeThreats.Where(t => !t.IsPlaceholder).ToList();
            var realHistory = history.Where(t => !t.IsPlaceholder).ToList();
            var lines = new List<string>
            {
                "JSL SentinelPro - Reporte de ciberseguridad",
                $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}",
                "",
                "Resultado general",
                realActiveThreats.Any()
                    ? $"Se detectaron {realActiveThreats.Count} amenaza(s) activa(s). Atiendelas desde el Centro de Incidentes antes de seguir usando archivos descargados o unidades externas."
                    : "Felicitaciones: no se detectaron virus activos en el ultimo escaneo. Sigue cuidando tu PC, manteniendo Windows Defender activo y evitando descargas sospechosas.",
                "",
                "Estado de proteccion",
                $"Proteccion en tiempo real: {(protectionActive ? "Activa" : "Inactiva")}",
                $"Firewall: {(firewallActive ? "Activo" : "Inactivo")}",
                $"Base de datos de virus: {signatureVersion}",
                $"Nivel de riesgo: {riskLevel}",
                "",
                "Amenazas activas"
            };

            if (realActiveThreats.Any())
            {
                foreach (var threat in realActiveThreats)
                    lines.Add($"{threat.ThreatName} | Tipo: {threat.ThreatType} | Severidad: {threat.Severity} | Ubicacion: {threat.FilePath}");

                lines.Add("");
                lines.Add("Solucion recomendada");
                lines.Add("- Opcion recomendada: enviar a cuarentena. La app mueve el archivo a una zona aislada para que deje de ejecutarse y puedas revisarlo sin perder evidencia.");
                lines.Add("- Opcion definitiva: eliminar amenaza. Usala cuando reconozcas que el archivo no es necesario o viene de una descarga sospechosa.");
                lines.Add("- Opcion de riesgo: ignorar. No es buena opcion si no conoces el archivo, porque el virus podria seguir activo o volver a ejecutarse.");
                lines.Add("");
                lines.Add("Como hacerlo en JSL SentinelPro");
                lines.Add("1. Ve al Centro de Incidentes.");
                lines.Add("2. Selecciona la amenaza detectada.");
                lines.Add("3. Pulsa Enviar a cuarentena o Eliminar amenaza.");
                lines.Add("4. Ejecuta otro escaneo para confirmar que la tabla quede sin amenazas activas.");
            }
            else
            {
                lines.Add("Ninguna amenaza activa.");
                lines.Add("");
                lines.Add("Mensaje para el usuario");
                lines.Add("Tu equipo se mantiene en buen estado de seguridad. Sigue asi: revisa descargas, mantén el antivirus activo y ejecuta un escaneo periodico.");
            }

            lines.Add("");
            lines.Add("Historial de amenazas neutralizadas");
            if (realHistory.Any())
            {
                foreach (var threat in realHistory.Take(20))
                    lines.Add($"{threat.DetectionDate:yyyy-MM-dd HH:mm} | {threat.ThreatName} | Accion: {threat.ActionTaken} | Severidad: {threat.Severity}");
            }
            else
            {
                lines.Add("Ninguna amenaza neutralizada registrada.");
            }

            lines.Add("");
            lines.Add("Tips de cuidado y vida util");
            lines.Add("- Mantén Windows y el navegador actualizados para reducir vulnerabilidades.");
            lines.Add("- Evita instaladores de paginas desconocidas, cracks o activadores.");
            lines.Add("- Limpia temporales y revisa programas de inicio para que el PC responda mejor.");
            lines.Add("- Haz copias de seguridad antes de borrar amenazas importantes.");
            lines.Add("");
            lines.Add("Registro del ultimo analisis");
            foreach (var log in scanLogs.TakeLast(25))
                lines.Add(log);

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"JSL_Ciberseguridad_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await PdfDocumentWriter.WriteAsync(path, lines, "Reporte de ciberseguridad");
            OpenFile(path);
            return path;
        }

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
