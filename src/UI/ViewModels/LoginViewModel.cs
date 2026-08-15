using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel para la pantalla de inicio de sesion.
    /// </summary>
    public class LoginViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly Action<User> _onLoginSuccess;
        private readonly Action _navigateToRegister;
        private readonly Action _navigateToPasswordReset;

        private string _username = string.Empty;
        private string _password = string.Empty;
        private string _errorMessage = string.Empty;
        private bool _isLoading;
        private bool _rememberMe;

        public string Username { get => _username; set => SetProperty(ref _username, value); }
        public string Password { get => _password; set => SetProperty(ref _password, value); }
        public string ErrorMessage { get => _errorMessage; set => SetProperty(ref _errorMessage, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool RememberMe { get => _rememberMe; set => SetProperty(ref _rememberMe, value); }

        public ICommand LoginCommand { get; }
        public ICommand NavigateToRegisterCommand { get; }
        public ICommand NavigateToPasswordResetCommand { get; }

        public LoginViewModel(DatabaseService database, Action<User> onLoginSuccess, Action navigateToRegister, Action navigateToPasswordReset)
        {
            _database = database;
            _onLoginSuccess = onLoginSuccess;
            _navigateToRegister = navigateToRegister;
            _navigateToPasswordReset = navigateToPasswordReset;

            LoginCommand = new RelayCommand(async _ => await LoginAsync(), _ => !IsLoading);
            NavigateToRegisterCommand = new RelayCommand(_ => _navigateToRegister());
            NavigateToPasswordResetCommand = new RelayCommand(_ => _navigateToPasswordReset());
        }

        private async Task LoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
                {
                    ErrorMessage = "Ingrese usuario y contrasena.";
                    return;
                }

                var user = await _database.AuthenticateAsync(Username.Trim(), Password);
                if (user != null)
                {
                    _onLoginSuccess(user);
                }
                else
                {
                    ErrorMessage = "Usuario o contrasena incorrectos.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error de conexion: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }
    }
}
