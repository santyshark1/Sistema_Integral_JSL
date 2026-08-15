using System.Windows;
using System.Windows.Controls;
using JSL_SentinelPro.src.UI.ViewModels;

namespace JSL_SentinelPro.src.UI.Views
{
    public partial class PasswordResetConfirmView : UserControl
    {
        public PasswordResetConfirmView()
        {
            InitializeComponent();
        }

        private void NewPwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is PasswordResetConfirmViewModel vm) vm.NewPassword = ((PasswordBox)sender).Password;
        }

        private void ConfirmPwd_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is PasswordResetConfirmViewModel vm) vm.ConfirmPassword = ((PasswordBox)sender).Password;
        }
    }
}
