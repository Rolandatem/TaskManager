using IncStores.TaskManager.WpfTaskViewer.ViewModels.Monitors;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.Monitors
{
    /// <summary>
    /// Interaction logic for DBErrorLogMonitorView.xaml
    /// </summary>
    public partial class DBErrorLogMonitorView : UserControl
    {
        public DBErrorLogMonitorView(IDBErrorLogMonitorViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
