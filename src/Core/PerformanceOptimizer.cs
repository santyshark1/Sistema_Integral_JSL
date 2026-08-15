using System.Linq;
using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;
using Microsoft.Win32;
using System.ServiceProcess;
using System.Diagnostics;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Optimizador de rendimiento del sistema.
    /// </summary>
    public class PerformanceOptimizer
    {
        /// <summary>
        /// Obtiene ajustes recomendados de optimizacion.
        /// </summary>
        public List<OptimizationSetting> GetRecommendedSettings()
        {
            var settings = new List<OptimizationSetting>();

            settings.Add(new OptimizationSetting
            {
                Id = "visual_effects",
                Name = "Reducir efectos visuales de Windows",
                Description = "Ajusta Windows para mejor rendimiento desactivando animaciones",
                Priority = "Alto",
                IsRecommended = false,
                EstimatedGainPercent = 8,
                Category = "Rendimiento"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "startup_optimization",
                Name = "Optimizar programas de inicio",
                Description = "Desactiva aplicaciones innecesarias al iniciar Windows",
                Priority = "Alto",
                IsRecommended = false,
                EstimatedGainPercent = 15,
                Category = "Rendimiento"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "power_plan_high",
                Name = "Plan de energia: maximo rendimiento",
                Description = "Aumenta el rendimiento del PC; en portatiles la bateria puede descargarse mas rapido",
                Priority = "Medio",
                IsRecommended = false,
                EstimatedGainPercent = 5,
                Category = "Energia"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "power_plan_saver",
                Name = "Plan de energia: ahorro de bateria",
                Description = "Reduce consumo y guarda bateria; puede bajar un poco el rendimiento",
                Priority = "Medio",
                IsRecommended = false,
                EstimatedGainPercent = 2,
                Category = "Energia"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "lower_brightness",
                Name = "Bajar brillo de pantalla",
                Description = "Reduce el brillo para disminuir consumo de bateria cuando el hardware lo permite",
                Priority = "Bajo",
                IsRecommended = false,
                EstimatedGainPercent = 2,
                Category = "Energia"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "night_light",
                Name = "Colocar luz nocturna",
                Description = "Abre el control de luz nocturna de Windows para reducir luz azul de la pantalla",
                Priority = "Bajo",
                IsRecommended = false,
                EstimatedGainPercent = 1,
                Category = "Pantalla"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "disable_bluetooth",
                Name = "Desactivar Bluetooth",
                Description = "Apaga el servicio Bluetooth si esta activo para ahorrar bateria y recursos",
                Priority = "Bajo",
                IsRecommended = false,
                EstimatedGainPercent = 2,
                Category = "Servicios"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "close_background_apps",
                Name = "Cerrar aplicaciones en segundo plano",
                Description = "Pregunta antes de cerrar navegadores y apps comunes abiertas en segundo plano",
                Priority = "Medio",
                IsRecommended = false,
                EstimatedGainPercent = 8,
                Category = "Rendimiento"
            });

            settings.Add(new OptimizationSetting
            {
                Id = "disk_defrag",
                Name = "Optimizar unidades de disco",
                Description = "Desfragmenta HDD o ejecuta TRIM en SSD",
                Priority = "Medio",
                IsRecommended = false,
                EstimatedGainPercent = 10,
                Category = "Almacenamiento"
            });

            return settings;
        }

        private double GetRamGB()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var total = Convert.ToUInt64(obj["TotalVisibleMemorySize"] ?? 0);
                    return total / (1024.0 * 1024.0);
                }
            }
            catch { }
            return 8;
        }

        /// <summary>
        /// Aplica un ajuste de optimizacion especifico.
        /// </summary>
        public async Task<bool> ApplyOptimizationAsync(string settingId)
        {
            return settingId switch
            {
                "visual_effects" => await ApplyVisualEffects(),
                "power_plan_high" => await ApplyPowerPlan("8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c"),
                "power_plan_saver" => await ApplyBatterySaverPlanAsync(),
                "lower_brightness" => await LowerBrightnessAsync(),
                "night_light" => await ApplyNightLightAsync(),
                "disable_bluetooth" => await DisableBluetoothAsync(),
                "close_background_apps" => await CloseBackgroundAppsAsync(),
                "disk_defrag" => await OptimizeDrives(),
                _ => false
            };
        }

        /// <summary>
        /// Estima la mejora de rendimiento basado en el estado actual.
        /// </summary>
        public double EstimatePerformanceGain(List<OptimizationSetting> selected)
        {
            var baseGain = selected.Where(s => s.IsRecommended || s.IsApplied).Sum(s => s.EstimatedGainPercent);
            return Math.Min(baseGain, 45);
        }

        private async Task<bool> ApplyVisualEffects()
        {
            try
            {
                var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects", true);
                if (key == null)
                    key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VisualEffects");
                key?.SetValue("VisualFXSetting", 2, RegistryValueKind.DWord);
                key?.Close();

                var currentKey = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", true);
                currentKey?.SetValue("UserPreferencesMask", new byte[] { 0x90, 0x12, 0x03, 0x80, 0x10, 0x00, 0x00, 0x00 }, RegistryValueKind.Binary);
                currentKey?.Close();

                return await Task.FromResult(true);
            }
            catch { return false; }
        }

        private async Task<bool> ApplyPowerPlan(string planGuid)
        {
            try
            {
                var extra = planGuid == "a1841308-3541-4fab-bc81-f71556f20b4a"
                    ? " & powercfg /setdcvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD 100 & powercfg /setacvalueindex SCHEME_CURRENT SUB_ENERGYSAVER ESBATTTHRESHOLD 100 & powercfg /setactive SCHEME_CURRENT"
                    : "";
                var psi = new ProcessStartInfo("cmd.exe", $"/c powercfg /setactive {planGuid}{extra}")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    return process.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }

        private async Task<bool> ApplyBatterySaverPlanAsync()
        {
            try
            {
                const string saverPlan = "a1841308-3541-4fab-bc81-f71556f20b4a";
                var commands = new[]
                {
                    $"powercfg /setdcvalueindex {saverPlan} SUB_ENERGYSAVER ESBATTTHRESHOLD 100",
                    $"powercfg /setacvalueindex {saverPlan} SUB_ENERGYSAVER ESBATTTHRESHOLD 100",
                    $"powercfg /setactive {saverPlan}"
                };

                var ok = true;
                foreach (var command in commands)
                    ok &= await RunCommandAsync("cmd.exe", $"/c {command}");

                try
                {
                    using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\BatterySaver");
                    key?.SetValue("EnergySaverStatus", 1, RegistryValueKind.DWord);
                    key?.SetValue("BatterySaverOn", 1, RegistryValueKind.DWord);
                }
                catch { }

                return ok;
            }
            catch { return false; }
        }

        private async Task<bool> RunCommandAsync(string fileName, string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo(fileName, arguments)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null)
                    return false;
                await Task.Run(() => process.WaitForExit());
                return process.ExitCode == 0;
            }
            catch { return false; }
        }

        private async Task<bool> ApplyNightLightAsync()
        {
            try
            {
                Process.Start(new ProcessStartInfo("ms-settings:nightlight")
                {
                    UseShellExecute = true
                });
                return await Task.FromResult(true);
            }
            catch { return false; }
        }

        private async Task<bool> LowerBrightnessAsync()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightness");
                var current = 100;
                foreach (ManagementObject obj in searcher.Get())
                {
                    current = Convert.ToInt32(obj["CurrentBrightness"] ?? 100);
                    break;
                }

                var target = 50;
                using var methods = new ManagementObjectSearcher(@"root\WMI", "SELECT * FROM WmiMonitorBrightnessMethods");
                var applied = false;
                foreach (ManagementObject obj in methods.Get())
                {
                    obj.InvokeMethod("WmiSetBrightness", new object[] { 1, target });
                    applied = true;
                }
                return await Task.FromResult(applied);
            }
            catch { return false; }
        }

        private async Task<bool> DisableBluetoothAsync()
        {
            try
            {
                await RunPowerShellAsync("Get-PnpDevice -Class Bluetooth -ErrorAction SilentlyContinue | Where-Object Status -eq 'OK' | Disable-PnpDevice -Confirm:$false -ErrorAction SilentlyContinue");
                using var service = new ServiceController("bthserv");
                if (service.Status == ServiceControllerStatus.Running)
                {
                    service.Stop();
                    service.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(10));
                }
                return await Task.FromResult(true);
            }
            catch { return false; }
        }

        private async Task<bool> RunPowerShellAsync(string command)
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe", $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var process = Process.Start(psi);
                if (process == null)
                    return false;
                await Task.Run(() => process.WaitForExit());
                return process.ExitCode == 0;
            }
            catch { return false; }
        }

        private async Task<bool> CloseBackgroundAppsAsync()
        {
            return await Task.Run(() =>
            {
                var processNames = new[]
                {
                    "chrome", "msedge", "firefox", "opera", "brave",
                    "spotify", "discord", "teams", "slack"
                };
                var closed = 0;
                foreach (var name in processNames)
                {
                    foreach (var process in Process.GetProcessesByName(name))
                    {
                        try
                        {
                            if (process.CloseMainWindow())
                            {
                                if (!process.WaitForExit(3000))
                                    process.Kill(true);
                            }
                            else
                            {
                                process.Kill(true);
                            }
                            closed++;
                        }
                        catch { }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
                return closed > 0;
            });
        }

        private async Task<bool> OptimizeDrives()
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", "-Command \"Get-Volume | Where-Object DriveType -eq Fixed | ForEach-Object { Optimize-Volume -DriveLetter $_.DriveLetter -Analyze -ErrorAction SilentlyContinue; Optimize-Volume -DriveLetter $_.DriveLetter -ErrorAction SilentlyContinue }\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };
                using var process = System.Diagnostics.Process.Start(psi);
                if (process != null)
                {
                    await Task.Run(() => process.WaitForExit());
                    return process.ExitCode == 0;
                }
            }
            catch { }
            return false;
        }
    }
}
