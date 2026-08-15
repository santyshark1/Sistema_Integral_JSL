using System;
using System.Collections.Generic;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Escaneo de hardware completo.
    /// </summary>
    public class HardwareScan
    {
        public int Id { get; set; }
        public DateTime ScanDate { get; set; }
        public double CpuUsage { get; set; }
        public ulong RamUsedBytes { get; set; }
        public ulong RamTotalBytes { get; set; }
        public ulong DiskUsedBytes { get; set; }
        public ulong DiskTotalBytes { get; set; }
        public double MaxTemperature { get; set; }
        public string Status { get; set; } = "Normal";
        public List<string> ComponentsAnalyzed { get; set; } = new List<string>();
    }
}
