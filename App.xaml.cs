using System;
using System.IO;
using System.Windows;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.UI.ViewModels; // <-- 1. Agregamos esta linea para que conozca el MainViewModel

namespace JSL_SentinelPro
{
    /// <summary>
    /// Punto de entrada principal de la aplicacion JSL SentinelPro.
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Servicio de base de datos compartido en toda la aplicacion.
        /// </summary>
        public static DatabaseService? DatabaseService { get; private set; }

        /// <summary>
        /// Servicio de email compartido en toda la aplicacion.
        /// </summary>
        public static EmailService? EmailService { get; private set; }

        /// <summary>
        /// Servicio de monitoreo del sistema.
        /// </summary>
        public static SystemMonitorService? SystemMonitorService { get; private set; }

        /// <summary>
        /// Cerebro principal de la interfaz grafica.
        /// </summary>
        public MainViewModel? MainViewModel { get; private set; } // <-- 2. Declaramos el cerebro de la ventana

        /// <summary>
        /// Evento al iniciar la aplicacion.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                string appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "JSL-SentinelPro");

                if (!Directory.Exists(appDataPath))
                    Directory.CreateDirectory(appDataPath);

                string dbPath = Path.Combine(appDataPath, "data.db");
                string configPath = Path.Combine(appDataPath, "config.json");

                DatabaseService = new DatabaseService(dbPath);
                DatabaseService.InitializeDatabaseAsync().GetAwaiter().GetResult();

                EmailService = new EmailService(configPath);

                SystemMonitorService = new SystemMonitorService(DatabaseService);
                SystemMonitorService.Start();

                // <-- 3. INICIALIZAMOS EL CEREBRO DE LA INTERFAZ -->
                MainViewModel = new MainViewModel(DatabaseService, EmailService, SystemMonitorService);

                Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Error al iniciar JSL SentinelPro: {ex.Message}",
                    "Error Critico",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
            }
        }

        /// <summary>
        /// Evento al cerrar la aplicacion.
        /// </summary>
        protected override void OnExit(ExitEventArgs e)
        {
            SystemMonitorService?.Stop();
            SystemMonitorService?.Dispose();
            DatabaseService?.Dispose();

            base.OnExit(e);
        }

        /// <summary>
        /// Maneja excepciones no controladas en el dispatcher de WPF.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogError($"DispatcherException: {e.Exception}");
            e.Handled = true;
            MessageBox.Show(
                $"Se produjo un error inesperado: {e.Exception.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        /// <summary>
        /// Maneja excepciones no controladas en el dominio de la aplicacion.
        /// </summary>
        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception ex)
            {
                LogError($"UnhandledException: {ex}");
            }
        }

        /// <summary>
        /// Registra errores en el archivo de log.
        /// </summary>
        private void LogError(string message)
        {
            try
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "JSL-SentinelPro", "logs");
                if (!Directory.Exists(logPath))
                    Directory.CreateDirectory(logPath);

                string logFile = Path.Combine(logPath, $"error_{DateTime.Now:yyyyMMdd}.log");
                File.AppendAllText(logFile, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch { }
        }
    }
}

