using System;

namespace JSL_SentinelPro.src.Models
{
    /// <summary>
    /// Token de recuperacion de contrasena.
    /// </summary>
    public class PasswordResetToken
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public string TempPassword { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
