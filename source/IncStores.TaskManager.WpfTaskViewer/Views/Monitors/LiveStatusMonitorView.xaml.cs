using IncStores.TaskManager.WpfTaskViewer.ViewModels.Monitors;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.Monitors
{
    /// <summary>
    /// Interaction logic for LiveStatusMonitorView.xaml
    /// </summary>
    public partial class LiveStatusMonitorView : UserControl
    {
        public LiveStatusMonitorView(ILiveStatusMonitorViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
