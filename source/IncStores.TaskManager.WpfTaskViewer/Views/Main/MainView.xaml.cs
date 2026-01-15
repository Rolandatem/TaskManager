using IncStores.TaskManager.WpfTaskViewer.ViewModels.Main;
using System.ComponentModel;
using System.Windows;

namespace IncStores.TaskManager.WpfTaskViewer.Views.Main
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : Window
    {
        #region "Member Variables"
        #endregion

        public MainView(IMainViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
