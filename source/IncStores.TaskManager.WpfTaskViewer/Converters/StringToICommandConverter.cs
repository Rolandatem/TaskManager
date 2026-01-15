using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows.Data;

namespace IncStores.TaskManager.WpfTaskViewer.Converters
{
    public class StringToICommandConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //--values[0]:  Command name if applicable
            //--values[1]:  ViewModel

            if (values[0] == null) { return null; }

            PropertyInfo command = values[1]
                .GetType()
                .GetProperties()
                .Where(p => p.Name.ToLower() == values[0].ToString().ToLower())
                .FirstOrDefault();

            if (command == null) { throw new ArgumentException($"Command not found in ViewModel: '{values[0]}'", nameof(values)); }
            return command.GetValue(values[1]);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
