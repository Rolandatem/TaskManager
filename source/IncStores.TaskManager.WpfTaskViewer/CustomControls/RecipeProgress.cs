using IncStores.TaskManager.WpfTaskViewer.Models;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.CustomControls
{
    public class RecipeProgress : Control
    {
        #region "Dependency Properties"
        public static readonly DependencyProperty RecipeProgressSourceProperty =
            DependencyProperty.Register(nameof(RecipeProgressSource), typeof(ObservableCollection<RecipeWorkerStatus>), typeof(RecipeProgress),
                new PropertyMetadata(default(ObservableCollection<RecipeWorkerStatus>)));
        public ObservableCollection<RecipeWorkerStatus> RecipeProgressSource
        {
            get => (ObservableCollection<RecipeWorkerStatus>)GetValue(RecipeProgressSourceProperty);
            set => SetValue(RecipeProgressSourceProperty, value);
        }
        #endregion
    }
}
