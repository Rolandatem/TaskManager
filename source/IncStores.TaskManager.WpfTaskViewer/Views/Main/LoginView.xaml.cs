using IncStores.TaskManager.WpfTaskViewer.ViewModels.Main;
using System.Windows;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.Main
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
        }

        public LoginView(ILoginViewModel vm)
        {
            InitializeComponent();

            this.DataContext = vm;
            vm.UpdateUIPassword = (password) =>
            {
                passwordBox.Password = password;
            };
            vm.CheckUserSecretsForLogin();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            LoginViewModel vm = this.DataContext as LoginViewModel;
            vm.Password = (sender as PasswordBox).Password;
        }
    }
}
