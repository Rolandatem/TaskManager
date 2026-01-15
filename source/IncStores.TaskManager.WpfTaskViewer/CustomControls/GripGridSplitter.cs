using System.Windows;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.CustomControls
{
    public class GripGridSplitter : GridSplitter
    {
        #region "Dependency Properties"
        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(GripGridSplitter),
                new PropertyMetadata(default(CornerRadius)));
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        #endregion
    }
}
