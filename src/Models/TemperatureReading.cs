namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Lectura de temperatura de un sensor.
    /// </summary>
    public class TemperatureReading
    {
        public string HardwareName { get; set; } = string.Empty;
        public string SensorName { get; set; } = string.Empty;
        public double ValueCelsius { get; set; }
        public string Status
        {
            get
            {
                if (ValueCelsius >= 85) return "Critico";
                if (ValueCelsius >= 70) return "Alto";
                if (ValueCelsius >= 55) return "Medio";
                return "Normal";
            }
        }
        public string StatusColor
        {
            get
            {
                return Status switch
                {
                    "Critico" => "#EF4444",
                    "Alto" => "#F59E0B",
                    "Medio" => "#3B82F6",
                    _ => "#22C55E"
                };
            }
        }
    }
}
