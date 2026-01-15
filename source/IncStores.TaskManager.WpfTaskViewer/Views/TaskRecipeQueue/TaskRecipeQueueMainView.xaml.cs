using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipeQueue;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipeQueue
{
    /// <summary>
    /// Interaction logic for TaskRecipeQueueMainView.xaml
    /// </summary>
    public partial class TaskRecipeQueueMainView : UserControl
    {
        public TaskRecipeQueueMainView(ITaskRecipeQueueMainViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
