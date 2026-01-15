using Microsoft.Extensions.Logging;
using System;

namespace IncStores.TaskManager.WpfTaskViewer.Models
{
    public class ErrorLogItem
    {
        public ErrorLogItem() 
        {
            this.NotifiedDate = DateTime.Now;
        }
        public ErrorLogItem(LogLevel logLevel, string message)
            : base()
        {
            this.LogLevel = logLevel;
            this.Message = message;
        }

        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
        public DateTime NotifiedDate { get; private set; }
    }
}
