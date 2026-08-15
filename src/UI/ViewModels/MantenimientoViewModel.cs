using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel de mantenimiento y optimizacion.
    /// </summary>
    public class MantenimientoViewModel : BaseViewModel
    {
        private readonly SystemCleaner _cleaner;
        private readonly PerformanceOptimizer _optimizer;
        private readonly DatabaseService _database;
        private readonly MaintenanceReportService _reports;

        private double _tempFilesGB;
        private double _recoverableSpaceGB;
        private string _storageStatus = "Normal";
        private double _storagePercent;
        private ObservableCollection<CleanupItem> _cleanupItems = new ObservableCollection<CleanupItem>();
        private int _startupProgramCount;
        private double _estimatedPerformanceGain;
        private string _performanceHealth = "Normal";
        private ObservableCollection<StartupProgram> _startupPrograms = new ObservableCollection<StartupProgram>();
        private ObservableCollection<OptimizationSetting> _optimizationSettings = new ObservableCollection<OptimizationSetting>();
        private ObservableCollection<MaintenanceLog> _maintenanceHistory = new ObservableCollection<MaintenanceLog>();
        private ObservableCollection<MaintenanceLog> _cleanupHistory = new ObservableCollection<MaintenanceLog>();
        private ObservableCollection<MaintenanceLog> _optimizationHistory = new ObservableCollection<MaintenanceLog>();
        private bool _isCleaning;
        private bool _isOptimizing;
        private bool _hasMaintenanceAction;
        private string _lastCleanupDate = "Nunca";
        private string _securityTip = "Mantenga su sistema actualizado para prevenir vulnerabilidades.";
        private string _maintenanceMessage = string.Empty;
        private List<CleanupItem> _lastCleanedItems = new List<CleanupItem>();
        private List<OptimizationSetting> _lastOptimizedSettings = new List<OptimizationSetting>();
        private long _lastFreedBytes;
        private string _lastCleanupDetails = string.Empty;
        private string _lastOptimizationDetails = string.Empty;

        public double TempFilesGB { get => _tempFilesGB; set => SetProperty(ref _tempFilesGB, value); }
        public double RecoverableSpaceGB { get => _recoverableSpaceGB; set => SetProperty(ref _recoverableSpaceGB, value); }
        public string StorageStatus { get => _storageStatus; set => SetProperty(ref _storageStatus, value); }
        public double StoragePercent { get => _storagePercent; set => SetProperty(ref _storagePercent, value); }
        public ObservableCollection<CleanupItem> CleanupItems { get => _cleanupItems; set => SetProperty(ref _cleanupItems, value); }
        public int StartupProgramCount { get => _startupProgramCount; set => SetProperty(ref _startupProgramCount, value); }
        public double EstimatedPerformanceGain { get => _estimatedPerformanceGain; set => SetProperty(ref _estimatedPerformanceGain, value); }
        public string PerformanceHealth { get => _performanceHealth; set => SetProperty(ref _performanceHealth, value); }
        public ObservableCollection<StartupProgram> StartupPrograms { get => _startupPrograms; set => SetProperty(ref _startupPrograms, value); }
        public ObservableCollection<OptimizationSetting> OptimizationSettings { get => _optimizationSettings; set => SetProperty(ref _optimizationSettings, value); }
        public ObservableCollection<MaintenanceLog> MaintenanceHistory { get => _maintenanceHistory; set => SetProperty(ref _maintenanceHistory, value); }
        public ObservableCollection<MaintenanceLog> CleanupHistory { get => _cleanupHistory; set => SetProperty(ref _cleanupHistory, value); }
        public ObservableCollection<MaintenanceLog> OptimizationHistory { get => _optimizationHistory; set => SetProperty(ref _optimizationHistory, value); }
        public bool IsCleaning { get => _isCleaning; set => SetProperty(ref _isCleaning, value); }
        public bool IsOptimizing { get => _isOptimizing; set => SetProperty(ref _isOptimizing, value); }
        public bool HasMaintenanceAction { get => _hasMaintenanceAction; set => SetProperty(ref _hasMaintenanceAction, value); }
        public string LastCleanupDate { get => _lastCleanupDate; set => SetProperty(ref _lastCleanupDate, value); }
        public string SecurityTip { get => _securityTip; set => SetProperty(ref _securityTip, value); }
        public string MaintenanceMessage { get => _maintenanceMessage; set => SetProperty(ref _maintenanceMessage, value); }

        public ICommand CleanNowCommand { get; }
        public ICommand OptimizeNowCommand { get; }
        public ICommand GeneratePdfCommand { get; }
        public ICommand RefreshCommand { get; }

        public MantenimientoViewModel(SystemCleaner cleaner, PerformanceOptimizer optimizer, DatabaseService database)
        {
            _cleaner = cleaner;
            _optimizer = optimizer;
            _database = database;
            _reports = new MaintenanceReportService();

            CleanNowCommand = new RelayCommand(async _ => await CleanNowAsync(), _ => !IsCleaning);
            OptimizeNowCommand = new RelayCommand(async _ => await OptimizeNowAsync(), _ => !IsOptimizing);
            GeneratePdfCommand = new RelayCommand(async _ => await GeneratePdfAsync(), _ => HasMaintenanceAction);
            RefreshCommand = new RelayCommand(_ => RefreshData());

            RefreshData();
            LoadHistory();
        }

        private void RefreshData()
        {
            var items = _cleaner.GetCleanupItems();
            CleanupItems = new ObservableCollection<CleanupItem>(items);
            foreach (var item in CleanupItems)
            {
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(CleanupItem.IsSelected))
                        RecoverableSpaceGB = CleanupItems.Where(i => i.IsSelected).Sum(i => i.SizeGB);
                };
            }
            TempFilesGB = items.Sum(i => i.SizeGB);
            RecoverableSpaceGB = items.Where(i => i.IsSelected).Sum(i => i.SizeGB);
            StoragePercent = items.Count > 0 ? Math.Min(100, TempFilesGB * 5) : 0;
            StorageStatus = StoragePercent > 80 ? "Critico" : StoragePercent > 50 ? "Atencion" : "Normal";

            var programs = _cleaner.GetStartupPrograms();
            StartupPrograms = new ObservableCollection<StartupProgram>(programs);
            StartupProgramCount = programs.Count;

            var settings = _optimizer.GetRecommendedSettings();
            OptimizationSettings = new ObservableCollection<OptimizationSetting>(settings);
            foreach (var setting in OptimizationSettings)
            {
                setting.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(OptimizationSetting.IsRecommended))
                    {
                        EstimatedPerformanceGain = _optimizer.EstimatePerformanceGain(OptimizationSettings.ToList());
                        PerformanceHealth = EstimatedPerformanceGain > 20 ? "Atencion" : "Normal";
                    }
                };
            }
            EstimatedPerformanceGain = _optimizer.EstimatePerformanceGain(settings.ToList());
            PerformanceHealth = EstimatedPerformanceGain > 20 ? "Atencion" : "Normal";
        }

        private async Task CleanNowAsync()
        {
            IsCleaning = true;
            var selected = CleanupItems.Where(i => i.IsSelected).ToList();
            if (!selected.Any())
            {
                MaintenanceMessage = "Selecciona al menos una opcion de limpieza antes de ejecutar.";
                IsCleaning = false;
                return;
            }

            var result = await _cleaner.CleanTempFilesAsync(selected);
            _lastCleanedItems = selected;
            _lastFreedBytes = result.FreedBytes;
            _lastCleanupDetails = result.Details;

            var log = new MaintenanceLog
            {
                ActionDate = DateTime.Now,
                ActionType = "Limpieza",
                SpaceFreedBytes = result.FreedBytes,
                Details = result.Details
            };
            await _database.SaveMaintenanceLogAsync(log);

            LastCleanupDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
            MaintenanceMessage = result.FreedBytes > 0
                ? $"Limpieza completada. Espacio liberado: {result.FreedBytes / (1024.0 * 1024.0 * 1024.0):F2} GB."
                : "Limpieza completada. No se pudo liberar espacio adicional porque los archivos estaban en uso o ya habian sido eliminados.";
            HasMaintenanceAction = true;
            RefreshData();
            LoadHistory();
            IsCleaning = false;
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task OptimizeNowAsync()
        {
            IsOptimizing = true;
            var selectedSettings = OptimizationSettings.Where(s => s.IsRecommended).ToList();
            if (!selectedSettings.Any())
            {
                MaintenanceMessage = "Selecciona al menos una opcion de optimizacion antes de ejecutar.";
                IsOptimizing = false;
                return;
            }

            var selectedPowerPlans = selectedSettings.Where(s => s.Id == "power_plan_high" || s.Id == "power_plan_saver").ToList();
            if (selectedPowerPlans.Count > 1)
            {
                MaintenanceMessage = "Selecciona solo un plan de energia: maximo rendimiento o ahorro de bateria.";
                IsOptimizing = false;
                return;
            }

            if (selectedSettings.Any(s => s.Id == "power_plan_high"))
            {
                var confirmHigh = System.Windows.MessageBox.Show(
                    "El modo maximo rendimiento puede hacer que la bateria se descargue mas rapido. Deseas aplicarlo?",
                    "Plan de energia",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);
                if (confirmHigh != System.Windows.MessageBoxResult.Yes)
                    selectedSettings.RemoveAll(s => s.Id == "power_plan_high");
            }

            if (selectedSettings.Any(s => s.Id == "power_plan_saver"))
            {
                var confirmSaver = System.Windows.MessageBox.Show(
                    "El modo ahorro guarda bateria, pero puede bajar un poco el rendimiento. Deseas aplicarlo?",
                    "Plan de energia",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Information);
                if (confirmSaver != System.Windows.MessageBoxResult.Yes)
                    selectedSettings.RemoveAll(s => s.Id == "power_plan_saver");
            }

            if (selectedSettings.Any(s => s.Id == "close_background_apps"))
            {
                var confirmClose = System.Windows.MessageBox.Show(
                    "Quieres cerrar aplicaciones y pestanas en segundo plano? Se intentaran cerrar navegadores y apps comunes abiertas. Si eliges No, se mantienen abiertas.",
                    "Cerrar segundo plano",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Question);
                if (confirmClose != System.Windows.MessageBoxResult.Yes)
                    selectedSettings.RemoveAll(s => s.Id == "close_background_apps");
            }

            if (!selectedSettings.Any())
            {
                MaintenanceMessage = "No se aplicaron optimizaciones porque cancelaste las opciones seleccionadas.";
                IsOptimizing = false;
                return;
            }

            int applied = 0;
            var appliedSettings = new List<OptimizationSetting>();
            var optimizationDetails = new List<string>();
            foreach (var setting in selectedSettings)
            {
                if (setting.Id == "startup_optimization")
                    continue;

                if (await _optimizer.ApplyOptimizationAsync(setting.Id))
                {
                    applied++;
                    setting.IsApplied = true;
                    appliedSettings.Add(setting);
                    optimizationDetails.Add(setting.Name);
                }
                else
                {
                    optimizationDetails.Add($"{setting.Name}: no se pudo aplicar o requiere permisos de administrador");
                }
            }

            var programsToDisable = StartupPrograms.Where(p => !p.IsEnabled).Select(p => p.Name).ToList();
            if (programsToDisable.Any())
            {
                var startupResult = await _cleaner.OptimizeStartupAsync(programsToDisable);
                if (startupResult.Count > 0)
                {
                    applied++;
                    var startupSetting = selectedSettings.FirstOrDefault(s => s.Id == "startup_optimization");
                    if (startupSetting != null)
                    {
                        startupSetting.IsApplied = true;
                        appliedSettings.Add(startupSetting);
                    }
                }
                if (!string.IsNullOrWhiteSpace(startupResult.Details))
                    optimizationDetails.Add(startupResult.Details);
            }

            _lastOptimizedSettings = appliedSettings;
            _lastOptimizationDetails = string.Join("; ", optimizationDetails);
            if (string.IsNullOrWhiteSpace(_lastOptimizationDetails))
                _lastOptimizationDetails = "No se aplicaron cambios visibles. Revisa permisos o selecciona otra opcion.";

            var log = new MaintenanceLog
            {
                ActionDate = DateTime.Now,
                ActionType = "Optimizacion",
                Details = $"Ajustes aplicados: {applied}. Programas deshabilitados: {programsToDisable.Count}. {_lastOptimizationDetails}"
            };
            await _database.SaveMaintenanceLogAsync(log);

            HasMaintenanceAction = true;
            MaintenanceMessage = applied > 0
                ? $"Optimizacion completada. Ajustes aplicados: {applied}. {_lastOptimizationDetails}"
                : $"No se aplicaron optimizaciones. {_lastOptimizationDetails}";
            RefreshData();
            LoadHistory();
            IsOptimizing = false;
            CommandManager.InvalidateRequerySuggested();
        }

        private async void LoadHistory()
        {
            var logs = await _database.GetMaintenanceLogsAsync(10);
            MaintenanceHistory = new ObservableCollection<MaintenanceLog>(logs);
            CleanupHistory = new ObservableCollection<MaintenanceLog>(logs.Where(l => l.ActionType == "Limpieza"));
            OptimizationHistory = new ObservableCollection<MaintenanceLog>(logs.Where(l => l.ActionType == "Optimizacion"));
            var last = logs.FirstOrDefault(l => l.ActionType == "Limpieza");
            if (last != null)
                LastCleanupDate = last.ActionDate.ToString("yyyy-MM-dd HH:mm");
        }

        private async Task GeneratePdfAsync()
        {
            var path = await _reports.GeneratePdfAsync(
                _lastCleanedItems,
                _lastFreedBytes,
                _lastCleanupDetails,
                _lastOptimizedSettings,
                _lastOptimizationDetails,
                MaintenanceHistory);

            System.Windows.MessageBox.Show($"Reporte PDF generado:\n{path}", "Reporte de mantenimiento", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
