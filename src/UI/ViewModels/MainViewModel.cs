using System;
using System.Windows;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel principal que gestiona la navegacion y el estado de autenticacion.
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly EmailService _email;
        private readonly SystemMonitorService _monitor;
        private readonly HardwareMonitor _hardware;
        private readonly TemperatureMonitor _temperature;
        private readonly AntivirusEngine _antivirus;
        private readonly SystemCleaner _cleaner;
        private readonly PerformanceOptimizer _optimizer;
        private readonly PasswordResetService _resetService;

        private object? _currentView;
        private User? _currentUser;
        private bool _isAuthenticated;
        private string _currentViewName = "Login";
        private int _alertCount;
        private bool _hasCriticalAlerts;

        public object? CurrentView { get => _currentView; set => SetProperty(ref _currentView, value); }
        public User? CurrentUser { get => _currentUser; set => SetProperty(ref _currentUser, value); }
        public bool IsAuthenticated { get => _isAuthenticated; set => SetProperty(ref _isAuthenticated, value); }
        public string CurrentViewName { get => _currentViewName; set => SetProperty(ref _currentViewName, value); }
        public int AlertCount { get => _alertCount; set => SetProperty(ref _alertCount, value); }
        public bool HasCriticalAlerts { get => _hasCriticalAlerts; set => SetProperty(ref _hasCriticalAlerts, value); }

        public ICommand NavigateToLoginCommand { get; }
        public ICommand NavigateToRegisterCommand { get; }
        public ICommand NavigateToPasswordResetCommand { get; }
        public ICommand NavigateToPasswordResetConfirmCommand { get; }
        public ICommand NavigateToDashboardCommand { get; }
        public ICommand NavigateToDiagnosticoCommand { get; }
        public ICommand NavigateToCiberseguridadCommand { get; }
        public ICommand NavigateToMantenimientoCommand { get; }
        public ICommand NavigateToReportesCommand { get; }
        public ICommand NavigateToUsuariosCommand { get; }
        public ICommand NavigateToEmpresasCommand { get; }
        public ICommand NavigateToConfiguracionCommand { get; }
        public ICommand NavigateToAyudaCommand { get; }
        public ICommand LogoutCommand { get; }

        public MainViewModel(DatabaseService database, EmailService email, SystemMonitorService monitor)
        {
            _database = database;
            _email = email;
            _monitor = monitor;
            _hardware = new HardwareMonitor();
            _temperature = new TemperatureMonitor();
            _antivirus = new AntivirusEngine();
            _cleaner = new SystemCleaner();
            _optimizer = new PerformanceOptimizer();
            _resetService = new PasswordResetService(database, email);

            NavigateToLoginCommand = new RelayCommand(_ => ShowLogin());
            NavigateToRegisterCommand = new RelayCommand(_ => ShowRegister());
            NavigateToPasswordResetCommand = new RelayCommand(_ => ShowPasswordReset());
            NavigateToPasswordResetConfirmCommand = new RelayCommand<string>(email => ShowPasswordResetConfirm(email ?? ""));
            NavigateToDashboardCommand = new RelayCommand(_ => ShowDashboard(), _ => IsAuthenticated);
            NavigateToDiagnosticoCommand = new RelayCommand(_ => ShowDiagnostico(), _ => IsAuthenticated);
            NavigateToCiberseguridadCommand = new RelayCommand(_ => ShowCiberseguridad(), _ => IsAuthenticated);
            NavigateToMantenimientoCommand = new RelayCommand(_ => ShowMantenimiento(), _ => IsAuthenticated);
            NavigateToReportesCommand = new RelayCommand(_ => ShowReportes(), _ => IsAuthenticated);
            NavigateToUsuariosCommand = new RelayCommand(_ => ShowUsuarios(), _ => IsAuthenticated);
            NavigateToEmpresasCommand = new RelayCommand(_ => ShowEmpresas(), _ => IsAuthenticated);
            NavigateToConfiguracionCommand = new RelayCommand(_ => ShowConfiguracion(), _ => IsAuthenticated);
            NavigateToAyudaCommand = new RelayCommand(_ => ShowAyuda(), _ => IsAuthenticated);
            LogoutCommand = new RelayCommand(_ => Logout());

            _monitor.OnAlert += (_, __) => UpdateAlertSummary();
            UpdateAlertSummary();
            ShowLogin();
        }

        private void OnLoginSuccess(User user)
        {
            CurrentUser = user;
            IsAuthenticated = true;
            ShowDashboard();
        }

        private void Logout()
        {
            CurrentUser = null;
            IsAuthenticated = false;
            ShowLogin();
            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateAlertSummary()
        {
            AlertCount = _monitor.ActiveAlerts.Count;
            HasCriticalAlerts = _monitor.ActiveAlerts.Exists(a => a.Type == "Critico" || a.Type == "Alta");
        }

        public void ShowLogin()
        {
            var vm = new LoginViewModel(_database, OnLoginSuccess, ShowRegister, ShowPasswordReset);
            CurrentView = new Views.LoginView { DataContext = vm };
            CurrentViewName = "Login";
        }

        public void ShowRegister()
        {
            var vm = new RegisterViewModel(_database, _email, ShowLogin);
            CurrentView = new Views.RegisterView { DataContext = vm };
            CurrentViewName = "Registro";
        }

        public void ShowPasswordReset()
        {
            var vm = new PasswordResetViewModel(_resetService, ShowLogin, ShowPasswordResetConfirm);
            CurrentView = new Views.PasswordResetView { DataContext = vm };
            CurrentViewName = "Restablecer";
        }

        public void ShowPasswordResetConfirm(string email)
        {
            var vm = new PasswordResetConfirmViewModel(_resetService, ShowLogin, email);
            CurrentView = new Views.PasswordResetConfirmView { DataContext = vm };
            CurrentViewName = "Confirmar Restablecer";
        }

        public void ShowDashboard()
        {
            var vm = new DashboardViewModel(_monitor, _hardware, _database);
            CurrentView = new Views.DashboardView { DataContext = vm };
            CurrentViewName = "Inicio";
        }

        public void ShowDiagnostico()
        {
            var vm = new DiagnosticoViewModel(_hardware, _temperature, _database);
            CurrentView = new Views.DiagnosticoView { DataContext = vm };
            CurrentViewName = "Diagnostico";
        }

        public void ShowCiberseguridad()
        {
            var vm = new CiberseguridadViewModel(_antivirus, _database);
            CurrentView = new Views.CiberseguridadView { DataContext = vm };
            CurrentViewName = "Ciberseguridad";
        }

        public void ShowMantenimiento()
        {
            var vm = new MantenimientoViewModel(_cleaner, _optimizer, _database);
            CurrentView = new Views.MantenimientoView { DataContext = vm };
            CurrentViewName = "Mantenimiento";
        }

        public void ShowReportes()
        {
            var vm = new ReportesViewModel(_database, _monitor, _hardware);
            CurrentView = new Views.ReportesView { DataContext = vm };
            CurrentViewName = "Reportes";
        }

        public void ShowUsuarios()
        {
            var vm = new UsuariosViewModel(_database, CurrentUser);
            CurrentView = new Views.UsuariosView { DataContext = vm };
            CurrentViewName = "Usuarios";
        }

        public void ShowEmpresas()
        {
            var vm = new EmpresasViewModel(_database);
            CurrentView = new Views.EmpresasView { DataContext = vm };
            CurrentViewName = "Empresas";
        }

        public void ShowConfiguracion()
        {
            var vm = new ConfiguracionViewModel(_email, _database, CurrentUser);
            CurrentView = new Views.ConfiguracionView { DataContext = vm };
            CurrentViewName = "Configuracion";
        }

        public void ShowAyuda()
        {
            var vm = new AyudaViewModel();
            CurrentView = new Views.AyudaView { DataContext = vm };
            CurrentViewName = "Ayuda";
        }
    }
}
