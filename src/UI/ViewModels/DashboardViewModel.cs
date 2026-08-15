using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    /// <summary>
    /// ViewModel del panel principal (Dashboard).
    /// </summary>
    public class DashboardViewModel : BaseViewModel
    {
        private readonly SystemMonitorService _monitor;
        private readonly HardwareMonitor _hardware;
        private readonly DatabaseService _database;
        private readonly HardwareReportService _reports;

        private double _cpuUsage;
        private double _ramUsedGB;
        private double _ramTotalGB;
        private double _ramPercent;
        private double _diskUsedGB;
        private double _diskTotalGB;
        private double _diskPercent;
        private double _temperature;
        private double _networkSpeed;
        private string _uptimeText = "0d 0h 0m";
        private int _alertCount;
        private bool _hasCriticalAlerts;
        private DateTime _lastHardwareScan = DateTime.MinValue;
        private ObservableCollection<HardwareScan> _recentScans = new ObservableCollection<HardwareScan>();
        private ObservableCollection<Alert> _activeAlerts = new ObservableCollection<Alert>();
        private List<ISeries> _cpuSeries = new List<ISeries>();
        private readonly List<double> _cpuHistory = new List<double>();
        private readonly List<double> _ramHistory = new List<double>();
        private readonly List<double> _diskHistory = new List<double>();

        public double CpuUsage { get => _cpuUsage; set => SetProperty(ref _cpuUsage, value); }
        public double RamUsedGB { get => _ramUsedGB; set => SetProperty(ref _ramUsedGB, value); }
        public double RamTotalGB { get => _ramTotalGB; set => SetProperty(ref _ramTotalGB, value); }
        public double RamPercent { get => _ramPercent; set => SetProperty(ref _ramPercent, value); }
        public double DiskUsedGB { get => _diskUsedGB; set => SetProperty(ref _diskUsedGB, value); }
        public double DiskTotalGB { get => _diskTotalGB; set => SetProperty(ref _diskTotalGB, value); }
        public double DiskPercent { get => _diskPercent; set => SetProperty(ref _diskPercent, value); }
        public double Temperature { get => _temperature; set => SetProperty(ref _temperature, value); }
        public double NetworkSpeed { get => _networkSpeed; set => SetProperty(ref _networkSpeed, value); }
        public string UptimeText { get => _uptimeText; set => SetProperty(ref _uptimeText, value); }
        public int AlertCount { get => _alertCount; set => SetProperty(ref _alertCount, value); }
        public bool HasCriticalAlerts { get => _hasCriticalAlerts; set => SetProperty(ref _hasCriticalAlerts, value); }
        public DateTime LastHardwareScan { get => _lastHardwareScan; set => SetProperty(ref _lastHardwareScan, value); }
        public ObservableCollection<HardwareScan> RecentScans { get => _recentScans; set => SetProperty(ref _recentScans, value); }
        public ObservableCollection<Alert> ActiveAlerts { get => _activeAlerts; set => SetProperty(ref _activeAlerts, value); }
        public List<ISeries> CpuSeries { get => _cpuSeries; set => SetProperty(ref _cpuSeries, value); }

        public ICommand RefreshCommand { get; }
        public ICommand ScanHardwareCommand { get; }
        public ICommand GenerateScanPdfCommand { get; }
        public ICommand ViewAlertsCommand { get; }

        public DashboardViewModel(SystemMonitorService monitor, HardwareMonitor hardware, DatabaseService database)
        {
            _monitor = monitor;
            _hardware = hardware;
            _database = database;
            _reports = new HardwareReportService(_hardware, new TemperatureMonitor(), _database);

            _monitor.OnSnapshot += OnSnapshotReceived;
            _monitor.OnAlert += OnAlertReceived;

            RefreshCommand = new RelayCommand(_ => RefreshData());
            ScanHardwareCommand = new RelayCommand(async _ => await ScanHardwareAsync());
            GenerateScanPdfCommand = new RelayCommand(async _ => await GenerateScanPdfAsync(), _ => RecentScans.Any());
            ViewAlertsCommand = new RelayCommand(_ => { });

            InitializeChart();
            RefreshData();
            LoadRecentScans();
        }

        private void InitializeChart()
        {
            CpuSeries = new List<ISeries>
            {
                new LineSeries<double>
                {
                    Values = new List<double>(),
                    Stroke = new SolidColorPaint(SKColor.Parse("#0078D4"), 2),
                    Fill = new SolidColorPaint(SKColor.Parse("#E3F2FD")),
                    GeometrySize = 0
                }
            };
        }

        private void OnSnapshotReceived(object? sender, SystemSnapshot snapshot)
        {
            CpuUsage = snapshot.CpuUsage;
            RamPercent = snapshot.RamUsedPercent;
            RamUsedGB = Math.Round(RamTotalGB * RamPercent / 100.0, 2);
            DiskPercent = snapshot.DiskUsedPercent;
            Temperature = snapshot.MaxTemp;
            NetworkSpeed = snapshot.NetworkSpeedMbps;

            _cpuHistory.Add(snapshot.CpuUsage);
            if (_cpuHistory.Count > 30) _cpuHistory.RemoveAt(0);
            _ramHistory.Add(snapshot.RamUsedPercent);
            if (_ramHistory.Count > 30) _ramHistory.RemoveAt(0);
            _diskHistory.Add(snapshot.DiskUsedPercent);
            if (_diskHistory.Count > 30) _diskHistory.RemoveAt(0);

            CpuSeries = new List<ISeries>
            {
                new LineSeries<double>
                {
                    Values = new List<double>(_cpuHistory),
                    Name = "CPU %",
                    Stroke = new SolidColorPaint(SKColor.Parse("#0078D4"), 2),
                    Fill = null,
                    GeometrySize = 0
                },
                new LineSeries<double>
                {
                    Values = new List<double>(_ramHistory),
                    Name = "RAM %",
                    Stroke = new SolidColorPaint(SKColor.Parse("#22C55E"), 2),
                    Fill = null,
                    GeometrySize = 0
                },
                new LineSeries<double>
                {
                    Values = new List<double>(_diskHistory),
                    Name = "Disco %",
                    Stroke = new SolidColorPaint(SKColor.Parse("#F59E0B"), 2),
                    Fill = null,
                    GeometrySize = 0
                }
            };
        }

        private void OnAlertReceived(object? sender, Alert alert)
        {
            System.Windows.Application.Current?.Dispatcher.Invoke(() =>
            {
                ActiveAlerts.Insert(0, alert);
                if (ActiveAlerts.Count > 10) ActiveAlerts.RemoveAt(ActiveAlerts.Count - 1);
                AlertCount = ActiveAlerts.Count;
                HasCriticalAlerts = ActiveAlerts.Any(a => a.Type == "Critico" || a.Type == "Alta");
            });
        }

        private void RefreshData()
        {
            var mem = _hardware.GetMemoryInfo();
            var disks = _hardware.GetDiskInfo();
            var uptime = _hardware.GetUptime();

            RamTotalGB = mem.TotalGB;
            RamUsedGB = mem.UsedGB;
            RamPercent = mem.UsagePercent;
            CpuUsage = _hardware.GetCpuUsage();

            if (disks.Count > 0)
            {
                var mainDisk = disks.First();
                DiskTotalGB = mainDisk.TotalGB;
                DiskUsedGB = mainDisk.UsedGB;
                DiskPercent = mainDisk.UsagePercent;
            }

            UptimeText = $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m";
            Temperature = _monitor.CurrentSnapshot?.MaxTemp ?? 0;
        }

        private async System.Threading.Tasks.Task ScanHardwareAsync()
        {
            RefreshData();

            var scan = new HardwareScan
            {
                ScanDate = DateTime.Now,
                CpuUsage = CpuUsage,
                RamUsedBytes = (ulong)(RamUsedGB * 1024 * 1024 * 1024),
                RamTotalBytes = (ulong)(RamTotalGB * 1024 * 1024 * 1024),
                DiskUsedBytes = (ulong)(DiskUsedGB * 1024 * 1024 * 1024),
                DiskTotalBytes = (ulong)(DiskTotalGB * 1024 * 1024 * 1024),
                MaxTemperature = Temperature,
                Status = Temperature > 85 ? "Critico" : "Normal"
            };

            await _database.SaveHardwareScanAsync(scan);
            LastHardwareScan = scan.ScanDate;
            LoadRecentScans();
        }

        private async void LoadRecentScans()
        {
            var scans = await _database.GetHardwareScansAsync(DateTime.Now.AddDays(-7), DateTime.Now);
            RecentScans = new ObservableCollection<HardwareScan>(scans.Take(5));
            LastHardwareScan = RecentScans.FirstOrDefault()?.ScanDate ?? DateTime.MinValue;
            CommandManager.InvalidateRequerySuggested();
        }

        private async System.Threading.Tasks.Task GenerateScanPdfAsync()
        {
            var scan = RecentScans.FirstOrDefault();
            if (scan == null)
                return;

            var path = await _reports.GenerateHardwareScanPdfAsync(scan, RecentScans);
            System.Windows.MessageBox.Show($"Reporte PDF generado:\n{path}", "Reporte de hardware", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }
    }
}
