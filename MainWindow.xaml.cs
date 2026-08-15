using System.Windows;
using JSL_SentinelPro.src.Core;
using JSL_SentinelPro.src.UI.ViewModels;

namespace JSL_SentinelPro
{
    /// <summary>
    /// Ventana principal de la aplicacion JSL SentinelPro.
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            if (Application.Current is App { MainViewModel: not null } app)
            {
                DataContext = app.MainViewModel;
            }
            else if (App.DatabaseService != null && App.EmailService != null && App.SystemMonitorService != null)
            {
                DataContext = new MainViewModel(App.DatabaseService, App.EmailService, App.SystemMonitorService);
            }
            else
            {
                MessageBox.Show("No se pudieron inicializar los servicios principales.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown(1);
            }
        }
    }
}
