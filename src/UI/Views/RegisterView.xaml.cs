using System.Windows;
using System.Windows.Controls;
using JSL_SentinelPro.src.UI.ViewModels;

namespace JSL_SentinelPro.src.UI.Views
{
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
        }

        private void RegPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm && !vm.ShowPassword)
                vm.Password = ((PasswordBox)sender).Password;
        }

        private void RegConfirm_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel vm && !vm.ShowPassword)
                vm.ConfirmPassword = ((PasswordBox)sender).Password;
        }

        private void ShowPassword_Checked(object sender, RoutedEventArgs e)
        {
            if (DataContext is not RegisterViewModel vm)
                return;

            if (vm.ShowPassword)
            {
                RegPasswordText.Text = RegPassword.Password;
                RegConfirmText.Text = RegConfirm.Password;
            }
            else
            {
                RegPassword.Password = RegPasswordText.Text;
                RegConfirm.Password = RegConfirmText.Text;
            }
        }
    }
}
