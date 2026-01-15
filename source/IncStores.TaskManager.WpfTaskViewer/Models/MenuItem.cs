using System.Collections.ObjectModel;

namespace IncStores.TaskManager.WpfTaskViewer.Models
{
    public class MenuItem
    {
        public string Text { get; set; }
        public string Command { get; set; }
        public bool IsParent { get; set; }
        public ObservableCollection<MenuItem> Children { get; set; }
    }
}
