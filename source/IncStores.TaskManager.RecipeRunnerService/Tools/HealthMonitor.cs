using IncStores.TaskManager.Core.Events;
using IncStores.TaskManager.Core.Events.Models;
using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.RecipeRunnerService.Interfaces;
using IncStores.TaskManager.RecipeRunnerService.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IHealthMonitor : IRecipeRunnerTool { }

    internal class HealthMonitor : IHealthMonitor
    {
        #region "Member Variables"
        readonly ILogger<HealthMonitor> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> _taskManagerRecipeHub = null;
        readonly Overlord _overlord = null;
        readonly HeartbeatMediator _heartbeatMediator = null;
        #endregion

        #region "Constructor"
        public HealthMonitor(
            ILogger<HealthMonitor> logger,
            IAuditHelper auditHelper,
            IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> taskManagerRecipeHub,
            Overlord overlord,
            HeartbeatMediator heartbeatMediator)
        {
            _logger = logger;
            _auditHelper = auditHelper;
            _taskManagerRecipeHub = taskManagerRecipeHub;
            _overlord = overlord;
            _heartbeatMediator = heartbeatMediator;
        }
        #endregion

        #region "Heartbeat Monitors"
        private void RegisterHeartbeatMonitors()
        {
            _heartbeatMediator.HeartbeatRegisterRecipeEvent += async (s, e) => await OnRegisterRecipeAsync(e);
            _heartbeatMediator.HeartbeatRecipeProgressUpdateEvent += async (s, e) => await OnRecipeProgressUpdateAsync(e);
            _heartbeatMediator.HeartbeatRecipeProgressCompleteEvent += async (s, e) => await OnRecipeProgressCompleteAsync(e);

            _heartbeatMediator.HeartbeatRegisterTaskEvent += async (s, e) => await OnRegisterTaskAsync(e);
            _heartbeatMediator.HeartbeatTaskProgressUpdateEvent += async (s, e) => await OnTaskProgressUpdateAsync(e);
        }

        private async Task OnRegisterRecipeAsync(HeartbeatRegisterRecipeEventArgs e)
        {
            await _taskManagerRecipeHub.Clients.All.OnRegisterRecipeAsync(e);
        }
        private async Task OnRecipeProgressUpdateAsync(HeartbeatRecipeProgressUpdateEventArgs e)
        {
            await _taskManagerRecipeHub.Clients.All.OnRecipeProgressUpdateAsync(e);
        }
        private async Task OnRecipeProgressCompleteAsync(HeartbeatRecipeProgressCompleteEventArgs e)
        {
            await _taskManagerRecipeHub.Clients.All.OnRecipeProgressCompleteAsync(e);
        }

        private async Task OnRegisterTaskAsync(HeartbeatRegisterTaskEventArgs e)
        {
            await _taskManagerRecipeHub.Clients.All.OnRegisterTaskAsync(e);
        }
        private async Task OnTaskProgressUpdateAsync(HeartbeatTaskProgressUpdateEventArgs e)
        {
            await _taskManagerRecipeHub.Clients.All.OnTaskProgressUpdateAsync(e);
        }
        #endregion

        #region "IRecipeRunnerTool"
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ManualResetEventSlim ResetFlag { get; set; }
        public async Task StartAsync()
        {
            this.CancellationTokenSource = new CancellationTokenSource();
            this.ResetFlag = new ManualResetEventSlim();

            try
            {
                await _auditHelper.AddAuditAsync("Health Monitor Initialized.", "System", "SYSTEM");

                RegisterHeartbeatMonitors();
                await _auditHelper.AddAuditAsync("Heartbeat Monitors Registered.", "System", "SYSTEM");

                //--Unlike other tools, this doesn't really do anything other than transfer information.
                //--so just loop to keep tool alive.
                while (this.CancellationTokenSource.IsCancellationRequested == false)
                {
                    await Task.Delay(1000);
                }
            }
            catch (OperationCanceledException)
            {
                //--Ignore cancellation token exception
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health Monitor had an exception, requesting system shutdown.");
                await _auditHelper.AddAuditAsync("Health Monitor had an exception, requesting system shutdown.", "System", "SYSTEM");
                _overlord.CancellationTokenSource.Cancel();
            }
            finally
            {
                await _auditHelper.AddAuditAsync("Health Monitor shut down.", "System", "SYSTEM");
                this.ResetFlag.Set();
            }
        }
        public async Task StopAsync()
        {
            await Task.Yield();
            this.CancellationTokenSource.Cancel();
        }
        #endregion
    }
}
