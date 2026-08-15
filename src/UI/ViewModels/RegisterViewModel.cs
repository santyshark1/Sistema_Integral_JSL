using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    public class RegisterViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly EmailService _emailService; // 1. Cambiamos el nombre aquí
        private readonly Action _navigateToLogin;

        private string _fullName = string.Empty;
        private string _emailText = string.Empty; // 2. Cambiamos el nombre aquí para evitar líos
        private string _username = string.Empty;
        private string _accountType = "Usuario estandar";
        private string _password = string.Empty;
        private string _confirmPassword = string.Empty;
        private bool _acceptTerms;
        private bool _showPassword;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private string _successMessage = string.Empty;

        public string FullName { get => _fullName; set => SetProperty(ref _fullName, value); }
        public string Email { get => _emailText; set => SetProperty(ref _emailText, value); } // 3. Usamos _emailText
        public string Username { get => _username; set => SetProperty(ref _username, value); }
        public string AccountType { get => _accountType; set => SetProperty(ref _accountType, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public bool AcceptTerms { get => _acceptTerms; set => SetProperty(ref _acceptTerms, value); }
        public bool ShowPassword { get => _showPassword; set => SetProperty(ref _showPassword, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public string SuccessMessage { get => _successMessage; set => SetProperty(ref _successMessage, value); }

        public ICommand RegisterCommand { get; }
        public ICommand NavigateToLoginCommand { get; }

        public RegisterViewModel(DatabaseService database, EmailService email, Action navigateToLogin)
        {
            _database = database;
            _emailService = email; // 4. Asignamos al nuevo nombre
            _navigateToLogin = navigateToLogin;

            RegisterCommand = new RelayCommand(async _ => await RegisterAsync(), _ => !IsLoading);
            NavigateToLoginCommand = new RelayCommand(_ => _navigateToLogin());
        }

        private async Task RegisterAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;
                SuccessMessage = string.Empty;

                if (!Validate()) return;

                var existing = await _database.GetUserByUsernameAsync(Username.Trim());
                if (existing != null)
                {
                    ErrorMessage = "El nombre de usuario ya esta registrado.";
                    return;
                }

                var existingEmail = await _database.GetUserByEmailAsync(Email.Trim());
                if (existingEmail != null)
                {
                    ErrorMessage = "El correo electronico ya esta registrado.";
                    return;
                }

                var user = new User
                {
                    FullName = FullName.Trim(),
                    Email = Email.Trim(),
                    Username = Username.Trim(),
                    AccountType = AccountType
                };

                await _database.RegisterUserAsync(user, Password);
                try { await _emailService.SendWelcomeAsync(Email.Trim(), FullName.Trim()); } catch { } // 5. Usamos _emailService

                SuccessMessage = "Cuenta creada exitosamente. Redirigiendo al inicio de sesion...";
                await Task.Delay(2000);
                _navigateToLogin();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error al registrar: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) ||
                string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "Todos los campos son obligatorios.";
                return false;
            }

            if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                ErrorMessage = "El correo electronico no tiene un formato valido. Revise su correo nuevamente.";
                return false;
            }

            if (Password.Length < 8)
            {
                ErrorMessage = "La contrasena debe tener al menos 8 caracteres.";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Las contrasenas no coinciden.";
                return false;
            }

            if (!AcceptTerms)
            {
                ErrorMessage = "Debes aceptar los terminos y condiciones.";
                return false;
            }

            return true;
        }
    }
}
