using IncStores.TaskManager.WpfTaskViewer.Tools.Enumerations;
using System;
using System.Globalization;
using System.Windows.Data;

namespace IncStores.TaskManager.WpfTaskViewer.Converters
{
    public class PingStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //--value: PingStatus
            if (value is null) { return null; }

            return (PingStatus)value switch
            {
                PingStatus.Success => "#FF009900",
                PingStatus.Requested => "#FFEE9912",
                PingStatus.Failed => "#FF990000",
                _ => null,
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
