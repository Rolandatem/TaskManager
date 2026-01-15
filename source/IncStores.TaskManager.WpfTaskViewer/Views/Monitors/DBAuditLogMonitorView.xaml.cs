using IncStores.TaskManager.WpfTaskViewer.ViewModels.Monitors;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.Monitors
{
    /// <summary>
    /// Interaction logic for DBAuditLogMonitorView.xaml
    /// </summary>
    public partial class DBAuditLogMonitorView : UserControl
    {
        public DBAuditLogMonitorView(IDBAuditLogMonitorViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
