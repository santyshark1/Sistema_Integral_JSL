namespace JSL_SentinelPro.src.Models
{
    public class GpuInfo
    {
        public string Name { get; set; } = "Desconocida";
        public string DriverVersion { get; set; } = "Desconocido";
        public string VideoProcessor { get; set; } = "Desconocido";
        public double MemoryGB { get; set; }
        public string Status { get; set; } = "No detectada";
    }
}
