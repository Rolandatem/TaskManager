using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipe;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipe
{
    /// <summary>
    /// Interaction logic for UpsertRecipeTypeView.xaml
    /// </summary>
    public partial class UpsertRecipeTypeView : UserControl
    {
        public UpsertRecipeTypeView(IUpsertRecipeTypeViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
