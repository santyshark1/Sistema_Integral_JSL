namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Informacion del procesador.
    /// </summary>
    public class CpuInfo
    {
        public string Name { get; set; } = string.Empty;
        public int CoreCount { get; set; }
        public int ThreadCount { get; set; }
        public double CurrentClockSpeed { get; set; }
        public double MaxClockSpeed { get; set; }
        public double UsagePercent { get; set; }
        public string Status { get; set; } = "Desconocido";
        public bool Is64Bit { get; set; }
        public string Architecture { get; set; } = string.Empty;
    }
}
