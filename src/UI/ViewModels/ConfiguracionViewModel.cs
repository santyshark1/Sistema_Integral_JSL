using System;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    public class ConfiguracionViewModel : BaseViewModel
    {
        private readonly EmailService _email;
        private readonly DatabaseService _database;
        private readonly User? _currentUser;

        private bool _automaticScansEnabled = true;
        private string _scanFrequency = "Semanal";
        private bool _automaticMaintenanceEnabled;
        private string _maintenanceTime = "20:00";
        private bool _securityAlertsEnabled = true;
        private bool _systemStatusReportsEnabled = true;
        private string _currentPassword = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _settingsMessage = string.Empty;
        private string _passwordMessage = string.Empty;
        private string _smtpServer = string.Empty;
        private string _smtpPort = "587";
        private string _smtpUsername = string.Empty;
        private string _smtpPassword = string.Empty;
        private string _smtpFromEmail = string.Empty;
        private string _smtpFromName = "JSL SentinelPro";
        private bool _smtpEnableSsl = true;
        private string _smtpMessage = string.Empty;
        private string _appVersion = "1.0.0";
        private System.Collections.Generic.List<string> _scanFrequencies = new System.Collections.Generic.List<string> { "Semanal", "Mensual" };

        public bool AutomaticScansEnabled { get => _automaticScansEnabled; set => SetProperty(ref _automaticScansEnabled, value); }
        public string ScanFrequency { get => _scanFrequency; set => SetProperty(ref _scanFrequency, value); }
        public bool AutomaticMaintenanceEnabled
        {
            get => _automaticMaintenanceEnabled;
            set
            {
                SetProperty(ref _automaticMaintenanceEnabled, value);
                CommandManager.InvalidateRequerySuggested();
            }
        }
        public string MaintenanceTime { get => _maintenanceTime; set => SetProperty(ref _maintenanceTime, value); }
        public bool SecurityAlertsEnabled { get => _securityAlertsEnabled; set => SetProperty(ref _securityAlertsEnabled, value); }
        public bool SystemStatusReportsEnabled { get => _systemStatusReportsEnabled; set => SetProperty(ref _systemStatusReportsEnabled, value); }
        public string CurrentPassword { get => _currentPassword; set => SetProperty(ref _currentPassword, value); }
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public string SettingsMessage { get => _settingsMessage; set => SetProperty(ref _settingsMessage, value); }
        public string PasswordMessage { get => _passwordMessage; set => SetProperty(ref _passwordMessage, value); }
        public string SmtpServer { get => _smtpServer; set => SetProperty(ref _smtpServer, value); }
        public string SmtpPort { get => _smtpPort; set => SetProperty(ref _smtpPort, value); }
        public string SmtpUsername { get => _smtpUsername; set => SetProperty(ref _smtpUsername, value); }
        public string SmtpPassword { get => _smtpPassword; set => SetProperty(ref _smtpPassword, value); }
        public string SmtpFromEmail { get => _smtpFromEmail; set => SetProperty(ref _smtpFromEmail, value); }
        public string SmtpFromName { get => _smtpFromName; set => SetProperty(ref _smtpFromName, value); }
        public bool SmtpEnableSsl { get => _smtpEnableSsl; set => SetProperty(ref _smtpEnableSsl, value); }
        public string SmtpMessage { get => _smtpMessage; set => SetProperty(ref _smtpMessage, value); }
        public string AppVersion { get => _appVersion; set => SetProperty(ref _appVersion, value); }
        public System.Collections.Generic.List<string> ScanFrequencies { get => _scanFrequencies; set => SetProperty(ref _scanFrequencies, value); }

        public ICommand SaveConfigCommand { get; }
        public ICommand ChangePasswordCommand { get; }
        public ICommand CheckUpdatesCommand { get; }
        public ICommand SaveSmtpCommand { get; }
        public ICommand SendTestEmailCommand { get; }

        public ConfiguracionViewModel(EmailService email, DatabaseService database, User? currentUser)
        {
            _email = email;
            _database = database;
            _currentUser = currentUser;

            SaveConfigCommand = new RelayCommand(_ => SaveConfig());
            ChangePasswordCommand = new RelayCommand(async _ => await ChangePasswordAsync());
            CheckUpdatesCommand = new RelayCommand(_ => CheckUpdates());
            SaveSmtpCommand = new RelayCommand(_ => SaveSmtpConfig());
            SendTestEmailCommand = new RelayCommand(async _ => await SendTestEmailAsync());

            AppVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
            LoadSmtpConfig();
        }

        private void LoadSmtpConfig()
        {
            var config = _email.GetConfig();
            SmtpServer = config.Server;
            SmtpPort = config.Port.ToString();
            SmtpUsername = config.Username;
            SmtpPassword = config.Password;
            SmtpFromEmail = config.FromEmail;
            SmtpFromName = config.FromName;
            SmtpEnableSsl = config.EnableSsl;
        }

        private void SaveConfig()
        {
            var maintenance = AutomaticMaintenanceEnabled
                ? $"Mantenimiento automatico activo a las {MaintenanceTime}."
                : "Mantenimiento automatico desactivado.";
            var scans = AutomaticScansEnabled
                ? $"Escaneos automaticos activos con frecuencia {ScanFrequency}."
                : "Escaneos automaticos desactivados.";
            SettingsMessage = $"{scans} {maintenance}";
        }

        private void SaveSmtpConfig()
        {
            if (!int.TryParse(SmtpPort, out var port) || port <= 0)
            {
                SmtpMessage = "El puerto SMTP no es valido.";
                return;
            }

            if (string.IsNullOrWhiteSpace(SmtpServer) ||
                string.IsNullOrWhiteSpace(SmtpUsername) ||
                string.IsNullOrWhiteSpace(SmtpPassword) ||
                string.IsNullOrWhiteSpace(SmtpFromEmail))
            {
                SmtpMessage = "Complete servidor, usuario, contrasena y correo remitente.";
                return;
            }

            _email.SaveConfig(new SmtpConfig
            {
                Server = SmtpServer.Trim(),
                Port = port,
                Username = SmtpUsername.Trim(),
                Password = SmtpPassword,
                FromEmail = SmtpFromEmail.Trim(),
                FromName = string.IsNullOrWhiteSpace(SmtpFromName) ? "JSL SentinelPro" : SmtpFromName.Trim(),
                EnableSsl = SmtpEnableSsl
            });

            SmtpMessage = "Configuracion SMTP guardada.";
        }

        private async System.Threading.Tasks.Task SendTestEmailAsync()
        {
            if (_currentUser == null || string.IsNullOrWhiteSpace(_currentUser.Email))
            {
                SmtpMessage = "No hay usuario autenticado para enviar correo de prueba.";
                return;
            }

            try
            {
                SaveSmtpConfig();
                if (!_email.IsConfigured())
                    return;

                await _email.SendTestAsync(_currentUser.Email);
                SmtpMessage = $"Correo de prueba enviado a {_currentUser.Email}.";
            }
            catch (Exception ex)
            {
                SmtpMessage = $"No se pudo enviar el correo de prueba: {ex.Message}";
            }
        }

        private async System.Threading.Tasks.Task ChangePasswordAsync()
        {
            if (_currentUser == null)
            {
                PasswordMessage = "No hay usuario autenticado.";
                return;
            }

            if (!BCrypt.Net.BCrypt.Verify(CurrentPassword, _currentUser.PasswordHash))
            {
                PasswordMessage = "La contrasena actual no es correcta.";
                return;
            }

            if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
            {
                PasswordMessage = "La nueva contrasena debe tener minimo 8 caracteres.";
                return;
            }

            if (NewPassword != ConfirmPassword)
            {
                PasswordMessage = "La confirmacion no coincide.";
                return;
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(NewPassword);
            if (await _database.UpdatePasswordAsync(_currentUser.Id, hash))
            {
                _currentUser.PasswordHash = hash;
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmPassword = string.Empty;
                PasswordMessage = "Contrasena actualizada correctamente.";
            }
            else
            {
                PasswordMessage = "No se pudo actualizar la contrasena.";
            }
        }

        private void CheckUpdates()
        {
            System.Windows.MessageBox.Show("Tu version es la mas reciente.", "Actualizaciones", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
