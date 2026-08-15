using System;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Alerta del sistema.
    /// </summary>
    public class Alert
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "Info";
        public bool IsRead { get; set; } = false;
        public string TypeColor
        {
            get
            {
                return Type switch
                {
                    "Critico" => "#EF4444",
                    "Alta" => "#F59E0B",
                    "Media" => "#3B82F6",
                    "Baja" => "#6B7280",
                    _ => "#3B82F6"
                };
            }
        }
    }
}
