using System.Windows;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.CustomControls
{
    public class ContentSection : ContentControl
    {
        #region "Dependency Properties"
        public static readonly DependencyProperty HeaderProperty =
            DependencyProperty.Register(nameof(Header), typeof(object), typeof(ContentSection),
                new PropertyMetadata(default(object)));
        public object Header
        {
            get => (object)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty CornerRadiusProperty =
            DependencyProperty.Register(nameof(CornerRadius), typeof(CornerRadius), typeof(ContentSection),
                new PropertyMetadata(default(CornerRadius)));
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }
        #endregion
    }
}
