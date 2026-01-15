using IncStores.TaskManager.WpfTaskViewer.ViewModels.Main;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.Main
{
    /// <summary>
    /// Interaction logic for MaintenanceWindowUnderwayView.xaml
    /// </summary>
    public partial class MaintenanceWindowUnderwayView : UserControl
    {
        public MaintenanceWindowUnderwayView(IMaintenanceWindowUnderwayViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
