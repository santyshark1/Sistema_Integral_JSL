using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Servicio de monitoreo continuo del sistema.
    /// </summary>
    public class SystemMonitorService : IDisposable
    {
        private readonly DatabaseService _database;
        private readonly HardwareMonitor _hardware;
        private readonly TemperatureMonitor _temperature;
        private Timer? _timer;
        private DateTime _lastSnapshot = DateTime.MinValue;
        private bool _isDisposed;

        public event EventHandler<SystemSnapshot>? OnSnapshot;
        public event EventHandler<Alert>? OnAlert;

        public SystemSnapshot? CurrentSnapshot { get; private set; }
        public List<Alert> ActiveAlerts { get; private set; } = new List<Alert>();

        public SystemMonitorService(DatabaseService database)
        {
            _database = database;
            _hardware = new HardwareMonitor();
            _temperature = new TemperatureMonitor();
        }

        public void Start()
        {
            _timer = new Timer(OnTimerTick, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        public void Stop()
        {
            _timer?.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void OnTimerTick(object? state)
        {
            try
            {
                var cpu = _hardware.GetCpuUsage();
                var mem = _hardware.GetMemoryInfo();
                var disks = _hardware.GetDiskInfo() ?? new List<DiskInfo>();
                var mainDisk = disks.Count > 0 ? disks[0] : null;
                var temps = _temperature.GetAllTemperatures() ?? new List<TemperatureReading>();
                var network = _hardware.GetNetworkInfo();

                // BYPASS: Calculamos la temperatura máxima manualmente
                double maxTemperatura = 0;
                if (temps.Count > 0)
                {
                    foreach (var t in temps)
                    {
                        if (t.ValueCelsius > maxTemperatura) 
                            maxTemperatura = t.ValueCelsius;
                    }
                }

                var snapshot = new SystemSnapshot
                {
                    Timestamp = DateTime.Now,
                    CpuUsage = cpu,
                    RamUsedPercent = mem.UsagePercent,
                    DiskUsedPercent = mainDisk?.UsagePercent ?? 0,
                    MaxTemp = maxTemperatura,
                    NetworkSpeedMbps = network.SpeedMbps
                };

                CurrentSnapshot = snapshot;

                Application.Current?.Dispatcher.Invoke(() =>
                {
                    OnSnapshot?.Invoke(this, snapshot);
                });

                CheckAlerts(snapshot, mem, disks, temps);

                if (DateTime.Now - _lastSnapshot >= TimeSpan.FromMinutes(5))
                {
                    _ = Task.Run(async () =>
                    {
                        try { await _database.SaveSystemSnapshotAsync(snapshot); }
                        catch { }
                    });
                    _lastSnapshot = DateTime.Now;
                }
            }
            catch { }
        }

        private void CheckAlerts(SystemSnapshot snapshot, MemoryInfo mem, List<DiskInfo> disks, List<TemperatureReading> temps)
        {
            if (snapshot.MaxTemp > 85)
            {
                RaiseAlert("Temperatura Critica", $"La temperatura maxima del sistema alcanzo {snapshot.MaxTemp:F1}C", "Critico");
            }

            foreach (var disk in disks)
            {
                if (disk.UsagePercent > 90)
                {
                    RaiseAlert("Disco casi lleno", $"La unidad {disk.DriveLetter} esta al {disk.UsagePercent:F1}% de su capacidad", "Critico");
                }
            }

            if (mem.UsagePercent > 95)
            {
                RaiseAlert("RAM Critica", $"El uso de memoria RAM alcanzo el {mem.UsagePercent:F1}%", "Critico");
            }

            if (snapshot.CpuUsage > 95)
            {
                RaiseAlert("CPU Sobrecargada", $"El uso del procesador alcanzo el {snapshot.CpuUsage:F1}%", "Alta");
            }
        }

        private void RaiseAlert(string title, string message, string type)
        {
            var alert = new Alert
            {
                Title = title,
                Message = message,
                Type = type,
                Timestamp = DateTime.Now
            };

            ActiveAlerts.Add(alert);
            if (ActiveAlerts.Count > 50)
                ActiveAlerts.RemoveAt(0);

            Application.Current?.Dispatcher.Invoke(() =>
            {
                OnAlert?.Invoke(this, alert);
            });
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Stop();
                _timer?.Dispose();
                _temperature?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
