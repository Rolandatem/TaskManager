using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace IncStores.TaskManager.WpfTaskViewer.Converters
{
    public class IterationDaysShorteningConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            //--value: String list of days
            if (value == null) { return null; }

            List<string> iterationDays = value
                .ToString()
                .Split(",")
                .ToList();

            string returnString = String.Empty;
            iterationDays.ForEach(day =>
            {
                returnString += day switch
                {
                    "Monday" => "Mo,",
                    "Tuesday" => "Tu,",
                    "Wednesday" => "We,",
                    "Thursday" => "Th,",
                    "Friday" => "Fr,",
                    "Saturday" => "Sa,",
                    "Sunday" => "Su,",
                    _ => String.Empty
                };
            });

            return returnString.Length == 0 ? returnString : returnString[0..^1];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
