using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipe;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipe
{
    /// <summary>
    /// Interaction logic for TaskRecipeMainView.xaml
    /// </summary>
    public partial class TaskRecipeMainView : UserControl
    {
        public TaskRecipeMainView(ITaskRecipeMainViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
