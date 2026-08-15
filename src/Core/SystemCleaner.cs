using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JSL_SentinelPro.src.Models;
using Microsoft.Win32;

namespace JSL_SentinelPro.src.Core
{
    /// <summary>
    /// Limpieza del sistema.
    /// </summary>
    public class SystemCleaner
    {
        /// <summary>
        /// Obtiene items de limpieza disponibles con su tamano.
        /// </summary>
        public List<CleanupItem> GetCleanupItems()
        {
            var items = new List<CleanupItem>();

            var tempPath = Path.GetTempPath();
            var windowsTemp = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Temp");
            var recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
            var prefetchPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");

            AddCleanupItem(items, "Archivos temporales de usuario", tempPath, "Alto", "Temporales");
            AddCleanupItem(items, "Archivos temporales de Windows", windowsTemp, "Alto", "Temporales");
            AddRecentDocumentsItem(items, recentPath);
            AddCleanupItem(items, "Prefetch de Windows", prefetchPath, "Medio", "Rendimiento");

            var browsers = new[] { "Google", "Mozilla", "Microsoft", "Opera" };
            foreach (var browser in browsers)
            {
                var cachePaths = FindBrowserCache(browser);
                foreach (var cache in cachePaths)
                {
                    AddCleanupItem(items, $"Cache de {browser}", cache, "Medio", "Navegador");
                }
            }

            var recycleBin = GetRecycleBinSize();
            if (recycleBin > 0)
            {
                items.Add(new CleanupItem
                {
                    Name = "Papelera de reciclaje",
                    Path = "RecycleBin",
                    SizeBytes = recycleBin,
                    Priority = "Medio",
                    Category = "Sistema"
                });
            }

            return items;
        }

        private void AddCleanupItem(List<CleanupItem> items, string name, string path, string priority, string category)
        {
            if (Directory.Exists(path))
            {
                try
                {
                    var size = GetDirectorySize(path);
                    if (size > 1024)
                    {
                        items.Add(new CleanupItem
                        {
                            Name = name,
                            Path = path,
                            SizeBytes = size,
                            Priority = priority,
                            Category = category
                        });
                    }
                }
                catch { }
            }
        }

        private void AddRecentDocumentsItem(List<CleanupItem> items, string recentPath)
        {
            if (!Directory.Exists(recentPath))
                return;

            try
            {
                var size = GetDirectorySize(recentPath);
                var recentDownloads = GetRecentDownloadedDocuments();
                var recentDownloadSize = GetRecentDownloadedDocumentsSize();
                if (size > 1024 || recentDownloads.Any())
                {
                    items.Add(new CleanupItem
                    {
                        Name = recentDownloads.Any()
                            ? $"Documentos recientes y descargas recientes ({recentDownloads.Count} archivo(s), {FormatBytes(recentDownloadSize)})"
                            : "Documentos recientes",
                        Path = "RecentDocuments",
                        SizeBytes = size + recentDownloadSize,
                        Priority = "Bajo",
                        Category = "Privacidad"
                    });
                }
            }
            catch { }
        }

        private List<string> GetRecentDownloadedDocuments()
        {
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            if (!Directory.Exists(downloads))
                return new List<string>();

            var extensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt"
            };

            try
            {
                return Directory.GetFiles(downloads)
                    .Select(f => new FileInfo(f))
                    .Where(f => extensions.Contains(f.Extension) && f.LastWriteTime >= DateTime.Now.AddHours(-24))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Take(10)
                    .Select(f => f.FullName)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private long GetRecentDownloadedDocumentsSize()
        {
            try
            {
                return GetRecentDownloadedDocuments()
                    .Where(File.Exists)
                    .Sum(f => new FileInfo(f).Length);
            }
            catch { return 0; }
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L)
                return $"{bytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            if (bytes >= 1024L * 1024L)
                return $"{bytes / (1024.0 * 1024.0):F2} MB";
            return $"{bytes / 1024.0:F2} KB";
        }

