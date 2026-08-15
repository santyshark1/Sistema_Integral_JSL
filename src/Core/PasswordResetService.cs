using System.Linq;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Servicio de recuperacion de contrasena.
    /// </summary>
    public class PasswordResetService
    {
        private readonly DatabaseService _database;
        private readonly EmailService _email;

        public PasswordResetService(DatabaseService database, EmailService email)
        {
            _database = database;
            _email = email;
        }

        /// <summary>
        /// Genera un token de 6 caracteres alfanumericos legibles.
        /// </summary>
        public string GenerateToken()
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var sb = new StringBuilder(6);
            using var rng = RandomNumberGenerator.Create();
            var buffer = new byte[6];
            rng.GetBytes(buffer);
            for (int i = 0; i < 6; i++)
            {
                sb.Append(chars[buffer[i] % chars.Length]);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Genera una contrasena temporal de 10 caracteres.
        /// </summary>
        public string GenerateTempPassword()
        {
            const string upper = "ABCDEFGHJKMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%^&*";
            var all = upper + lower + digits + symbols;
            var sb = new StringBuilder(10);
            using var rng = RandomNumberGenerator.Create();
            sb.Append(upper[GetRandomByte(rng) % upper.Length]);
            sb.Append(lower[GetRandomByte(rng) % lower.Length]);
            sb.Append(digits[GetRandomByte(rng) % digits.Length]);
            sb.Append(symbols[GetRandomByte(rng) % symbols.Length]);
            var buffer = new byte[6];
            rng.GetBytes(buffer);
            for (int i = 0; i < 6; i++)
                sb.Append(all[buffer[i] % all.Length]);
            return ShuffleString(sb.ToString());
        }

        private byte GetRandomByte(RandomNumberGenerator rng)
        {
            var b = new byte[1];
            rng.GetBytes(b);
            return b[0];
        }

        private string ShuffleString(string input)
        {
            var chars = input.ToCharArray();
            using var rng = RandomNumberGenerator.Create();
            for (int i = chars.Length - 1; i > 0; i--)
            {
                var b = new byte[1];
                rng.GetBytes(b);
                int j = b[0] % (i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }
            return new string(chars);
        }

        /// <summary>
        /// Solicita el restablecimiento de contrasena.
        /// </summary>
        public async Task<(bool Success, string Message)> RequestResetAsync(string email)
        {
            email = email.Trim();
            var user = await _database.GetUserByEmailAsync(email);
            if (user == null)
                return (false, "No existe una cuenta con ese correo electronico.");

            var token = GenerateToken();
            var tempPassword = GenerateTempPassword();

            await _database.CreatePasswordResetAsync(email, token, tempPassword);

            try
            {
                await _email.SendPasswordResetAsync(email, token, user.FullName, tempPassword);
                return (true, "Se ha enviado un codigo de recuperacion a tu correo electronico.");
            }
            catch
            {
                return (false, "No fue posible enviar el codigo de recuperacion al correo. Verifica la conexion y la configuracion de correo del sistema.");
            }
        }

        /// <summary>
        /// Confirma el restablecimiento con token y nueva contrasena.
        /// </summary>
        public async Task<(bool Success, string Message)> ConfirmResetAsync(string email, string token, string newPassword)
        {
            email = email.Trim();
            token = token.Trim().ToUpperInvariant();

            var reset = await _database.ValidatePasswordResetTokenAsync(email, token);
            if (reset == null)
                return (false, "El codigo de recuperacion no corresponde a ese correo, es invalido o ha expirado.");

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            var user = await _database.GetUserByEmailAsync(reset.Email);
            if (user == null)
                return (false, "No se encontro el usuario asociado al token.");

            await _database.UpdatePasswordAsync(user.Id, hash);
            await _database.MarkPasswordResetUsedAsync(reset.Id);

            return (true, "Tu contrasena ha sido restablecida exitosamente.");
        }
    }
}
