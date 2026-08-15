using System;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Modelo de usuario del sistema.
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string AccountType { get; set; } = "Usuario estandar";
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLogin { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
