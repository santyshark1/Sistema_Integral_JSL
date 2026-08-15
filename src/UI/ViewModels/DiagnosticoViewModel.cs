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
    /// ViewModel de diagnostico de hardware.
    /// </summary>
    public class DiagnosticoViewModel : BaseViewModel
    {
        private readonly HardwareMonitor _hardware;
        private readonly TemperatureMonitor _temperature;
        private readonly DatabaseService _database;
        private readonly HardwareReportService _reports;

        private double _scanProgress;
        private int _componentsAnalyzed;
        private int _totalComponents = 18;
        private string _timeRemaining = "Sin iniciar";
        private bool _isScanning;
        private bool _hasScanResults;
        private bool _hasStartedScan;
        private CpuInfo _cpuInfo = new CpuInfo();
        private MemoryInfo _memoryInfo = new MemoryInfo();
        private ObservableCollection<DiskInfo> _disks = new ObservableCollection<DiskInfo>();
        private ObservableCollection<TemperatureReading> _temperatures = new ObservableCollection<TemperatureReading>();
        private ObservableCollection<string> _recommendations = new ObservableCollection<string>();
        private ObservableCollection<HardwareScan> _scanHistory = new ObservableCollection<HardwareScan>();

        public double ScanProgress { get => _scanProgress; set => SetProperty(ref _scanProgress, value); }
        public int ComponentsAnalyzed { get => _componentsAnalyzed; set => SetProperty(ref _componentsAnalyzed, value); }
        public int TotalComponents { get => _totalComponents; set => SetProperty(ref _totalComponents, value); }
        public string TimeRemaining { get => _timeRemaining; set => SetProperty(ref _timeRemaining, value); }
        public bool IsScanning { get => _isScanning; set => SetProperty(ref _isScanning, value); }
        public bool HasScanResults { get => _hasScanResults; set => SetProperty(ref _hasScanResults, value); }
        public bool HasStartedScan { get => _hasStartedScan; set => SetProperty(ref _hasStartedScan, value); }
        public CpuInfo CpuInfo { get => _cpuInfo; set => SetProperty(ref _cpuInfo, value); }
        public MemoryInfo MemoryInfo { get => _memoryInfo; set => SetProperty(ref _memoryInfo, value); }
        public ObservableCollection<DiskInfo> Disks { get => _disks; set => SetProperty(ref _disks, value); }
        public ObservableCollection<TemperatureReading> Temperatures { get => _temperatures; set => SetProperty(ref _temperatures, value); }
        public ObservableCollection<string> Recommendations { get => _recommendations; set => SetProperty(ref _recommendations, value); }
        public ObservableCollection<HardwareScan> ScanHistory { get => _scanHistory; set => SetProperty(ref _scanHistory, value); }

        public ICommand StartScanCommand { get; }
        public ICommand RefreshHardwareCommand { get; }
        public ICommand RecognizePcCommand { get; }

        public DiagnosticoViewModel(HardwareMonitor hardware, TemperatureMonitor temperature, DatabaseService database)
        {
            _hardware = hardware;
            _temperature = temperature;
            _database = database;
            _reports = new HardwareReportService(_hardware, _temperature, _database);

            StartScanCommand = new RelayCommand(async _ => await StartScanAsync(), _ => !IsScanning);
            RefreshHardwareCommand = new RelayCommand(_ => RefreshHardware());
            RecognizePcCommand = new RelayCommand(async _ => await RecognizePcAsync(), _ => !IsScanning && HasStartedScan && HasScanResults);

            LoadHistory();
        }

        private void RefreshHardware()
        {
            CpuInfo = _hardware.GetCpuInfo();
            MemoryInfo = _hardware.GetMemoryInfo();
            Disks = new ObservableCollection<DiskInfo>(_hardware.GetDiskInfo());
            Temperatures = new ObservableCollection<TemperatureReading>(_temperature.GetAllTemperatures());
            GenerateRecommendations();
            HasScanResults = true;
        }

        private void ReportProgress(int done, int total, DateTime startTime)
        {
            ComponentsAnalyzed = done;
            ScanProgress = done * 100.0 / total;
            var elapsed = DateTime.Now - startTime;
            var remaining = done > 0
                ? TimeSpan.FromSeconds(elapsed.TotalSeconds / done * (total - done))
                : TimeSpan.Zero;
            TimeRemaining = $"{remaining.Minutes}:{remaining.Seconds:D2}";
        }

        private async Task StartScanAsync()
        {
            IsScanning = true;
            ScanProgress = 0;
            ComponentsAnalyzed = 0;
            TimeRemaining = "Analizando...";
            HasScanResults = false;
            var startTime = DateTime.Now;

            // Fases REALES del escaneo. Cada lectura de hardware se ejecuta en segundo
            // plano (Task.Run) para no congelar la UI; el 'await' devuelve el control al
            // hilo de UI, donde se asigna la propiedad enlazada. El progreso refleja
            // trabajo real, no un retardo artificial.
            TotalComponents = 4;

            var cpu = await Task.Run(() => _hardware.GetCpuInfo());
            CpuInfo = cpu;
            ReportProgress(1, TotalComponents, startTime);

            var memory = await Task.Run(() => _hardware.GetMemoryInfo());
            MemoryInfo = memory;
            ReportProgress(2, TotalComponents, startTime);

            var disks = await Task.Run(() => _hardware.GetDiskInfo());
            Disks = new ObservableCollection<DiskInfo>(disks);
            ReportProgress(3, TotalComponents, startTime);

            var temps = await Task.Run(() => _temperature.GetAllTemperatures());
            Temperatures = new ObservableCollection<TemperatureReading>(temps);
            ReportProgress(4, TotalComponents, startTime);

            GenerateRecommendations();
            HasScanResults = true;
            HasStartedScan = true;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();

            var scan = new HardwareScan
            {
                ScanDate = DateTime.Now,
                CpuUsage = CpuInfo.UsagePercent,
                RamUsedBytes = (ulong)(MemoryInfo.UsedGB * 1024 * 1024 * 1024),
                RamTotalBytes = (ulong)(MemoryInfo.TotalGB * 1024 * 1024 * 1024),
                DiskUsedBytes = Disks.Count > 0 ? (ulong)(Disks[0].UsedGB * 1024 * 1024 * 1024) : 0,
                DiskTotalBytes = Disks.Count > 0 ? (ulong)(Disks[0].TotalGB * 1024 * 1024 * 1024) : 0,
                MaxTemperature = Temperatures.Count > 0 ? Temperatures.Max(t => t.ValueCelsius) : 0,
                Status = Temperatures.Any(t => t.ValueCelsius > 85) ? "Critico" : "Normal"
            };

            await _database.SaveHardwareScanAsync(scan);
            LoadHistory();
            TimeRemaining = "Completado";
            IsScanning = false;
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private async Task RecognizePcAsync()
        {
            RefreshHardware();
            var path = await _reports.GeneratePcRecognitionPdfAsync();
            System.Windows.MessageBox.Show($"Reporte PDF generado:\n{path}", "Reconoce tu PC", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void GenerateRecommendations()
        {
            var recs = new List<string>();
            if (MemoryInfo.UsagePercent > 80)
                recs.Add("[ALTO] El uso de RAM es elevado. Considere cerrar aplicaciones innecesarias.");
            if (Disks.Any(d => d.UsagePercent > 85))
                recs.Add("[ALTO] El disco esta casi lleno. Libere espacio o considere una unidad nueva.");
            if (Temperatures.Any(t => t.ValueCelsius > 80))
                recs.Add("[ALTO] Temperaturas elevadas detectadas. Limpie los ventiladores y verifique la pasta termica.");
            if (CpuInfo.UsagePercent > 90)
                recs.Add("[MEDIO] CPU sobrecargada. Revise procesos en segundo plano.");
            if (!recs.Any())
                recs.Add("[INFO] El sistema opera dentro de parametros normales. Mantenimiento preventivo recomendado cada 3 meses.");
            Recommendations = new ObservableCollection<string>(recs);
        }

        private async void LoadHistory()
        {
            var scans = await _database.GetHardwareScansAsync(DateTime.Now.AddDays(-30), DateTime.Now);
            ScanHistory = new ObservableCollection<HardwareScan>(scans.Take(10));
        }
    }
}
