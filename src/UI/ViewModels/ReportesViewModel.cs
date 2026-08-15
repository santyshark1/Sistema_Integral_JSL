using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace JSL_SentinelPro.src.UI.ViewModels
{
    public class ReportesViewModel : BaseViewModel
    {
        private readonly DatabaseService _database;
        private readonly SystemMonitorService _monitor;
        private readonly HardwareMonitor _hardware;

        private double _globalHealth = 85;
        private int _threatsBlocked;
        private double _monthlyUptime = 99.2;
        private int _maintenanceCount;
        private ObservableCollection<SystemSnapshot> _monthlyData = new ObservableCollection<SystemSnapshot>();
        private ObservableCollection<ThreatScanResult> _recentThreats = new ObservableCollection<ThreatScanResult>();
        private ObservableCollection<MaintenanceLog> _maintenanceLogs = new ObservableCollection<MaintenanceLog>();
        private List<ISeries> _performanceSeries = new List<ISeries>();
        private string _systemProcessor = "Desconocido";
        private string _osVersion = "Windows";
        private string _technicalSummary = "Sin informacion tecnica.";
        private string _healthTrendIcon = "OK";
        private string _healthTrendText = "Sin tendencia";
        private string _healthTrendColor = "#6B7280";
        private string _performanceInsight = "Aun no hay suficientes datos para comparar.";
        private List<ThreatScanResult> _allThreats = new List<ThreatScanResult>();

        public double GlobalHealth { get => _globalHealth; set => SetProperty(ref _globalHealth, value); }
        public int ThreatsBlocked { get => _threatsBlocked; set => SetProperty(ref _threatsBlocked, value); }
        public double MonthlyUptime { get => _monthlyUptime; set => SetProperty(ref _monthlyUptime, value); }
        public int MaintenanceCount { get => _maintenanceCount; set => SetProperty(ref _maintenanceCount, value); }
        public ObservableCollection<SystemSnapshot> MonthlyData { get => _monthlyData; set => SetProperty(ref _monthlyData, value); }
        public ObservableCollection<ThreatScanResult> RecentThreats { get => _recentThreats; set => SetProperty(ref _recentThreats, value); }
        public ObservableCollection<MaintenanceLog> MaintenanceLogs { get => _maintenanceLogs; set => SetProperty(ref _maintenanceLogs, value); }
        public List<ISeries> PerformanceSeries { get => _performanceSeries; set => SetProperty(ref _performanceSeries, value); }
        public string SystemProcessor { get => _systemProcessor; set => SetProperty(ref _systemProcessor, value); }
        public string OsVersion { get => _osVersion; set => SetProperty(ref _osVersion, value); }
        public string TechnicalSummary { get => _technicalSummary; set => SetProperty(ref _technicalSummary, value); }
        public string HealthTrendIcon { get => _healthTrendIcon; set => SetProperty(ref _healthTrendIcon, value); }
        public string HealthTrendText { get => _healthTrendText; set => SetProperty(ref _healthTrendText, value); }
        public string HealthTrendColor { get => _healthTrendColor; set => SetProperty(ref _healthTrendColor, value); }
        public string PerformanceInsight { get => _performanceInsight; set => SetProperty(ref _performanceInsight, value); }

        public ICommand ExportCsvCommand { get; }
        public ICommand GeneratePdfCommand { get; }
        public ICommand RefreshCommand { get; }

        public ReportesViewModel(DatabaseService database, SystemMonitorService monitor, HardwareMonitor hardware)
        {
            _database = database;
            _monitor = monitor;
            _hardware = hardware;

            ExportCsvCommand = new RelayCommand(async _ => await ExportCsvAsync());
            GeneratePdfCommand = new RelayCommand(async _ => await GeneratePdfAsync());
            RefreshCommand = new RelayCommand(_ => LoadData());

            LoadData();
        }

        private async void LoadData()
        {
            var from = DateTime.Now.AddMonths(-6);
            var to = DateTime.Now;
            var snapshots = await _database.GetSystemHistoryAsync(from, to);
            MonthlyData = new ObservableCollection<SystemSnapshot>(snapshots);

            _allThreats = await _database.GetThreatDetectionsAsync(from, to);
            var threats48h = _allThreats.Where(t => t.DetectionDate >= DateTime.Now.AddHours(-48)).ToList();
            RecentThreats = new ObservableCollection<ThreatScanResult>(
                threats48h.Any() ? threats48h : new[] { ThreatScanResult.Empty("Ninguna actualmente") });
            ThreatsBlocked = _allThreats.Count(t => t.IsNeutralized);

            var logs = await _database.GetMaintenanceLogsAsync(100);
            MaintenanceLogs = new ObservableCollection<MaintenanceLog>(logs.Take(20));
            MaintenanceCount = logs.Count;

            GlobalHealth = CalculateHealth(snapshots, _allThreats, logs);
            CalculateTrend(snapshots, _allThreats, logs);
            GeneratePerformanceChart(snapshots, _allThreats, logs);
            LoadTechnicalSummary();
        }

        private double CalculateHealth(List<SystemSnapshot> snapshots, List<ThreatScanResult> threats, List<MaintenanceLog> logs)
        {
            var recent = snapshots.Where(s => s.Timestamp >= DateTime.Now.AddDays(-7)).ToList();
            var activeUnresolvedThreats = threats.Count(t =>
                !t.IsPlaceholder &&
                !t.IsNeutralized &&
                t.ActionTaken != "Ignorado" &&
                t.DetectionDate >= DateTime.Now.AddDays(-7));

            if (!recent.Any())
            {
                if (activeUnresolvedThreats > 0)
                    return 62;
                return logs.Any() ? 92 : 95;
            }

            var avgCpu = recent.Average(s => s.CpuUsage);
            var avgRam = recent.Average(s => s.RamUsedPercent);
            var avgDisk = recent.Average(s => s.DiskUsedPercent);
            var avgTemp = recent.Average(s => s.MaxTemp);
            var health = 100.0 - (avgCpu * 0.18 + avgRam * 0.22 + avgDisk * 0.18 + Math.Max(0, avgTemp - 45) * 0.35);
            health -= activeUnresolvedThreats * 12;
            health += Math.Min(8, logs.Count(l => l.ActionDate >= DateTime.Now.AddDays(-30)) * 1.5);
            return Math.Max(0, Math.Min(100, Math.Round(health, 1)));
        }

        private void CalculateTrend(List<SystemSnapshot> snapshots, List<ThreatScanResult> threats, List<MaintenanceLog> logs)
        {
            var recent = snapshots.Where(s => s.Timestamp >= DateTime.Now.AddDays(-7)).ToList();
            var previous = snapshots.Where(s => s.Timestamp < DateTime.Now.AddDays(-7) && s.Timestamp >= DateTime.Now.AddDays(-14)).ToList();

            if (!recent.Any() || !previous.Any())
            {
                if (GlobalHealth >= 80)
                {
                    HealthTrendIcon = "OK";
                    HealthTrendText = "Estado saludable";
                    HealthTrendColor = "#22C55E";
                }
                else if (GlobalHealth < 60)
                {
                    HealthTrendIcon = "DOWN";
                    HealthTrendText = "Requiere atencion";
                    HealthTrendColor = "#EF4444";
                }
                else
                {
                    HealthTrendIcon = "OK";
                    HealthTrendText = "Sin comparacion previa";
                    HealthTrendColor = "#F59E0B";
                }
                return;
            }

            var currentHealth = CalculateHealth(recent, threats, logs);
            var previousHealth = CalculateHealth(previous, threats.Where(t => t.DetectionDate < DateTime.Now.AddDays(-7)).ToList(), logs.Where(l => l.ActionDate < DateTime.Now.AddDays(-7)).ToList());
            var diff = Math.Round(currentHealth - previousHealth, 1);

            if (diff >= 1)
            {
                HealthTrendIcon = "UP";
                HealthTrendText = $"Subiendo +{diff:F1}%";
                HealthTrendColor = "#22C55E";
            }
            else if (diff <= -1)
            {
                HealthTrendIcon = "DOWN";
                HealthTrendText = $"Declinando {diff:F1}%";
                HealthTrendColor = "#EF4444";
            }
            else
            {
                HealthTrendIcon = "OK";
                HealthTrendText = "Estable";
                HealthTrendColor = "#6B7280";
            }
        }

        private void GeneratePerformanceChart(List<SystemSnapshot> snapshots, List<ThreatScanResult> threats, List<MaintenanceLog> logs)
        {
            var months = Enumerable.Range(0, 6)
                .Select(i => new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(i - 5))
                .ToList();

            var healthByMonth = months.Select(month =>
            {
                var monthSnapshots = snapshots.Where(s => s.Timestamp.Year == month.Year && s.Timestamp.Month == month.Month).ToList();
                var monthThreats = threats.Where(t => t.DetectionDate.Year == month.Year && t.DetectionDate.Month == month.Month).ToList();
                var monthLogs = logs.Where(l => l.ActionDate.Year == month.Year && l.ActionDate.Month == month.Month).ToList();
                return CalculateHealth(monthSnapshots, monthThreats, monthLogs);
            }).ToList();

            var threatsByMonth = months.Select(month => (double)threats.Count(t => t.DetectionDate.Year == month.Year && t.DetectionDate.Month == month.Month)).ToList();
            var maintenanceByMonth = months.Select(month => (double)logs.Count(l => l.ActionDate.Year == month.Year && l.ActionDate.Month == month.Month)).ToList();

            PerformanceInsight = BuildPerformanceInsight(healthByMonth, threatsByMonth, maintenanceByMonth);

            PerformanceSeries = new List<ISeries>
            {
                new LineSeries<double>
                {
                    Values = healthByMonth,
                    Name = "Salud del sistema",
                    Stroke = new SolidColorPaint(SKColor.Parse("#22C55E"), 3),
                    Fill = null,
                    GeometrySize = 6
                },
                new LineSeries<double>
                {
                    Values = maintenanceByMonth,
                    Name = "Mantenimientos",
                    Stroke = new SolidColorPaint(SKColor.Parse("#0078D4"), 3),
                    Fill = null,
                    GeometrySize = 6
                },
                new LineSeries<double>
                {
                    Values = threatsByMonth,
                    Name = "Amenazas",
                    Stroke = new SolidColorPaint(SKColor.Parse("#EF4444"), 3),
                    Fill = null,
                    GeometrySize = 6
                }
            };
        }

        private static string BuildPerformanceInsight(List<double> health, List<double> threats, List<double> maintenance)
        {
            var diff = health.LastOrDefault() - health.FirstOrDefault();
            var threatTotal = threats.Sum();
            var maintenanceTotal = maintenance.Sum();
            return $"En los ultimos 6 meses la salud cambio {diff:+0.0;-0.0;0.0} puntos. Mantenimientos registrados: {maintenanceTotal:F0}. Amenazas registradas: {threatTotal:F0}.";
        }

        private void LoadTechnicalSummary()
        {
            var cpu = _hardware.GetCpuInfo();
            var memory = _hardware.GetMemoryInfo();
            var disks = _hardware.GetDiskInfo();
            var mainDisk = disks.FirstOrDefault();

            SystemProcessor = string.IsNullOrWhiteSpace(cpu.Name) ? "Desconocido" : cpu.Name;
            OsVersion = Environment.OSVersion.ToString();
            TechnicalSummary = $"CPU: {SystemProcessor}. RAM: {memory.TotalGB:F1} GB. Disco principal: {(mainDisk == null ? "No detectado" : $"{mainDisk.DriveLetter} {mainDisk.DriveType} {mainDisk.TotalGB:F1} GB, {mainDisk.FreeGB:F1} GB libres")}. SO: {OsVersion}.";
        }

        private async Task ExportCsvAsync()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"JSL_Report_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            var lines = new List<string>
            {
                "Seccion,Fecha,Tipo,CPU%,RAM%,Disco%,TempC,RedMbps,Amenaza,Accion,Severidad,EspacioGB,Detalles",
                $"Resumen,{DateTime.Now:yyyy-MM-dd HH:mm},Salud,{GlobalHealth:F2},,,,,,,,{MaintenanceCount},\"{TechnicalSummary.Replace("\"", "'")}\""
            };

            foreach (var s in MonthlyData)
                lines.Add($"Rendimiento,{s.Timestamp:yyyy-MM-dd HH:mm},Snapshot,{s.CpuUsage:F2},{s.RamUsedPercent:F2},{s.DiskUsedPercent:F2},{s.MaxTemp:F2},{s.NetworkSpeedMbps:F2},,,,,");

            foreach (var t in _allThreats)
                lines.Add($"Amenazas,{t.DetectionDate:yyyy-MM-dd HH:mm},{t.ThreatType},,,,,,{EscapeCsv(t.ThreatName)},{t.ActionTaken},{t.Severity},,");

            foreach (var log in MaintenanceLogs)
                lines.Add($"Mantenimiento,{log.ActionDate:yyyy-MM-dd HH:mm},{log.ActionType},,,,,,,,,{log.SpaceFreedGB:F2},{EscapeCsv(log.Details)}");

            await File.WriteAllLinesAsync(path, lines);
            OpenFile(path);
        }

        private async Task GeneratePdfAsync()
        {
            var lines = new List<string>
            {
                "JSL SentinelPro - Reporte general",
                $"Fecha: {DateTime.Now:yyyy-MM-dd HH:mm}",
                "",
                "Resumen ejecutivo",
                $"Salud global: {GlobalHealth:F1}% ({HealthTrendText})",
                $"Amenazas bloqueadas: {ThreatsBlocked}",
                $"Mantenimientos registrados: {MaintenanceCount}",
                PerformanceInsight,
                "",
                "Resumen tecnico del sistema",
                TechnicalSummary,
                "",
                "Amenazas detectadas en las ultimas 48 horas"
            };

            foreach (var threat in RecentThreats)
                lines.Add($"{threat.DetectionDate:yyyy-MM-dd HH:mm} - {threat.ThreatName} - {threat.ActionTaken} - {threat.Severity}");

            lines.Add("");
            lines.Add("Mantenimientos recientes");
            foreach (var log in MaintenanceLogs.Take(15))
                lines.Add($"{log.ActionDate:yyyy-MM-dd HH:mm} - {log.ActionType} - {log.SpaceFreedGB:F2} GB - {log.Details}");

            lines.Add("");
            lines.Add("Sustento del reporte");
            lines.Add("Este reporte usa historial de rendimiento, amenazas guardadas, acciones de mantenimiento y lectura actual del hardware.");

            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), $"JSL_Reporte_General_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
            await PdfDocumentWriter.WriteAsync(path, lines, "Reporte general");
            OpenFile(path);
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            return $"\"{value.Replace("\"", "'")}\"";
        }

        private static void OpenFile(string path)
        {
            try
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch { }
        }
    }
}
