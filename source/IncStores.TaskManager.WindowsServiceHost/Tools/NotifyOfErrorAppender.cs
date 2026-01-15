using IncStores.TaskManager.RecipeRunnerService.SignalR;
using log4net.Appender;
using log4net.Core;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IncStores.TaskManager.WindowsServiceHost.Tools
{
    public class NotifyOfErrorAppender : AppenderSkeleton
    {
        protected override void Append(LoggingEvent loggingEvent)
        {
            //--Since we can't use dependency injection with an appender,
            //--we'll borrow the service provider from the IWebHost.
            LogLevel level =
                loggingEvent.Level == Level.Info ? LogLevel.Information :
                loggingEvent.Level == Level.Debug ? LogLevel.Debug :
                loggingEvent.Level == Level.Critical ? LogLevel.Critical :
                loggingEvent.Level == Level.Trace ? LogLevel.Trace :
                loggingEvent.Level == Level.Warn ? LogLevel.Warning :
                loggingEvent.Level == Level.Error ? LogLevel.Error :
                LogLevel.None;

            var _taskManagerRecipeHub = Program.webHost.Services.GetService<IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub>>();
            _taskManagerRecipeHub.Clients.All.OnErrorLogEntryAsync(level, loggingEvent.RenderedMessage);
        }
    }
}
