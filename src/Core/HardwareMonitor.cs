using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Monitoreo de hardware real del sistema.
    /// </summary>
    public class HardwareMonitor
    {
        private PerformanceCounter? _cpuUtilityCounter;
        private PerformanceCounter? _cpuCounter;
        private PerformanceCounter? _diskActivityCounter;
        private readonly ManagementObjectSearcher? _memorySearcher;
        private readonly ManagementObjectSearcher? _diskSearcher;
        private readonly ManagementObjectSearcher? _networkSearcher;

        public HardwareMonitor()
        {
            try
            {
                _cpuUtilityCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
                _cpuUtilityCounter.NextValue();
            }
            catch { _cpuUtilityCounter = null; }

            try
            {
                _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
                _cpuCounter.NextValue();
            }
            catch { _cpuCounter = null; }

            if (_cpuUtilityCounter != null || _cpuCounter != null)
                Task.Delay(1000).Wait();

            try { _memorySearcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem"); } catch { }
            try { _diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDisk WHERE DriveType = 3"); } catch { }
            try { _networkSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PerfFormattedData_Tcpip_NetworkInterface"); } catch { }
            try
            {
                _diskActivityCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
                _diskActivityCounter.NextValue();
            }
            catch { _diskActivityCounter = null; }
        }

        /// <summary>
        /// Obtiene el porcentaje de uso de CPU.
        /// </summary>
        public double GetCpuUsage()
        {
            try
            {
                if (_cpuUtilityCounter != null)
                    return ClampPercent(_cpuUtilityCounter.NextValue());
            }
            catch { }

            try
            {
                if (_cpuCounter != null)
                    return ClampPercent(_cpuCounter.NextValue());
            }
            catch { }

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT PercentProcessorUtility FROM Win32_PerfFormattedData_Counters_ProcessorInformation WHERE Name = '_Total'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return ClampPercent(Convert.ToDouble(obj["PercentProcessorUtility"] ?? 0));
                }
            }
            catch { }

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT PercentProcessorTime FROM Win32_PerfFormattedData_PerfOS_Processor WHERE Name = '_Total'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    return ClampPercent(Convert.ToDouble(obj["PercentProcessorTime"] ?? 0));
                }
            }
            catch { }

            return 0;
        }

        private static double ClampPercent(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return 0;

            return Math.Min(100, Math.Max(0, Math.Round(value, 2)));
        }

        public double GetDiskActivityPercent()
        {
            try
            {
                if (_diskActivityCounter != null)
                    return Math.Min(100, Math.Round(_diskActivityCounter.NextValue(), 2));
            }
            catch { }

            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT PercentDiskTime FROM Win32_PerfFormattedData_PerfDisk_PhysicalDisk WHERE Name = '_Total'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var value = Convert.ToDouble(obj["PercentDiskTime"] ?? 0);
                    return Math.Min(100, Math.Round(value, 2));
                }
            }
            catch { }

            return 0;
        }

        /// <summary>
        /// Obtiene informacion detallada del CPU.
        /// </summary>
        public CpuInfo GetCpuInfo()
        {
            var info = new CpuInfo();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.Name = obj["Name"]?.ToString() ?? "Desconocido";
                    info.CoreCount = Convert.ToInt32(obj["NumberOfCores"] ?? 0);
                    info.ThreadCount = Convert.ToInt32(obj["NumberOfLogicalProcessors"] ?? 0);
                    info.MaxClockSpeed = Convert.ToDouble(obj["MaxClockSpeed"] ?? 0) / 1000.0;
                    info.CurrentClockSpeed = Convert.ToDouble(obj["CurrentClockSpeed"] ?? 0) / 1000.0;
                    info.Is64Bit = obj["Architecture"] != null && Convert.ToUInt16(obj["Architecture"]) == 9;
                    info.Architecture = info.Is64Bit ? "x64" : "x86";
                    break;
                }
            }
            catch { }
            info.UsagePercent = GetCpuUsage();
            info.Status = info.UsagePercent > 95 ? "Critico" : info.UsagePercent > 80 ? "Alta carga" : "Carga estable";
            return info;
        }

        /// <summary>
        /// Obtiene informacion de la memoria RAM.
        /// </summary>
        public MemoryInfo GetMemoryInfo()
        {
            var info = new MemoryInfo();
            try
            {
                if (_memorySearcher != null)
                {
                    foreach (ManagementObject obj in _memorySearcher.Get())
                    {
                        info.TotalBytes = Convert.ToUInt64(obj["TotalVisibleMemorySize"] ?? 0) * 1024;
                        info.FreeBytes = Convert.ToUInt64(obj["FreePhysicalMemory"] ?? 0) * 1024;
                        info.UsedBytes = info.TotalBytes - info.FreeBytes;
                        break;
                    }
                }
            }
            catch { }
            return info;
        }

        /// <summary>
        /// Obtiene informacion de todos los discos logicos.
        /// </summary>
        public List<DiskInfo> GetDiskInfo()
        {
            var disks = new List<DiskInfo>();
            try
            {
                if (_diskSearcher != null)
                {
                    foreach (ManagementObject obj in _diskSearcher.Get())
                    {
                        var disk = new DiskInfo
                        {
                            DriveLetter = obj["DeviceID"]?.ToString() ?? "C:",
                            Label = obj["VolumeName"]?.ToString() ?? "Disco Local",
                            TotalBytes = Convert.ToUInt64(obj["Size"] ?? 0),
                            FreeBytes = Convert.ToUInt64(obj["FreeSpace"] ?? 0),
                            FileSystem = obj["FileSystem"]?.ToString() ?? "NTFS"
                        };
                        disk.UsedBytes = disk.TotalBytes - disk.FreeBytes;
                        disk.DriveType = DetectDriveType(disk.DriveLetter);
                        disks.Add(disk);
                    }
                }
            }
            catch { }
            if (disks.Count == 0)
            {
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady && d.DriveType == DriveType.Fixed))
                {
                    disks.Add(new DiskInfo
                    {
                        DriveLetter = drive.Name,
                        Label = drive.VolumeLabel,
                        TotalBytes = (ulong)drive.TotalSize,
                        FreeBytes = (ulong)drive.AvailableFreeSpace,
                        UsedBytes = (ulong)(drive.TotalSize - drive.AvailableFreeSpace),
                        DriveType = "HDD",
                        FileSystem = drive.DriveFormat
                    });
                }
            }
            return disks;
        }

        public List<GpuInfo> GetGpuInfo()
        {
            var gpus = new List<GpuInfo>();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var memoryBytes = Convert.ToDouble(obj["AdapterRAM"] ?? 0);
                    gpus.Add(new GpuInfo
                    {
                        Name = obj["Name"]?.ToString() ?? "Desconocida",
                        DriverVersion = obj["DriverVersion"]?.ToString() ?? "Desconocido",
                        VideoProcessor = obj["VideoProcessor"]?.ToString() ?? "Desconocido",
                        MemoryGB = memoryBytes > 0 ? Math.Round(memoryBytes / (1024.0 * 1024.0 * 1024.0), 2) : 0,
                        Status = obj["Status"]?.ToString() ?? "Desconocido"
                    });
                }
            }
            catch { }

            if (gpus.Count == 0)
                gpus.Add(new GpuInfo());

            return gpus;
        }

        private string DetectDriveType(string driveLetter)
        {
            try
            {
                var deviceId = driveLetter.TrimEnd('\\');
                using var partitionSearcher = new ManagementObjectSearcher(
                    $"ASSOCIATORS OF {{Win32_LogicalDisk.DeviceID='{deviceId}'}} WHERE AssocClass=Win32_LogicalDiskToPartition");

                foreach (ManagementObject partition in partitionSearcher.Get())
                {
                    var partitionId = partition["DeviceID"]?.ToString()?.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    if (string.IsNullOrWhiteSpace(partitionId))
                        continue;

                    using var diskSearcher = new ManagementObjectSearcher(
                        $"ASSOCIATORS OF {{Win32_DiskPartition.DeviceID=\"{partitionId}\"}} WHERE AssocClass=Win32_DiskDriveToDiskPartition");

                    foreach (ManagementObject disk in diskSearcher.Get())
                    {
                        var model = disk["Model"]?.ToString()?.ToLowerInvariant() ?? "";
                        var mediaType = disk["MediaType"]?.ToString()?.ToLowerInvariant() ?? "";
                        var interfaceType = disk["InterfaceType"]?.ToString()?.ToLowerInvariant() ?? "";

                        if (model.Contains("ssd") || model.Contains("nvme") || model.Contains("m.2") ||
                            mediaType.Contains("ssd") || interfaceType.Contains("nvme"))
                            return "SSD";

                        try
                        {
                            var index = Convert.ToInt32(disk["Index"] ?? -1);
                            var busType = GetPhysicalDiskBusType(index);
                            if (busType.Contains("SSD", StringComparison.OrdinalIgnoreCase) ||
                                busType.Contains("NVMe", StringComparison.OrdinalIgnoreCase))
                                return "SSD";
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return "HDD";
        }

        private string GetPhysicalDiskBusType(int diskIndex)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -Command \"Get-PhysicalDisk | Where-Object DeviceId -eq {diskIndex} | ForEach-Object {{ $_.MediaType.ToString() + ' ' + $_.BusType.ToString() }}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null)
                    return string.Empty;

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);
                return output.Trim();
            }
            catch { return string.Empty; }
        }

        /// <summary>
        /// Obtiene informacion de red.
        /// </summary>
        public NetworkInfo GetNetworkInfo()
        {
            var info = new NetworkInfo();
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2");
                foreach (ManagementObject obj in searcher.Get())
                {
                    info.AdapterName = obj["NetConnectionID"]?.ToString() ?? obj["Name"]?.ToString() ?? "Desconocido";
                    info.MacAddress = obj["MACAddress"]?.ToString() ?? "N/A";
                    break;
                }
            }
            catch { }

            try
            {
                if (_networkSearcher != null)
                {
                    foreach (ManagementObject obj in _networkSearcher.Get())
                    {
                        var bytesSec = Convert.ToUInt64(obj["BytesTotalPersec"] ?? 0);
                        info.SpeedMbps = Math.Round(bytesSec * 8.0 / 1_000_000.0, 2);
                        info.IsConnected = true;
                        break;
                    }
                }
            }
            catch { }
            return info;
        }

        /// <summary>
        /// Obtiene el tiempo de actividad del sistema.
        /// </summary>
        public TimeSpan GetUptime()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var lastBoot = ManagementDateTimeConverter.ToDateTime(obj["LastBootUpTime"]?.ToString() ?? "");
                    return DateTime.Now - lastBoot;
                }
            }
            catch { }
            return TimeSpan.Zero;
        }
    }
}
