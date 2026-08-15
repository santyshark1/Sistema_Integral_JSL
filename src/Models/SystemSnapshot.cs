using System;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Captura del estado del sistema en un momento dado.
    /// </summary>
    public class SystemSnapshot
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double RamUsedPercent { get; set; }
        public double DiskUsedPercent { get; set; }
        public double MaxTemp { get; set; }
        public double NetworkSpeedMbps { get; set; }
    }
}
