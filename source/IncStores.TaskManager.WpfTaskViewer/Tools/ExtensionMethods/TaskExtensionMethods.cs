using IncStores.TaskManager.WpfTaskViewer.Tools.General;
using System;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.Tools.ExtensionMethods
{
    //-- https://johnthiriet.com/removing-async-void/

    public static class TaskExtensionMethods
    {
        public static async void FireAndForgetSafeAsync(this Task task, IErrorHandler handler = null)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                handler?.HandleError(ex);
            }
        }
    }
}
