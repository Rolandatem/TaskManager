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
    public interface ISignalRServerPinger : IRecipeRunnerTool { }

    internal class SignalRServerPinger : ISignalRServerPinger
    {
        #region "Member Variables"
        readonly ILogger<SignalRServerPinger> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly Overlord _overlord = null;
        readonly IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> _taskManagerRecipeHub = null;
        #endregion

        #region "Constructor"
        public SignalRServerPinger(
            ILogger<SignalRServerPinger> logger,
            IAuditHelper auditHelper,
            Overlord overlord,
            IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> taskManagerRecipeHub)
        {
            _logger = logger;
            _auditHelper = auditHelper;
            _overlord = overlord;
            _taskManagerRecipeHub = taskManagerRecipeHub;
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
                await _auditHelper.AddAuditAsync("SignalR Server Pinger Initialized.", "System", "SYSTEM");

                while (this.CancellationTokenSource.IsCancellationRequested == false)
                {
                    await _taskManagerRecipeHub.Clients.All.OnPingClientsKeepAliveAsync();

                    //--Commented out because this will severly load the DB with messages every 10 seconds.
                    //--Leaving here for testing later if necessary.
                    //await _auditHelper.AddAuditAsync("Client Keep Alive Ping Sent.", "System", "SYSTEM");
                    //_logger.LogInformation("Client Keep Alive Ping Sent.");

                    await Task.Delay(10000, this.CancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                //--Ignore cancellation token exception
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalRServerPinger had an exception, requesting system shutdown.");
                await _auditHelper.AddAuditAsync("SignalRServerPinger had an exception, requesting system shutdown.", "System", "SYSTEM");
                _overlord.CancellationTokenSource.Cancel();
            }
            finally
            {
                await _auditHelper.AddAuditAsync("SignalR Server Pinger shut down.", "System", "SYSTEM");
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
