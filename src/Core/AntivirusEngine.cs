using System.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Motor de antivirus que utiliza Windows Defender (MpCmdRun.exe).
    /// </summary>
    public class AntivirusEngine
    {
        private readonly string _defenderPath;

        public AntivirusEngine()
        {
            _defenderPath = FindMpCmdRunPath();
        }

        private string FindMpCmdRunPath()
        {
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Windows Defender", "MpCmdRun.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Windows Defender", "MpCmdRun.exe"),
                Path.Combine(Environment.GetEnvironmentVariable("ProgramW6432") ?? "", "Windows Defender", "MpCmdRun.exe"),
                @"C:\Program Files\Windows Defender\MpCmdRun.exe",
                @"C:\Program Files (x86)\Windows Defender\MpCmdRun.exe",
                @"C:\Windows\System32\MpCmdRun.exe"
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                    return path;
            }
            return string.Empty;
        }

        /// <summary>
        /// Obtiene el estado de proteccion de Windows Defender.
        /// </summary>
        public Dictionary<string, object> GetProtectionStatus()
        {
            var status = new Dictionary<string, object>
            {
                ["IsAvailable"] = !string.IsNullOrEmpty(_defenderPath),
                ["RealTimeProtection"] = false,
                ["AntivirusEnabled"] = false,
                ["AntivirusSignatureVersion"] = "Desconocido",
                ["LastUpdate"] = DateTime.MinValue
            };

            try
            {
                using var searcher = new ManagementObjectSearcher(@"ROOT\Microsoft\Windows\Defender", "SELECT * FROM MSFT_MpComputerStatus");
                foreach (ManagementObject obj in searcher.Get())
                {
                    status["RealTimeProtection"] = Convert.ToBoolean(obj["RealTimeProtectionEnabled"] ?? false);
                    status["AntivirusEnabled"] = Convert.ToBoolean(obj["AntivirusEnabled"] ?? false);
                    break;
                }
            }
            catch { }

            try
            {
                using var searcher = new ManagementObjectSearcher(@"ROOT\Microsoft\Windows\Defender", "SELECT * FROM MSFT_MpSignature");
                foreach (ManagementObject obj in searcher.Get())
                {
                    status["AntivirusSignatureVersion"] = obj["AntivirusSignatureVersion"]?.ToString() ?? "Desconocido";
                    try
                    {
                        status["LastUpdate"] = ManagementDateTimeConverter.ToDateTime(obj["AntivirusSignatureLastUpdated"]?.ToString() ?? "");
                    }
                    catch { }
                    break;
                }
            }
            catch { }

            return status;
        }

        /// <summary>
        /// Escaneo rapido acotado sobre rutas reales de alto riesgo.
        /// </summary>
        public async Task<List<ThreatScanResult>> ScanCriticalAreasAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var results = new List<ThreatScanResult>();
                var targets = GetCriticalScanTargets();
                var files = new List<string>();
                const int maxFiles = 2500;

                progress?.Report("[INFO] Escaneo rapido acotado iniciado.");
                progress?.Report("[INFO] Alcance: inicio de Windows, Descargas, Escritorio y carpetas temporales del usuario.");

                foreach (var target in targets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (!Directory.Exists(target)) continue;

                    progress?.Report($"[RUTA] {target}");
                    foreach (var file in EnumerateFilesSafe(target, cancellationToken))
                    {
                        files.Add(file);
                        if (files.Count >= maxFiles) break;
                    }

                    if (files.Count >= maxFiles) break;
                }

                progress?.Report($"[TOTAL] {files.Count}");

                var checkedCount = 0;
                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    checkedCount++;

                    var threat = AnalyzeFileHeuristically(file);
                    if (threat != null)
                    {
                        results.Add(threat);
                        progress?.Report($"[ALERTA] {threat.ThreatName}: {file}");
                    }

                    if (checkedCount == files.Count || checkedCount % 25 == 0)
                    {
                        var percent = files.Count == 0 ? 100 : checkedCount * 100.0 / files.Count;
                        progress?.Report($"[PROGRESO] {checkedCount}/{files.Count}|{percent:F0}");
                    }
                }

                progress?.Report($"[OK] Archivos reales revisados: {checkedCount}.");
                return results;
            }, cancellationToken);
        }

        private List<string> GetCriticalScanTargets()
        {
            var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var commonStartup = Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup);

            return new List<string>
            {
                Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                commonStartup,
                Path.Combine(userProfile, "Downloads"),
                Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                Path.GetTempPath(),
                Path.Combine(localAppData, "Temp"),
                Path.Combine(appData, "Microsoft", "Windows", "Start Menu", "Programs", "Startup")
            }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        }

        private IEnumerable<string> EnumerateFilesSafe(string root, CancellationToken cancellationToken)
        {
            var pending = new Stack<string>();
            pending.Push(root);

            while (pending.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var current = pending.Pop();

                string[] files;
                try { files = Directory.GetFiles(current); }
                catch { files = Array.Empty<string>(); }

                foreach (var file in files)
                    yield return file;

                string[] dirs;
                try { dirs = Directory.GetDirectories(current); }
                catch { dirs = Array.Empty<string>(); }

                foreach (var dir in dirs)
                    pending.Push(dir);
            }
        }

        private ThreatScanResult? AnalyzeFileHeuristically(string file)
        {
            try
            {
                var info = new FileInfo(file);
                var extension = info.Extension.ToLowerInvariant();
                var name = info.Name.ToLowerInvariant();
                var fullPath = info.FullName.ToLowerInvariant();

                var riskyExtension = extension is ".exe" or ".scr" or ".bat" or ".cmd" or ".ps1" or ".vbs" or ".js" or ".msi";
                var suspiciousName = name.Contains("crack") || name.Contains("keygen") || name.Contains("activator") ||
                                     name.Contains("patcher") || name.Contains("loader") || name.Contains("trojan") ||
                                     name.Contains("stealer") || name.Contains("miner");
                var riskyLocation = fullPath.Contains("\\temp\\") || fullPath.Contains("\\downloads\\") ||
                                    fullPath.Contains("\\startup\\");

                if (suspiciousName || (riskyExtension && riskyLocation && info.LastWriteTime > DateTime.Now.AddDays(-30)))
                {
                    return new ThreatScanResult
                    {
                        DetectionDate = DateTime.Now,
                        ThreatName = suspiciousName ? "Nombre sospechoso" : "Ejecutable reciente en ruta sensible",
                        ThreatType = "Heuristica",
                        FilePath = info.FullName,
                        Severity = suspiciousName ? "Alta" : "Media",
                        ActionTaken = "Pendiente",
                        Status = "Pendiente"
                    };
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Escanea una ruta especifica.
        /// </summary>
        public async Task<List<ThreatScanResult>> ScanPathAsync(string path, IProgress<string> progress, CancellationToken cancellationToken = default)
        {
            var results = new List<ThreatScanResult>();

            if (string.IsNullOrEmpty(_defenderPath))
            {
                progress?.Report("[ERROR] No se encontro MpCmdRun.exe. Windows Defender no esta disponible.");
                return results;
            }

            if (!Directory.Exists(path) && !File.Exists(path))
            {
                progress?.Report($"[ERROR] La ruta no existe: {path}");
                return results;
            }

            var psi = new ProcessStartInfo
            {
                FileName = _defenderPath,
                Arguments = $"-Scan -ScanType 3 -File \"{path}\" -DisableRemediation",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    progress?.Report("[ERROR] No se pudo iniciar el proceso de escaneo.");
                    return results;
                }

                var outputTask = Task.Run(async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        var line = await process.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                            progress?.Report($"[INFO] {line}");
                    }
                }, cancellationToken);

                var errorTask = Task.Run(async () =>
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        var line = await process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                            progress?.Report($"[ERROR] {line}");
                    }
                }, cancellationToken);

                await Task.WhenAll(outputTask, errorTask);
                await Task.Run(() => process.WaitForExit(), cancellationToken);

                progress?.Report($"[INFO] Escaneo completado. Codigo de salida: {process.ExitCode}");

                if (process.ExitCode == 2)
                {
                    results.Add(new ThreatScanResult
                    {
                        ThreatName = "Amenaza detectada",
                        ThreatType = "Desconocido",
                        FilePath = path,
                        Severity = "Alta",
                        ActionTaken = "Pendiente"
                    });
                }
            }
            catch (Exception ex)
            {
                progress?.Report($"[ERROR] Excepcion durante escaneo: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Escaneo rapido del sistema con Windows Defender.
        /// </summary>
        public async Task<List<ThreatScanResult>> ScanSystemAsync(IProgress<string> progress, CancellationToken cancellationToken = default)
        {
            var results = new List<ThreatScanResult>();

            if (string.IsNullOrEmpty(_defenderPath))
            {
                progress?.Report("[ERROR] Windows Defender no disponible.");
                return results;
            }

            progress?.Report("[INFO] Iniciando escaneo rapido de Windows Defender.");

            var psi = new ProcessStartInfo
            {
                FileName = _defenderPath,
                Arguments = "-Scan -ScanType 1 -DisableRemediation",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Verb = "runas"
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null) return results;

                var lines = new List<string>();
                using var cancelRegistration = cancellationToken.Register(() =>
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill(true);
                    }
                    catch { }
                });

                var outputTask = Task.Run(async () =>
                {
                    while (!process.StandardOutput.EndOfStream)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        var line = await process.StandardOutput.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lines.Add(line);
                            progress?.Report($"[OK] {line}");
                        }
                    }
                }, cancellationToken);

                var errorTask = Task.Run(async () =>
                {
                    while (!process.StandardError.EndOfStream)
                    {
                        if (cancellationToken.IsCancellationRequested) break;
                        var line = await process.StandardError.ReadLineAsync();
                        if (!string.IsNullOrWhiteSpace(line))
                            progress?.Report($"[ERROR] {line}");
                    }
                }, cancellationToken);

                await Task.Run(() => process.WaitForExit(), cancellationToken);
                await Task.WhenAll(outputTask, errorTask);

                progress?.Report($"[INFO] Escaneo finalizado. Codigo de salida: {process.ExitCode}");

                if (process.ExitCode == 2)
                {
                    var threatNames = ParseThreatsFromOutput(lines);
                    foreach (var name in threatNames)
                    {
                        results.Add(new ThreatScanResult
                        {
                            ThreatName = name,
                            ThreatType = "Desconocido",
                            Severity = "Alta",
                            Status = "Pendiente",
                            ActionTaken = "Pendiente"
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                progress?.Report("[INFO] Escaneo cancelado.");
            }
            catch (Exception ex)
            {
                progress?.Report($"[ERROR] No se pudo completar el escaneo: {ex.Message}");
            }

            return results;
        }

        private List<string> ParseThreatsFromOutput(List<string> lines)
        {
            var threats = new List<string>();
            foreach (var line in lines)
            {
                if (line.Contains("Threat:") || line.Contains("Amenaza:") || line.Contains("found:"))
                {
                    var parts = line.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 1)
                        threats.Add(string.Join(" ", parts.Skip(1).Take(3)));
                }
            }
            if (threats.Count == 0)
                threats.Add("Amenaza no especificada");
            return threats;
        }

        /// <summary>
        /// Acciones sobre amenazas detectadas.
        /// </summary>
        public async Task<bool> RemediateThreatAsync(string action, string? path = null)
        {
            if (string.IsNullOrEmpty(_defenderPath)) return false;

            string arguments = action switch
            {
                "Eliminar" => "-RemoveDefinitions -All",
                _ => "-Scan -ScanType 1"
            };

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _defenderPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
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
    }
}
