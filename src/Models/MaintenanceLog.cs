using System;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Registro de mantenimiento.
    /// </summary>
    public class MaintenanceLog
    {
        public int Id { get; set; }
        public DateTime ActionDate { get; set; }
        public string ActionType { get; set; } = string.Empty;
        public long SpaceFreedBytes { get; set; }
        public string Details { get; set; } = string.Empty;
        public double SpaceFreedGB => SpaceFreedBytes / (1024.0 * 1024.0 * 1024.0);
    }
}
