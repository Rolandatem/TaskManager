using IncStores.TaskManager.DataLayer.DTOs.InternalTools;
using System;
using System.Globalization;
using System.Windows.Data;

namespace IncStores.TaskManager.WpfTaskViewer.Converters
{
    public class TaskRecipeQueueCanCancelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //--value: TaskRecipeQueueItemDTO

            if (value == null) { return false; }

            TaskRecipeQueueItemDTO recipe = value as TaskRecipeQueueItemDTO;
            if (recipe.Status == "QUEUED") { return true; }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
