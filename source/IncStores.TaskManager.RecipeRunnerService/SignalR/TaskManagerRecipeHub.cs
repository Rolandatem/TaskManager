using IncStores.TaskManager.Core.Events.Models;
using IncStores.TaskManager.RecipeRunnerService.Tools;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.SignalR
{
    public interface ITaskManagerRecipeHub
    {
        //--Only need to supply interface declarations for server methods
        //--and they must return a basic 'Task', no generics.
        Task OnAuditLogEntryAsync(string message, string ititiator, string groupKey = null, DateTime? auditDateTime = null);
        Task OnErrorLogEntryAsync(LogLevel logLevel, string message);
        Task OnPingClientsKeepAliveAsync();
        Task OnMaintenanceWindowStartedAsync(DateTime startTime, DateTime endTime);

        #region Heartbeat Monitors"
        Task OnRegisterRecipeAsync(HeartbeatRegisterRecipeEventArgs e);
        Task OnRecipeProgressUpdateAsync(HeartbeatRecipeProgressUpdateEventArgs e);
        Task OnRecipeProgressCompleteAsync(HeartbeatRecipeProgressCompleteEventArgs e);
        Task OnRegisterTaskAsync(HeartbeatRegisterTaskEventArgs e);
        Task OnTaskProgressUpdateAsync(HeartbeatTaskProgressUpdateEventArgs e);
        #endregion
    }

    public class TaskManagerRecipeHub : Hub<ITaskManagerRecipeHub>
    {
        #region "Overrides"
        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }
        public override Task OnDisconnectedAsync(Exception exception)
        {
            return base.OnDisconnectedAsync(exception);
        }
        #endregion

        #region "Server Methods"
        public async Task SendAuditLogEntryAsync(string message, string initiator, string groupKey = null, DateTime? auditDateTime = null)
        {
            await Clients.All.OnAuditLogEntryAsync(message, initiator, groupKey, auditDateTime);
        }
        public async Task SendErrorLogEntryAsync(LogLevel logLevel, string message)
        {
            await Clients.All.OnErrorLogEntryAsync(logLevel, message);
        }
        public async Task PingClientsKeepAliveAsync()
        {
            await Clients.All.OnPingClientsKeepAliveAsync();
        }
        #endregion

        #region "Heartbeat Monitors"
        //public async Task RecipeProgressUpdateAsync(HeartbeatRecipeProgressUpdateEventArgs e)
        //{
        //    await Clients.All.OnRecipeProgressUpdateAsync(e);
        //}
        #endregion

        #region "Client Methods"
        public Task<List<int>> GetRecipeWorkerNumberList()
        {
            IRecipeQueueRunnerCollection _workerCollection = RecipeRunnerWindowsService.ServiceProvider.GetService<IRecipeQueueRunnerCollection>();
            return Task.FromResult(_workerCollection.RecipeRunnerList
                .Select(item => item.WorkerNumber)
                .ToList());
        }
        #endregion
    }
}
