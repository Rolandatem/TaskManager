using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace IncStores.TaskManager.WpfTaskViewer.Converters
{
    public class LoggerNameShortenerConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //--value: {Namespace}.{ClassName}
            if (value == null) { return null; }

            return value
                .ToString()
                .Split(".").Last();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
