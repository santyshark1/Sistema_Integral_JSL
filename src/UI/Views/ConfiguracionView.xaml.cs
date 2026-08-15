using System.Windows;
using System.Windows.Controls;
using JSL_SentinelPro.src.UI.ViewModels;

namespace JSL_SentinelPro.src.UI.Views
{
    public partial class ConfiguracionView : UserControl
    {
        public ConfiguracionView()
        {
            InitializeComponent();
        }

        private void CurrentPwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfiguracionViewModel vm) vm.CurrentPassword = ((PasswordBox)sender).Password;
        }

        private void NewPwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfiguracionViewModel vm) vm.NewPassword = ((PasswordBox)sender).Password;
        }

        private void ConfirmPwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is ConfiguracionViewModel vm) vm.ConfirmPassword = ((PasswordBox)sender).Password;
        }
    }
}