        private List<string> FindBrowserCache(string browser)
        {
            var paths = new List<string>();
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            switch (browser)
            {
                case "Google":
                    var chrome = Path.Combine(localAppData, "Google", "Chrome", "User Data", "Default", "Cache");
                    if (Directory.Exists(chrome)) paths.Add(chrome);
                    break;
                case "Mozilla":
                    var mozilla = Path.Combine(localAppData, "Mozilla", "Firefox", "Profiles");
                    if (Directory.Exists(mozilla))
                    {
                        foreach (var profile in Directory.GetDirectories(mozilla))
                        {
                            var cache = Path.Combine(profile, "cache2");
                            if (Directory.Exists(cache)) paths.Add(cache);
                        }
                    }
                    break;
                case "Microsoft":
                    var edge = Path.Combine(localAppData, "Microsoft", "Edge", "User Data", "Default", "Cache");
                    if (Directory.Exists(edge)) paths.Add(edge);
                    break;
                case "Opera":
                    var opera = Path.Combine(localAppData, "Opera Software", "Opera Stable", "Cache");
                    if (Directory.Exists(opera)) paths.Add(opera);
                    break;
            }
            return paths;
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(file).Length; } catch { }
                }
            }
            catch { }
            return size;
        }

        private long GetRecycleBinSize()
        {
            try
            {
                long size = 0;
                foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady))
                {
                    var recyclePath = Path.Combine(drive.Name, "$Recycle.Bin");
                    if (Directory.Exists(recyclePath))
                    {
                        try { size += GetDirectorySize(recyclePath); } catch { }
                    }
                }
                return size;
            }
            catch { return 0; }
        }

        /// <summary>
        /// Limpia los items seleccionados.
        /// </summary>
        public async Task<(long FreedBytes, string Details)> CleanTempFilesAsync(List<CleanupItem> selectedItems)
        {
            long freed = 0;
            var details = new List<string>();

            foreach (var item in selectedItems.Where(i => i.IsSelected))
            {
                try
                {
                    if (item.Path == "RecycleBin")
                    {
                        freed += GetRecycleBinSize();
                        ClearRecycleBin();
                        details.Add($"Papelera vaciada");
                    }
                    else if (item.Path == "RecentDocuments")
                    {
                        var recentPath = Environment.GetFolderPath(Environment.SpecialFolder.Recent);
                        var recentDownloads = GetRecentDownloadedDocuments();
                        var before = Directory.Exists(recentPath) ? GetDirectorySize(recentPath) : 0;
                        var downloadBytes = recentDownloads.Where(File.Exists).Sum(f => new FileInfo(f).Length);
                        if (Directory.Exists(recentPath))
                            await Task.Run(() => CleanDirectory(recentPath));
                        foreach (var file in recentDownloads)
                        {
                            try { if (File.Exists(file)) File.Delete(file); } catch { }
                        }
                        var after = Directory.Exists(recentPath) ? GetDirectorySize(recentPath) : 0;
                        freed += Math.Max(0, before - after) + downloadBytes;
                        details.Add(recentDownloads.Any()
                            ? $"Historial y descargas recientes eliminadas: {string.Join(", ", recentDownloads.Select(Path.GetFileName))}. Espacio estimado: {downloadBytes / (1024.0 * 1024.0):F2} MB"
                            : "Historial de documentos recientes limpiado");
                    }
                    else if (Directory.Exists(item.Path))
                    {
                        var before = GetDirectorySize(item.Path);
                        await Task.Run(() => CleanDirectory(item.Path));
                        var after = GetDirectorySize(item.Path);
                        var itemFreed = before - after;
                        freed += itemFreed;
                        if (itemFreed > 0)
                            details.Add($"{item.Name}: {itemFreed / (1024.0 * 1024.0):F2} MB liberados");
                    }
                }
                catch (Exception ex)
                {
                    details.Add($"{item.Name}: Error - {ex.Message}");
                }
            }

            return (freed, string.Join("; ", details));
        }

        private void CleanDirectory(string path)
        {
            foreach (var file in Directory.GetFiles(path))
            {
                try { File.Delete(file); } catch { }
            }
            foreach (var dir in Directory.GetDirectories(path))
            {
                try
                {
                    Directory.Delete(dir, true);
                }
                catch
                {
                    CleanDirectory(dir);
                }
            }
        }

        private void ClearRecycleBin()
        {
            try
            {
                var script = "Clear-RecycleBin -Force -ErrorAction SilentlyContinue";
                var psi = new System.Diagnostics.ProcessStartInfo("powershell.exe", $"-Command \"{script}\"")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Verb = "runas"
                };
                using var process = System.Diagnostics.Process.Start(psi);
                process?.WaitForExit();
            }
            catch { }
        }

        /// <summary>
        /// Obtiene programas de inicio.
        /// </summary>
        public List<StartupProgram> GetStartupPrograms()
        {
            var programs = new List<StartupProgram>();
            var keys = new[]
            {
                Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run"),
                Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce"),
                Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce")
            };

            foreach (var key in keys)
            {
                if (key == null) continue;
                var keyName = key.Name;
                foreach (var valueName in key.GetValueNames())
                {
                    var value = key.GetValue(valueName)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        programs.Add(new StartupProgram
                        {
                            Name = valueName,
                            Path = value,
                            RegistryKey = keyName,
                            Impact = GetImpactLevel(value),
                            IsEnabled = true
                        });
                    }
                }
            }

            try
            {
                var taskKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run");
                if (taskKey != null)
                {
                    foreach (var name in taskKey.GetValueNames())
                    {
                        var prog = programs.FirstOrDefault(p => p.Name == name);
                        if (prog != null)
                        {
                            var val = taskKey.GetValue(name) as byte[];
                            if (val != null && val.Length > 0 && val[0] != 2)
                                prog.IsEnabled = false;
                        }
                    }
                }
            }
            catch { }

            return programs;
        }

        private string GetImpactLevel(string path)
        {
            if (path.Contains("OneDrive") || path.Contains("Dropbox") || path.Contains("chrome"))
                return "Alto";
            if (path.Contains("spotify") || path.Contains("discord"))
                return "Medio";
            return "Bajo";
        }

        /// <summary>
        /// Deshabilita programas de inicio seleccionados.
        /// </summary>
        public async Task<(int Count, string Details)> OptimizeStartupAsync(List<string> programsToDisable)
        {
            int count = 0;
            var details = new List<string>();

            foreach (var progName in programsToDisable)
            {
                try
                {
                    var runKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                    if (runKey?.GetValue(progName) != null)
                    {
                        runKey.DeleteValue(progName);
                        count++;
                        details.Add($"{progName} deshabilitado");
                    }
                    runKey?.Close();
                }
                catch (Exception ex)
                {
                    details.Add($"{progName}: Error - {ex.Message}");
                }
            }

            return await Task.FromResult((count, string.Join("; ", details)));
        }
    }
}
