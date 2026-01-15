using System;

namespace IncStores.TaskManager.WpfTaskViewer.Tools.General
{
    //-- https://johnthiriet.com/removing-async-void/

    public interface IErrorHandler
    {
        void HandleError(Exception ex);
    }
}
