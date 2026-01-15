using IncStores.TaskManager.WpfTaskViewer.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace IncStores.TaskManager.WpfTaskViewer.Converters
{
    public class MenuItemIsEnabledMultiBindingConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            //--values[0]: The MenuItem object.
            //--values[1]: SharedHubCommunicator.IsConnectedToSignalRServer

            MenuItem menuItem = values[0] as MenuItem;
            bool isConnectedToSignalRServer = (bool)values[1];

            //--"Always on" top level menu items.
            if (menuItem.IsParent) { return true; }

            //--"always on" menu items by text
            List<string> AlwaysOnMenuTextList = new List<string>()
            { "SignalR Server" };
            if (AlwaysOnMenuTextList.Contains(menuItem.Text)) { return true; };

            //--Enabled menu items when no connection to server
            List<string> OnWhenNotConnectedTextList = new List<string>()
            { "Open Connection" };
            if (OnWhenNotConnectedTextList.Contains(menuItem.Text)) { return !isConnectedToSignalRServer; }

            //--Everything else
            return isConnectedToSignalRServer;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
