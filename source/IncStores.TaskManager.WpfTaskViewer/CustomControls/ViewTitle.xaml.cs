using System.Windows;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.CustomControls
{
    /// <summary>
    /// Interaction logic for ViewTitle.xaml
    /// </summary>
    public partial class ViewTitle : UserControl
    {
        public ViewTitle()
        {
            InitializeComponent();
        }

        #region "Dependency Properties"
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(ViewTitle),
                new PropertyMetadata(default(string)));
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }
        #endregion
    }
}
