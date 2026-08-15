namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Programa de inicio del sistema.
    /// </summary>
    public class StartupProgram
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string RegistryKey { get; set; } = string.Empty;
        public string Impact { get; set; } = "Bajo";
        public bool IsEnabled { get; set; } = true;
    }
}
