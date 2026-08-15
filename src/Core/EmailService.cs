using System.Linq;
using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Configuracion de SMTP.
    /// </summary>
    public class SmtpConfig
    {
        public string Server { get; set; } = "smtp.gmail.com";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FromName { get; set; } = "JSL SentinelPro";
        public string FromEmail { get; set; } = string.Empty;
        public bool EnableSsl { get; set; } = true;
    }

    /// <summary>
    /// Servicio de envio de correos electronicos.
    /// </summary>
    public class EmailService
    {
        private SmtpConfig _config;
        private readonly string _configPath;

        public EmailService(string configPath)
        {
            _configPath = configPath;
            _config = LoadConfig();
        }

        private SmtpConfig LoadConfig()
        {
            if (File.Exists(_configPath))
            {
                try
                {
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<SmtpConfig>(json);
                    if (config != null && !string.IsNullOrEmpty(config.Username))
                        return config;
                }
                catch { }
            }
            return new SmtpConfig
            {
                Server = "smtp.gmail.com",
                Port = 587,
                EnableSsl = true,
                Username = "tu_email@gmail.com",
                Password = "tu_app_password",
                FromEmail = "tu_email@gmail.com",
                FromName = "JSL SentinelPro"
            };
        }

        public void SaveConfig(SmtpConfig config)
        {
            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configPath, json);
            _config = config;
        }

        public SmtpConfig GetConfig()
        {
            return new SmtpConfig
            {
                Server = _config.Server,
                Port = _config.Port,
                Username = _config.Username,
                Password = _config.Password,
                FromName = _config.FromName,
                FromEmail = _config.FromEmail,
                EnableSsl = _config.EnableSsl
            };
        }

        private SmtpClient CreateClient()
        {
            if (!IsConfigured())
                throw new InvalidOperationException("Configura primero el correo SMTP en la pestaña Configuracion.");

            return new SmtpClient(_config.Server, _config.Port)
            {
                EnableSsl = _config.EnableSsl,
                Credentials = new NetworkCredential(_config.Username, _config.Password),
                Timeout = 30000
            };
        }

        public bool IsConfigured()
        {
            return !string.IsNullOrWhiteSpace(_config.Server)
                && _config.Port > 0
                && !string.IsNullOrWhiteSpace(_config.Username)
                && !string.IsNullOrWhiteSpace(_config.Password)
                && !string.IsNullOrWhiteSpace(_config.FromEmail)
                && !_config.Username.Contains("tu_email", StringComparison.OrdinalIgnoreCase)
                && !_config.Password.Contains("tu_app_password", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Envía un correo de bienvenida al nuevo usuario.
        /// </summary>
        public async Task SendWelcomeAsync(string email, string userName)
        {
            var subject = "¡Bienvenido a JSL Proteccion Inteligente!";
            var body = $@"Hola {userName},

¡Bienvenido a JSL SentinelPro! Tu cuenta ha sido creada exitosamente.

Con JSL SentinelPro puedes:
• Monitorear el hardware de tu PC en tiempo real
• Ejecutar escaneos de amenazas con Windows Defender
• Limpiar y optimizar tu sistema
• Generar reportes de seguridad y rendimiento
• Acceder a nuestra red de centros tecnicos certificados

Tu cuenta tiene una evaluacion gratuita de 30 dias con acceso a todas las funcionalidades.

Si tienes alguna pregunta, contacta a nuestro equipo de soporte.

Saludos,
El equipo de JSL SentinelPro
Sistema Integral de Diagnostico y Ciberseguridad
";
            await SendEmailAsync(email, subject, body);
        }

        public async Task SendTestAsync(string email)
        {
            var subject = "Prueba de correo - JSL SentinelPro";
            var body = @"Hola,

La configuracion SMTP de JSL SentinelPro funciona correctamente.

Ya puedes usar recuperacion de contrasena por codigo.

Saludos,
JSL SentinelPro";
            await SendEmailAsync(email, subject, body);
        }

        /// <summary>
        /// Envía un correo de recuperacion de contrasena.
        /// </summary>
        public async Task SendPasswordResetAsync(string email, string token, string userName, string tempPassword)
        {
            var subject = "Recupera tu contrasena - JSL SentinelPro";
            var body = $@"Hola {userName},

Has solicitado restablecer tu contrasena de JSL SentinelPro.

Codigo de recuperacion: {token}

Para completar el proceso:
1. Abre la aplicacion JSL SentinelPro
2. Ve a 'Restablecer Contrasena' e ingresa el codigo de arriba
3. Crea una nueva contrasena segura

Importante:
• El codigo expira en 1 hora
• Tu contrasena actual no cambia hasta que confirmes el codigo

Si no solicitaste este cambio, ignora este mensaje.

Saludos,
El equipo de JSL SentinelPro
Sistema monitoreado 24/7
";
            await SendEmailAsync(email, subject, body);
        }

        private async Task SendEmailAsync(string to, string subject, string body)
        {
            using var client = CreateClient();
            var message = new MailMessage
            {
                From = new MailAddress(_config.FromEmail, _config.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };
            message.To.Add(to);

            try
            {
                await client.SendMailAsync(message);
                LogDebug($"[EMAIL ENVIADO] Para: {to} | Asunto: {subject}");
            }
            catch (SmtpException ex)
            {
                LogDebug($"[EMAIL ERROR] SMTP: {ex.Message} | Status: {ex.StatusCode}");
                throw;
            }
            catch (Exception ex)
            {
                LogDebug($"[EMAIL ERROR] General: {ex.Message}");
                throw;
            }
        }

        private void LogDebug(string message)
        {
            System.Diagnostics.Debug.WriteLine($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
