using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel para solicitar codigo de recuperacion de contrasena.
    /// </summary>
    public class PasswordResetViewModel : BaseViewModel
    {
        private readonly PasswordResetService _resetService;
        private readonly Action _navigateToLogin;
        private readonly Action<string> _navigateToConfirm;

        private string _email = string.Empty;
        private string _message = string.Empty;
        private bool _isLoading;
        private bool _isSuccess;

        public string Email { get => _email; set => SetProperty(ref _email, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }
        public bool IsSuccess { get => _isSuccess; set => SetProperty(ref _isSuccess, value); }

        public ICommand SendCodeCommand { get; }
        public ICommand NavigateToLoginCommand { get; }
        public ICommand NavigateToConfirmCommand { get; }

        public PasswordResetViewModel(PasswordResetService resetService, Action navigateToLogin, Action<string> navigateToConfirm)
        {
            _resetService = resetService;
            _navigateToLogin = navigateToLogin;
            _navigateToConfirm = navigateToConfirm;

            SendCodeCommand = new RelayCommand(async _ => await SendCodeAsync(), _ => !IsLoading);
            NavigateToLoginCommand = new RelayCommand(_ => _navigateToLogin());
            NavigateToConfirmCommand = new RelayCommand(_ => _navigateToConfirm(Email));
        }

        private async Task SendCodeAsync()
        {
            try
            {
                IsLoading = true;
                Message = string.Empty;
                IsSuccess = false;

                if (string.IsNullOrWhiteSpace(Email))
                {
                    Message = "Ingrese su correo electronico.";
                    return;
                }

                if (!Regex.IsMatch(Email.Trim(), @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                {
                    Message = "El correo electronico no tiene un formato valido.";
                    return;
                }

                var result = await _resetService.RequestResetAsync(Email.Trim());
                if (result.Success)
                {
                    IsSuccess = true;
                    Message = result.Message;
                    _navigateToConfirm(Email.Trim());
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
    }
}
