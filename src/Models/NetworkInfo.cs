namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Informacion de red.
    /// </summary>
    public class NetworkInfo
    {
        public string AdapterName { get; set; } = string.Empty;
        public string MacAddress { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public double SpeedMbps { get; set; }
        public bool IsConnected { get; set; } = false;
    }
}
