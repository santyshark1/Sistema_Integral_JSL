using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel para confirmar restablecimiento de contrasena.
    /// </summary>
    public class PasswordResetConfirmViewModel : BaseViewModel
    {
        private readonly PasswordResetService _resetService;
        private readonly Action _navigateToLogin;

        private string _email = string.Empty;
        private string _token = string.Empty;
        private string _newPassword = string.Empty;
        private string _confirmPassword = string.Empty;
        private string _message = string.Empty;
        private bool _isLoading;
        private bool _isSuccess;

        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Token { get => _token; set => SetProperty(ref _token, value); }
        public string NewPassword { get => _newPassword; set => SetProperty(ref _newPassword, value); }
        public string ConfirmPassword { get => _confirmPassword; set => SetProperty(ref _confirmPassword, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }

        public ICommand ResetPasswordCommand { get; }
        public ICommand NavigateToLoginCommand { get; }
        public ICommand ResendCodeCommand { get; }

        public PasswordResetConfirmViewModel(PasswordResetService resetService, Action navigateToLogin, string email)
        {
            _resetService = resetService;
            _navigateToLogin = navigateToLogin;
            Email = email;

            ResetPasswordCommand = new RelayCommand(async _ => await ResetPasswordAsync(), _ => !IsLoading);
            NavigateToLoginCommand = new RelayCommand(_ => _navigateToLogin());
            ResendCodeCommand = new RelayCommand(async _ => await ResendCodeAsync(), _ => !IsLoading);
        }

        private async Task ResetPasswordAsync()
        {
            try
            {
                IsLoading = true;
                Message = string.Empty;
                IsSuccess = false;

                if (!Validate()) return;

                var result = await _resetService.ConfirmResetAsync(Email.Trim(), Token.Trim().ToUpper(), NewPassword);
                if (result.Success)
                {
                    IsSuccess = true;
                    Message = "Contrasena restablecida exitosamente.";
                    await Task.Delay(2000);
                    _navigateToLogin();
                }
                else
                {
                    Message = result.Message;
                }
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ResendCodeAsync()
        {
            if (string.IsNullOrWhiteSpace(Email))
            {
                Message = "No hay correo asociado para reenviar.";
                return;
            }

            var result = await _resetService.RequestResetAsync(Email);
            Message = result.Success ? "Codigo reenviado. Revisa tu correo." : result.Message;
        }

        private bool Validate()
        {
            if (string.IsNullOrWhiteSpace(Token) || Token.Length != 6)
            {
                Message = "Ingrese el codigo de 6 caracteres.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Email) || !Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
            {
                Message = "Ingrese el correo electronico donde recibio el codigo.";
                return false;
            }

            if (NewPassword.Length < 8)
            {
                Message = "La nueva contrasena debe tener al menos 8 caracteres.";
                return false;
            }

            if (!Regex.IsMatch(NewPassword, @"[A-Z]"))
            {
                Message = "La contrasena debe incluir al menos una mayuscula.";
                return false;
            }

            if (!Regex.IsMatch(NewPassword, @"[0-9]"))
            {
                Message = "La contrasena debe incluir al menos un numero.";
                return false;
            }

            if (!Regex.IsMatch(NewPassword, @"[!@#$%^&*]"))
            {
                Message = "La contrasena debe incluir al menos un simbolo (!@#$%^&*).";
                return false;
            }

            if (NewPassword != ConfirmPassword)
            {
                Message = "Las contrasenas no coinciden.";
                return false;
            }

            return true;
        }
    }
}
