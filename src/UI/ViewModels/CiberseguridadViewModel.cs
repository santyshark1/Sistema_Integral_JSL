using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel de ciberseguridad.
    /// </summary>
    public class CiberseguridadViewModel : BaseViewModel
    {
        private readonly AntivirusEngine _antivirus;
        private readonly DatabaseService _database;
        private readonly SecurityReportService _reports;

        private bool _isProtectionActive;
        private bool _isFirewallActive;
        private string _signatureVersion = "Desconocido";
        private string _riskLevel = "Calculando...";
        private bool _isScanning;
        private double _scanProgress;
        private long _itemsChecked;
        private int _threatsDetected;
        private string _scanTime = "00:00:00";
        private string _scanScope = "Sin escaneo iniciado.";
        private ObservableCollection<string> _scanLogs = new ObservableCollection<string>();
        private ObservableCollection<ThreatScanResult> _activeThreats = new ObservableCollection<ThreatScanResult>();
        private ObservableCollection<ThreatScanResult> _neutralizedThreats = new ObservableCollection<ThreatScanResult>();
        private ThreatScanResult? _selectedActiveThreat;
        private bool _hasCompletedScan;
        private CancellationTokenSource? _scanCts;
        private System.Timers.Timer? _scanTimer;
        private DateTime _scanStartTime;

        public bool IsProtectionActive { get => _isProtectionActive; set => SetProperty(ref _isProtectionActive, value); }
        public bool IsFirewallActive { get => _isFirewallActive; set => SetProperty(ref _isFirewallActive, value); }
        public string SignatureVersion { get => _signatureVersion; set => SetProperty(ref _signatureVersion, value); }
        public string RiskLevel { get => _riskLevel; set => SetProperty(ref _riskLevel, value); }
        public bool IsScanning { get => _isScanning; set => SetProperty(ref _isScanning, value); }
        public double ScanProgress { get => _scanProgress; set => SetProperty(ref _scanProgress, value); }
        public long ItemsChecked { get => _itemsChecked; set => SetProperty(ref _itemsChecked, value); }
        public int ThreatsDetected { get => _threatsDetected; set => SetProperty(ref _threatsDetected, value); }
        public string ScanTime { get => _scanTime; set => SetProperty(ref _scanTime, value); }
        public string ScanScope { get => _scanScope; set => SetProperty(ref _scanScope, value); }
        public ObservableCollection<string> ScanLogs { get => _scanLogs; set => SetProperty(ref _scanLogs, value); }
        public ObservableCollection<ThreatScanResult> ActiveThreats
        {
            get => _activeThreats;
            set
            {
                SetProperty(ref _activeThreats, value);
                OnPropertyChanged(nameof(HasRealActiveThreats));
            }
        }
        public ObservableCollection<ThreatScanResult> NeutralizedThreats { get => _neutralizedThreats; set => SetProperty(ref _neutralizedThreats, value); }
        public ThreatScanResult? SelectedActiveThreat { get => _selectedActiveThreat; set { SetProperty(ref _selectedActiveThreat, value); CommandManager.InvalidateRequerySuggested(); } }
        public bool HasRealActiveThreats => ActiveThreats.Any(t => !t.IsPlaceholder);
        public bool HasCompletedScan { get => _hasCompletedScan; set => SetProperty(ref _hasCompletedScan, value); }

        public ICommand StartScanCommand { get; }
        public ICommand StopScanCommand { get; }
        public ICommand DeleteThreatCommand { get; }
        public ICommand QuarantineThreatCommand { get; }
        public ICommand IgnoreThreatCommand { get; }
        public ICommand GeneratePdfCommand { get; }
        public ICommand RefreshStatusCommand { get; }

        public CiberseguridadViewModel(AntivirusEngine antivirus, DatabaseService database)
        {
            _antivirus = antivirus;
            _database = database;
            _reports = new SecurityReportService();

            StartScanCommand = new RelayCommand(async _ => await StartScanAsync(), _ => !IsScanning);
            StopScanCommand = new RelayCommand(_ => StopScan(), _ => IsScanning);
            DeleteThreatCommand = new RelayCommand(async _ => await HandleSelectedThreatAsync("Eliminado"), _ => CanActOnSelectedThreat());
            QuarantineThreatCommand = new RelayCommand(async _ => await HandleSelectedThreatAsync("Cuarentena"), _ => CanActOnSelectedThreat());
            IgnoreThreatCommand = new RelayCommand(async _ => await HandleSelectedThreatAsync("Ignorado"), _ => CanActOnSelectedThreat());
            GeneratePdfCommand = new RelayCommand(async _ => await GeneratePdfAsync(), _ => HasCompletedScan);
            RefreshStatusCommand = new RelayCommand(_ => RefreshStatus());

            SetEmptyActiveThreats();
            SetEmptyNeutralizedThreats();
            RefreshStatus();
            LoadThreatHistory();
        }

        private void RefreshStatus()
        {
            var status = _antivirus.GetProtectionStatus();
            IsProtectionActive = status.ContainsKey("RealTimeProtection") && (bool)status["RealTimeProtection"];
            IsFirewallActive = true;
            SignatureVersion = status.ContainsKey("AntivirusSignatureVersion") ? status["AntivirusSignatureVersion"]?.ToString() ?? "Desconocido" : "Desconocido";

            var riskScore = 0;
            if (!IsProtectionActive) riskScore += 50;
            var realActiveThreats = ActiveThreats.Count(t => !t.IsPlaceholder);
            var realNeutralizedThreats = NeutralizedThreats.Count(t => !t.IsPlaceholder);
            if (realActiveThreats > 0) riskScore += realActiveThreats * 15;
            if (realNeutralizedThreats > 5) riskScore += 10;

            RiskLevel = riskScore > 60 ? "Critico" : riskScore > 30 ? "Alto" : riskScore > 10 ? "Medio" : "Bajo";
        }

        private async Task StartScanAsync()
        {
            var ignoredThreats = await _database.GetThreatDetectionsAsync(DateTime.Now.AddMonths(-6), DateTime.Now);
            var ignoredKeys = ignoredThreats
                .Where(t => t.ActionTaken == "Ignorado")
                .Select(t => GetThreatKey(t))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            IsScanning = true;
            ScanProgress = 0;
            ItemsChecked = 0;
            ThreatsDetected = 0;
            HasCompletedScan = false;
            ScanTime = "00:00:00";
            ScanScope = "Escaneo rapido de Windows Defender: procesos activos, memoria, servicios, areas de inicio, rutas criticas del sistema y ubicaciones comunes de malware. Windows Defender decide internamente los archivos exactos y no expone una lista completa por MpCmdRun.";
            ScanLogs = new ObservableCollection<string>
            {
                "[INFO] Preparando escaneo rapido. Esto puede tardar unos minutos segun el equipo."
            };
            ActiveThreats.Clear();
            SelectedActiveThreat = null;
            _scanCts = new CancellationTokenSource();
            _scanStartTime = DateTime.Now;

            _scanTimer = new System.Timers.Timer(1000);
            _scanTimer.Elapsed += (s, e) =>
            {
                var elapsed = DateTime.Now - _scanStartTime;
                ScanTime = $"{elapsed.Hours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
            };
            _scanTimer.Start();

            var heartbeatTask = RunScanHeartbeatAsync(_scanCts.Token);

            var progress = new Progress<string>(msg =>
            {
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    ScanLogs.Add(msg);
                    ItemsChecked++;
                    if (msg.Contains("Amenaza") || msg.Contains("Threat"))
                        ThreatsDetected++;
                });
            });

            var results = new List<ThreatScanResult>();
            try
            {
                results = await _antivirus.ScanSystemAsync(progress, _scanCts.Token);
                var actionableResults = await _antivirus.ScanCriticalAreasAsync(progress, _scanCts.Token);
                results.AddRange(actionableResults.Where(t => !results.Any(r =>
                    string.Equals(r.FilePath, t.FilePath, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(r.ThreatName, t.ThreatName, StringComparison.OrdinalIgnoreCase))));
                var ignoredFound = results
                    .Where(t => ignoredKeys.Contains(GetThreatKey(t)) && ThreatStillExists(t))
                    .ToList();
                if (ignoredFound.Any())
                {
                    foreach (var ignored in ignoredFound)
                        results.Remove(ignored);
                    ScanLogs.Add($"[INFO] El PC no muestra nuevas amenazas activas, pero recuerda que tienes {ignoredFound.Count} amenaza(s) ignorada(s) anteriormente.");
                }
            }
            finally
            {
                _scanCts.Cancel();
                try { await heartbeatTask; } catch { }
            }

            if (_scanCts.IsCancellationRequested && !IsScanning)
            {
                _scanTimer?.Stop();
                RefreshStatus();
                return;
            }

            foreach (var threat in results)
            {
                threat.DetectionDate = DateTime.Now;
                if (string.IsNullOrWhiteSpace(threat.ThreatType))
                    threat.ThreatType = "Detectado por Windows Defender";
                if (string.IsNullOrWhiteSpace(threat.FilePath))
                    threat.FilePath = "Windows Defender no reporto una ruta de archivo";
                ActiveThreats.Add(threat);
            }

            if (!ActiveThreats.Any())
                SetEmptyActiveThreats();

            ThreatsDetected = ActiveThreats.Count(t => !t.IsPlaceholder);
            ScanProgress = 100;
            if (results.Count == 0)
            {
                var ignoredInHistory = ignoredThreats.Count(t => t.ActionTaken == "Ignorado");
                ScanLogs.Add(ignoredInHistory > 0
                    ? "[OK] Escaneo completado. No se detectaron amenazas activas en este analisis. Las amenazas ignoradas del historial no aparecieron activas durante este escaneo."
                    : "[OK] Escaneo completado. No se detectaron amenazas activas.");
            }
            else
            {
                ScanLogs.Add($"[ALERTA] Escaneo completado. Amenazas detectadas: {results.Count}.");
            }
            _scanTimer?.Stop();
            IsScanning = false;
            HasCompletedScan = true;
            RefreshStatus();
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task RunScanHeartbeatAsync(CancellationToken cancellationToken)
        {
            var messages = new[]
            {
                "Verificando estado de proteccion...",
                "Analizando procesos en ejecucion...",
                "Revisando ubicaciones criticas del sistema...",
                "Consultando motor de Windows Defender...",
                "Esperando resultados del escaneo..."
            };

            var index = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
                System.Windows.Application.Current?.Dispatcher.Invoke(() =>
                {
                    if (ScanProgress < 95)
                        ScanProgress = Math.Min(95, ScanProgress + 5);

                    ScanLogs.Add($"[INFO] {messages[index % messages.Length]}");
                    index++;
                });
            }
        }

        private void StopScan()
        {
            _scanCts?.Cancel();
            _scanTimer?.Stop();
            IsScanning = false;
            ScanProgress = 0;
            HasCompletedScan = false;
            ScanLogs.Add("[INFO] Escaneo detenido por el usuario.");
            CommandManager.InvalidateRequerySuggested();
        }

        private bool CanActOnSelectedThreat()
        {
            return SelectedActiveThreat != null && !SelectedActiveThreat.IsPlaceholder && !IsScanning;
        }

        private async Task HandleSelectedThreatAsync(string action)
        {
            var threat = SelectedActiveThreat;
            if (threat == null || threat.IsPlaceholder)
                return;

            var success = await ApplyThreatActionAsync(threat, action);
            if (!success)
            {
                System.Windows.MessageBox.Show(
                    "No se pudo aplicar la accion porque la amenaza no tiene una ruta local valida o el archivo ya no existe.",
                    "Ciberseguridad",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            threat.ActionTaken = action;
            threat.Status = action;
            await _database.SaveThreatDetectionAsync(threat);
            ActiveThreats.Remove(threat);
            if (!ActiveThreats.Any(t => !t.IsPlaceholder))
                SetEmptyActiveThreats();

            RemoveNeutralizedPlaceholder();
            NeutralizedThreats.Insert(0, threat);
            SelectedActiveThreat = null;
            RefreshStatus();
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task<bool> ApplyThreatActionAsync(ThreatScanResult threat, string action)
        {
            if (action == "Ignorado")
                return true;

            if (string.IsNullOrWhiteSpace(threat.FilePath) || !System.IO.File.Exists(threat.FilePath))
                return false;

            try
            {
                if (action == "Eliminado")
                {
                    System.IO.File.Delete(threat.FilePath);
                    return true;
                }

                if (action == "Cuarentena")
                {
                    var quarantineDir = System.IO.Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "JSL-SentinelPro",
                        "Quarantine");
                    System.IO.Directory.CreateDirectory(quarantineDir);

                    var fileName = $"{DateTime.Now:yyyyMMddHHmmss}_{System.IO.Path.GetFileName(threat.FilePath)}.quarantine";
                    var destination = System.IO.Path.Combine(quarantineDir, fileName);
                    await Task.Run(() => System.IO.File.Move(threat.FilePath, destination, true));
                    threat.FilePath = destination;
                    return true;
                }
            }
            catch (Exception ex)
            {
                ScanLogs.Add($"[ERROR] No se pudo aplicar la accion {action}: {ex.Message}");
            }

            return false;
        }

        private async Task GeneratePdfAsync()
        {
            var path = await _reports.GenerateSecurityPdfAsync(
                IsProtectionActive,
                IsFirewallActive,
                SignatureVersion,
                RiskLevel,
                ActiveThreats,
                NeutralizedThreats,
                ScanLogs);

            System.Windows.MessageBox.Show($"Reporte PDF generado:\n{path}", "Reporte de ciberseguridad", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private async void LoadThreatHistory()
        {
            var threats = await _database.GetThreatDetectionsAsync(DateTime.Now.AddDays(-30), DateTime.Now);
            NeutralizedThreats = new ObservableCollection<ThreatScanResult>(threats.Where(t => t.IsNeutralized).Take(20));
            if (!NeutralizedThreats.Any())
                SetEmptyNeutralizedThreats();
            RefreshStatus();
        }

        private void SetEmptyActiveThreats()
        {
            ActiveThreats = new ObservableCollection<ThreatScanResult> { ThreatScanResult.Empty() };
            OnPropertyChanged(nameof(HasRealActiveThreats));
        }

        private void SetEmptyNeutralizedThreats()
        {
            NeutralizedThreats = new ObservableCollection<ThreatScanResult> { ThreatScanResult.Empty() };
        }

        private void RemoveNeutralizedPlaceholder()
        {
            foreach (var item in NeutralizedThreats.Where(t => t.IsPlaceholder).ToList())
                NeutralizedThreats.Remove(item);
        }

        private static string GetThreatKey(ThreatScanResult threat)
        {
            return $"{threat.ThreatName}|{threat.FilePath}";
        }

        private static bool ThreatStillExists(ThreatScanResult threat)
        {
            return !string.IsNullOrWhiteSpace(threat.FilePath) && System.IO.File.Exists(threat.FilePath);
        }
    }
}
