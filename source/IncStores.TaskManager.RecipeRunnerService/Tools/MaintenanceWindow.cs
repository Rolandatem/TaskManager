using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.RecipeRunnerService.Interfaces;
using IncStores.TaskManager.RecipeRunnerService.Models;
using IncStores.TaskManager.RecipeRunnerService.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IMaintenanceWindow : IRecipeRunnerTool { }

    internal class MaintenanceWindow : IMaintenanceWindow
    {
        #region "Member Variables"
        readonly ILogger<MaintenanceWindow> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly IGeneralTools _generalTools = null;
        readonly IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> _taskManagerRecipeHub = null;
        readonly Overlord _overlord = null;
        readonly RecipeRunnerSettings _recipeRunnerSettings = null;
        readonly MaintenanceWindowSettings _maintenanceWindowSettings = null;
        readonly string _groupKey = "Maintenance Window";
        #endregion

        #region "Constructor"
        public MaintenanceWindow(
            ILogger<MaintenanceWindow> logger,
            IAuditHelper auditHelper,
            IGeneralTools generalTools,
            IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> taskManagerRecipeHub,
            Overlord overlord,
            IOptions<RecipeRunnerSettings> recipeRunnerSettings,
            IOptions<MaintenanceWindowSettings> maintenanceWindowSettings)
        {
            _logger = logger;
            _auditHelper = auditHelper;
            _generalTools = generalTools;
            _taskManagerRecipeHub = taskManagerRecipeHub;
            _overlord = overlord;
            _recipeRunnerSettings = recipeRunnerSettings.Value;
            _maintenanceWindowSettings = maintenanceWindowSettings.Value;
        }
        #endregion

        #region "IRecipeRunnerTool"
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ManualResetEventSlim ResetFlag { get; set; }
        public async Task StartAsync()
        {
            try
            {
                this.CancellationTokenSource = new CancellationTokenSource();
                this.ResetFlag = new ManualResetEventSlim();

                if (_recipeRunnerSettings.RunMaintenanceWindow == false)
                {
                    this.CancellationTokenSource.Cancel();
                    this.ResetFlag.Set();
                    return;
                }

                await _auditHelper.AddAuditAsync("Maintenance Window Service Initialized.", _groupKey, _groupKey);

                DateTime startTime = DateTime.Parse(_maintenanceWindowSettings.StartTime);
                DateTime endTime = DateTime.Parse(_maintenanceWindowSettings.EndTime);

                while (this.CancellationTokenSource.IsCancellationRequested == false)
                {
                    //--Update timeframe if the current time is passed the end date (because the
                    //--restart already happened for today.
                    if (DateTime.Now > endTime)
                    {
                        startTime = startTime.AddDays(1);
                        endTime = endTime.AddDays(1);
                    }

                    //--Wait until we reach the start time.
                    while (DateTime.Now < startTime && _overlord.CancellationTokenSource.IsCancellationRequested == false)
                    {
                        //await Task.Yield();
                        await Task.Delay(1000, _overlord.CancellationTokenSource.Token);
                    }
                    if (_overlord.CancellationTokenSource.IsCancellationRequested) { return; }
                    await _auditHelper.AddAuditAsync("Maintenance Window started, shutting down tools.", _groupKey, _groupKey);
                    await _taskManagerRecipeHub.Clients.All.OnMaintenanceWindowStartedAsync(startTime, endTime);

                    //--We've reached the start time, lets shut down all other tools.
                    await _overlord.ShutdownSystemToolsAsync();
                    await _auditHelper.AddAuditAsync("Maintenance Window completed shutting down tools, waiting for the end time.", _groupKey, _groupKey);

                    //--Now wait for the end of the maintenance window
                    while (DateTime.Now < endTime && _overlord.CancellationTokenSource.IsCancellationRequested == false)
                    {
                        await Task.Yield();
                    }
                    if (_overlord.CancellationTokenSource.IsCancellationRequested) { return; }
                    await _auditHelper.AddAuditAsync("Maintenance Window end time reached, restarting application...", _groupKey, _groupKey);

                    //--We've reached the end of the maintenance window, time to restart tools.
                    await _overlord.StartSystemToolsAsync();

                    //--Update new start and end dates
                    startTime.AddDays(1);
                    endTime.AddDays(1);
                }
            }
            catch (OperationCanceledException)
            {
                //--Ignore cancellation token exception
            }
            catch (Exception ex)
            {
                await _generalTools.WritePhysicalFileExceptionAsync(ex, "Maintenance Window");
                await _generalTools.SendSystemWatcherSMSMessageAsync($"The Maintenance Window could not start up and shut down the application. ERROR: {ex.Message}");

                _logger.LogError(ex, "Maintenance Window - StartAsync");
                await _auditHelper.AddAuditAsync("The Maintenance Window encountered an error and requests a shutdown.", "System", "SYSTEM");
                _overlord.CancellationTokenSource.Cancel();
            }
            finally
            {
                await _auditHelper.AddAuditAsync("Maintenance Window shut down.", "System", "SYSTEM");
                this.ResetFlag.Set();
            }
        }
        public async Task StopAsync()
        {
            await Task.Yield();
            this.CancellationTokenSource.Cancel();
            this.ResetFlag.Set();
        }
        #endregion

    }
}
