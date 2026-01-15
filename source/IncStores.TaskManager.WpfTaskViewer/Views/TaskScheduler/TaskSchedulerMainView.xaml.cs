using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskScheduler;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.TaskScheduler
{
    /// <summary>
    /// Interaction logic for TaskSchedulerMainView.xaml
    /// </summary>
    public partial class TaskSchedulerMainView : UserControl
    {
        public TaskSchedulerMainView(ITaskSchedulerMainViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
