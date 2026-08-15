using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel de gestion de usuarios.
    /// </summary>
    public class UsuariosViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly User? _currentUser;

        private ObservableCollection<User> _users = new ObservableCollection<User>();
        private User? _selectedUser;
        private bool _isEditing;
        private string _editFullName = string.Empty;
        private string _editEmail = string.Empty;
        private string _editAccountType = "Usuario estandar";
        private string _message = string.Empty;
        private bool _isAdmin;
        private System.Collections.Generic.List<string> _accountTypes = new System.Collections.Generic.List<string> { "Usuario estandar", "Administrador" };

        public ObservableCollection<User> Users { get => _users; set => SetProperty(ref _users, value); }
        public User? SelectedUser { get => _selectedUser; set { if (SetProperty(ref _selectedUser, value)) OnUserSelected(); } }
        public bool IsEditing { get => _isEditing; set => SetProperty(ref _isEditing, value); }
        public string EditFullName { get => _editFullName; set => SetProperty(ref _editFullName, value); }
        public string EditEmail { get => _editEmail; set => SetProperty(ref _editEmail, value); }
        public string EditAccountType { get => _editAccountType; set => SetProperty(ref _editAccountType, value); }
        public string Message { get => _message; set => SetProperty(ref _message, value); }
        public bool IsAdmin { get => _isAdmin; set => SetProperty(ref _isAdmin, value); }
        public System.Collections.Generic.List<string> AccountTypes { get => _accountTypes; set => SetProperty(ref _accountTypes, value); }

        public ICommand LoadUsersCommand { get; }
        public ICommand SaveUserCommand { get; }
        public ICommand DeleteUserCommand { get; }
        public ICommand NewUserCommand { get; }
        public ICommand CancelEditCommand { get; }

        public UsuariosViewModel(DatabaseService database, User? currentUser)
        {
            _database = database;
            _currentUser = currentUser;
            IsAdmin = currentUser?.AccountType == "Administrador";

            LoadUsersCommand = new RelayCommand(async _ => await LoadUsersAsync());
            SaveUserCommand = new RelayCommand(async _ => await SaveUserAsync(), _ => IsEditing && IsAdmin);
            DeleteUserCommand = new RelayCommand<User>(async u => await DeleteUserAsync(u), _ => IsAdmin);
            NewUserCommand = new RelayCommand(_ => StartNewUser(), _ => IsAdmin);
            CancelEditCommand = new RelayCommand(_ => CancelEdit());

            _ = LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            var list = await _database.GetAllUsersAsync();
            if (!IsAdmin && _currentUser != null)
                list = list.Where(u => u.Id == _currentUser.Id).ToList();
            Users = new ObservableCollection<User>(list);
        }

        private void OnUserSelected()
        {
            if (SelectedUser != null)
            {
                EditFullName = SelectedUser.FullName;
                EditEmail = SelectedUser.Email;
                EditAccountType = SelectedUser.AccountType;
                IsEditing = IsAdmin;
            }
            else
            {
                CancelEdit();
            }
        }

        private void StartNewUser()
        {
            if (!IsAdmin) return;
            SelectedUser = null;
            EditFullName = string.Empty;
            EditEmail = string.Empty;
            EditAccountType = "Usuario estandar";
            IsEditing = true;
        }

        private void CancelEdit()
        {
            IsEditing = false;
            SelectedUser = null;
            Message = string.Empty;
        }

        private async Task SaveUserAsync()
        {
            if (!IsAdmin) return;
            if (SelectedUser != null)
            {
                SelectedUser.FullName = EditFullName;
                SelectedUser.Email = EditEmail;
                SelectedUser.AccountType = EditAccountType;
                if (await _database.UpdateUserAsync(SelectedUser))
                    Message = "Usuario actualizado correctamente.";
                else
                    Message = "Error al actualizar usuario.";
            }
            else
            {
                if (string.IsNullOrWhiteSpace(EditFullName) || string.IsNullOrWhiteSpace(EditEmail))
                {
                    Message = "Nombre y correo son obligatorios.";
                    return;
                }
                var newUser = new User
                {
                    FullName = EditFullName,
                    Email = EditEmail,
                    Username = EditEmail.Split('@')[0],
                    AccountType = EditAccountType
                };
                await _database.RegisterUserAsync(newUser, "TempPass123!");
                Message = "Usuario creado. Contrasena temporal: TempPass123!";
            }
            await LoadUsersAsync();
        }

        private async Task DeleteUserAsync(User? user)
        {
            if (!IsAdmin) return;
            if (user == null) return;
            var result = MessageBox.Show($"¿Eliminar al usuario {user.Username}?", "Confirmar", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                await _database.DeleteUserAsync(user.Id);
                await LoadUsersAsync();
                Message = "Usuario eliminado.";
            }
        }
    }
}
