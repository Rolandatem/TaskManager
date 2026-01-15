using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipeQueue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipeQueue
{
    /// <summary>
    /// Interaction logic for UpsertTaskRecipeRequestView.xaml
    /// </summary>
    public partial class UpsertTaskRecipeRequestView : UserControl
    {
        public UpsertTaskRecipeRequestView(IUpsertTaskRecipeRequestViewModel vm)
        {
            InitializeComponent();
            this.DataContext = vm;
        }
    }
}
