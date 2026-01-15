using System;

namespace IncStores.TaskManager.WpfTaskViewer.Events.Args
{
    public class IsConnectedToSignalRServerEventArgs : EventArgs
    {
        public IsConnectedToSignalRServerEventArgs() { }
        public IsConnectedToSignalRServerEventArgs(bool isConnected = false)
            : base()
        {
            this.IsConnected = isConnected;
        }

        public bool IsConnected { get; set; }
    }
}
