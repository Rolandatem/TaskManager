using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskScheduler;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.TaskScheduler
{
    /// <summary>
    /// Interaction logic for UpsertScheduledTaskView.xaml
    /// </summary>
    public partial class UpsertScheduledTaskView : UserControl
    {
        public UpsertScheduledTaskView(IUpsertScheduledTaskViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
