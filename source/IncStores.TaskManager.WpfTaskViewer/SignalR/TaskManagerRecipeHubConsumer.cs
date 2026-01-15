using IncStores.TaskManager.Core.Events.Models;
using IncStores.TaskManager.WpfTaskViewer.Models;
using IncStores.TaskManager.WpfTaskViewer.Settings.Models;
using IncStores.TaskManager.WpfTaskViewer.Tools.Enumerations;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Common;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Main;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.SignalR
{
    public interface ITaskManagerRecipeHubConsumer
    {
        #region "Properties"
        ISharedCommunicatorViewModel SharedConsumer { get; set; }
        #endregion

        Task InitAsync();

        #region "Client Methods"
        Task<bool> CloseConnectionAsync();
        Task<bool> ReconnectConnectionAsync();
        Task<List<int>> GetRecipeWorkerNumberListAsync();
        #endregion

        #region "Server Methods"
        Task OnAuditLogEntryAsync(string message, string initiator, string groupKey, DateTime? auditDateTime);
        Task OnErrorLogEntryAsync(LogLevel logLevel, string message);
        Task OnPingClientsKeepAliveAsync();
        Task OnMaintenanceWindowStartedAsync(DateTime startTime, DateTime endTime);

        //--Heartbeat Monitors
        //Task OnRecipeProgressUpdateAsync(HeartbeatRecipeProgressUpdateEventArgs e);
        #endregion
    }

    internal class TaskManagerRecipeHubConsumer : ITaskManagerRecipeHubConsumer
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly TaskManagerSignalRService _signalRService = null;
        HubConnection _taskManagerRecipeHubConnection = null;
        #endregion

        #region "Constructor"
        public TaskManagerRecipeHubConsumer(
            IServiceProvider serviceProvider,
            IOptions<TaskManagerSignalRService> signalRService)
        {
            _serviceProvider = serviceProvider;
            _signalRService = signalRService.Value;
        }
        #endregion

        #region "Public Properties"
        public ISharedCommunicatorViewModel SharedConsumer { get; set; }
        #endregion

        #region "General Recipe Hub Server Commands"
        private Task RegisterTaskManagerRecipeHubServerCommandsAsync()
        {
            //--OnAuditLogEntryAsync
            _taskManagerRecipeHubConnection.On<string, string, string, DateTime?>("OnAuditLogEntryAsync", async (message, initiator, groupKey, auditDateTime) => await OnAuditLogEntryAsync(message, initiator, groupKey, auditDateTime));

            //--OnErrorLogEntryAsync
            _taskManagerRecipeHubConnection.On<LogLevel, string>("OnErrorLogEntryAsync", async (logLevel, message) => await OnErrorLogEntryAsync(logLevel, message));

            //--OnPingClientsKeepAliveAsync
            _taskManagerRecipeHubConnection.On("OnPingClientsKeepAliveAsync", OnPingClientsKeepAliveAsync);

            //--Closed Event
            _taskManagerRecipeHubConnection.Closed += OnTaskManagerRecipeHubClosedAsync;

            //--Reconnected Event
            _taskManagerRecipeHubConnection.Reconnected += OnTaskManagerRecipeHubReconnectedAsync;

            //--OnMaintenanceWindowStartedAsync
            _taskManagerRecipeHubConnection.On<DateTime, DateTime>("OnMaintenanceWindowStartedAsync", async (startTime, endTime) => await OnMaintenanceWindowStartedAsync(startTime, endTime));

            return Task.CompletedTask;
        }

        public Task OnAuditLogEntryAsync(string message, string initiator, string groupKey, DateTime? auditDateTime)
        {
            this.SharedConsumer.AuditLog.Add(new AuditLogItem(message, initiator, groupKey, auditDateTime));
            return Task.CompletedTask;
        }
        public Task OnErrorLogEntryAsync(LogLevel logLevel, string message)
        {
            this.SharedConsumer.ErrorLog.Add(new ErrorLogItem(logLevel, message));
            return Task.CompletedTask;
        }
        public Task OnPingClientsKeepAliveAsync()
        {
            this.SharedConsumer.PingStatus = PingStatus.Success;
            return Task.CompletedTask;
        }
        public async Task OnMaintenanceWindowStartedAsync(DateTime startTime, DateTime endTime)
        {
            IMainViewModel mainViewModel = _serviceProvider.GetService<IMainViewModel>();
            await mainViewModel.DisplayMaintenanceWindowScreenAsync(startTime, endTime);
        }
        #endregion

        #region "Heartbeat Monitor Recipe Hub Server Commands"
        private Task RegisterHeartbeatMonitorServerCommandsAsync()
        {
            //--OnRegisterRecipeAsync
            _taskManagerRecipeHubConnection.On<HeartbeatRegisterRecipeEventArgs>("OnRegisterRecipeAsync", async (e) => await OnRegisterRecipeAsync(e));

            //--OnRecipeProgressUpdateAsync
            _taskManagerRecipeHubConnection.On<HeartbeatRecipeProgressUpdateEventArgs>("OnRecipeProgressUpdateAsync", async (e) => await OnRecipeProgressUpdateAsync(e));

            //--OnRecipeProgressCompleteAsync
            _taskManagerRecipeHubConnection.On<HeartbeatRecipeProgressCompleteEventArgs>("OnRecipeProgressCompleteAsync", async (e) => await OnRecipeProgressCompleteAsync(e));

            //--OnRegisterTaskAsync
            _taskManagerRecipeHubConnection.On<HeartbeatRegisterTaskEventArgs>("OnRegisterTaskAsync", async (e) => await OnRegisterTaskAsync(e));

            //--OnTaskProgressUpdateAsync
            _taskManagerRecipeHubConnection.On<HeartbeatTaskProgressUpdateEventArgs>("OnTaskProgressUpdateAsync", async (e) => await OnTaskProgressUpdateAsync(e));

            return Task.CompletedTask;
        }

        private async Task OnRegisterRecipeAsync(HeartbeatRegisterRecipeEventArgs e)
        {
            await this.SharedConsumer.RegisterRecipeAsync(e);
        }
        private async Task OnRecipeProgressUpdateAsync(HeartbeatRecipeProgressUpdateEventArgs e)
        {
            await this.SharedConsumer.UpdateRecipeProgressAsync(e);
        }
        private async Task OnRecipeProgressCompleteAsync(HeartbeatRecipeProgressCompleteEventArgs e)
        {
            await this.SharedConsumer.RecipeProgressCompleteAsync(e);
        }
        private async Task OnRegisterTaskAsync(HeartbeatRegisterTaskEventArgs e)
        {
            await this.SharedConsumer.RegisterTaskAsync(e);
        }
        private async Task OnTaskProgressUpdateAsync(HeartbeatTaskProgressUpdateEventArgs e)
        {
            await this.SharedConsumer.UpdateTaskProgressAsync(e);
        }
        #endregion

        public async Task InitAsync()
        {
            _taskManagerRecipeHubConnection = new HubConnectionBuilder()
                .WithUrl(_signalRService.TaskManagerRecipeHubUrl)
                .WithAutomaticReconnect()
                .Build();

            await RegisterTaskManagerRecipeHubServerCommandsAsync();
            await RegisterHeartbeatMonitorServerCommandsAsync();

            //--Other task manager recipe hub settings.
            _taskManagerRecipeHubConnection.ServerTimeout = TimeSpan.FromHours(24);

            await ReconnectConnectionAsync();
        }

        #region "Client Methods"
        public async Task<bool> CloseConnectionAsync()
        {
            if (_taskManagerRecipeHubConnection.State != HubConnectionState.Disconnected)
            {
                await _taskManagerRecipeHubConnection.StopAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> ReconnectConnectionAsync()
        {
            if (_taskManagerRecipeHubConnection.State == HubConnectionState.Disconnected)
            {
                await _taskManagerRecipeHubConnection.StartAsync();
                await this.SharedConsumer.RequestRecipeWorkersAsync();
                this.SharedConsumer.IsConnectedToSignalRServer = true;
                return true;
            }

            this.SharedConsumer.IsConnectedToSignalRServer = false;
            return false;
        }

        public async Task<List<int>> GetRecipeWorkerNumberListAsync()
        {
            return await _taskManagerRecipeHubConnection.InvokeAsync<List<int>>("GetRecipeWorkerNumberList");
        }
        #endregion

        #region "Client Events"
        private Task OnTaskManagerRecipeHubClosedAsync(Exception ex)
        {
            //--TODO: log error
            if (ex == null) { }

            this.SharedConsumer.IsConnectedToSignalRServer = false;
            return Task.CompletedTask;
        }

        private Task OnTaskManagerRecipeHubReconnectedAsync(string connectionId)
        {
            this.SharedConsumer.IsConnectedToSignalRServer = true;
            return Task.CompletedTask;
        }
        #endregion
    }
}
