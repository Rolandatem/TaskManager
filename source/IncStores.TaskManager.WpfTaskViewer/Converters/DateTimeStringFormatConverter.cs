using System;
using System.Globalization;
using System.Windows.Data;

namespace IncStores.TaskManager.WpfTaskViewer.Converters
{
    public class DateTimeStringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //--value: DateTime?
            if (value == null) { return null; }

            string format = parameter == null
                ? "g"
                : parameter.ToString();

            return ((DateTime?)value).Value.ToString(format);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
