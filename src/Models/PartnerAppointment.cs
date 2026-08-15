using System;

namespace JSL_SentinelPro.src.Models
{
    public class PartnerAppointment
    {
        public int Id { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.Now;
        public string CompanyName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string Contact { get; set; } = string.Empty;
        public string Status { get; set; } = "Solicitada";
    }
}
