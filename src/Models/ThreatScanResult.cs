using System;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Resultado de deteccion de amenaza.
    /// </summary>
    public class ThreatScanResult
    {
        public int Id { get; set; }
        public DateTime DetectionDate { get; set; }
        public string ThreatName { get; set; } = string.Empty;
        public string ThreatType { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ActionTaken { get; set; } = "Pendiente";
        public string Severity { get; set; } = "Media";
        public string Status { get; set; } = "Pendiente";
        public bool IsNeutralized => ActionTaken == "Eliminado" || ActionTaken == "Cuarentena";
        public bool IsPlaceholder { get; set; }

        public static ThreatScanResult Empty(string message = "Ninguno")
        {
            return new ThreatScanResult
            {
                Id = -1,
                DetectionDate = DateTime.Now,
                ThreatName = message,
                ThreatType = "Ninguno",
                FilePath = "Ninguno",
                ActionTaken = "Ninguno",
                Severity = "Ninguno",
                Status = "Ninguno",
                IsPlaceholder = true
            };
        }
    }
}
