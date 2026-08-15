namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Empresa asociada / centro tecnico.
    /// </summary>
    public class CompanyPartner
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Rating { get; set; }
        public bool IsAvailable { get; set; } = true;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public bool HasWarranty { get; set; } = false;
    }
}
