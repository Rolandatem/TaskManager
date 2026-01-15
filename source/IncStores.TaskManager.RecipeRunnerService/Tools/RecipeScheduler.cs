using IncStores.TaskManager.Core.Exceptions;
using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.DataLayer.Models.InternalTools.ScheduledItem;
using IncStores.TaskManager.DataLayer.Tools.Interfaces;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.RecipeRunnerService.Interfaces;
using IncStores.TaskManager.RecipeRunnerService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IRecipeScheduler : IRecipeRunnerTool { }

    internal class RecipeScheduler : IRecipeScheduler
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly ILogger<RecipeScheduler> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly IGeneralTools _generalTools = null;
        readonly Overlord _overlord = null;
        readonly RecipeRunnerSettings _recipeRunnerSettings = null;
        readonly INotify _notifyClient = null;
        readonly IScheduledItemValidator _scheduledItemValidator = null;
        #endregion

        #region "Constructor"
        public RecipeScheduler(
            IServiceProvider serviceProvider,
            ILogger<RecipeScheduler> logger,
            IAuditHelper auditHelper,
            IGeneralTools generalTools,
            Overlord overlord,
            IOptions<RecipeRunnerSettings> recipeRunnerSettings,
            INotify notifyClient,
            IScheduledItemValidator scheduledItemValidator)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _auditHelper = auditHelper;
            _generalTools = generalTools;
            _overlord = overlord;
            _recipeRunnerSettings = recipeRunnerSettings.Value;
            _notifyClient = notifyClient;
            _scheduledItemValidator = scheduledItemValidator;
        }
        #endregion

        #region "Private Methods"
        private async Task LogAndNotifyScheduleErrorAsync(string scheduleName, string message, Exception ex)
        {
            _logger.LogError(ex, message);
            await _auditHelper.AddAuditAsync($"{message}{Environment.NewLine}Please review the error logs at approximately {DateTime.Now} for more information.", "Scheduler", "Scheduler");
            await _notifyClient.OnTaskScheduledItemFailedAsync(scheduleName);
        }
        #endregion

        #region "IRecipeRunnerTool"
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ManualResetEventSlim ResetFlag { get; set; }
        public async Task StartAsync()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            using ICommonInternalToolsUnitOfWork internalTools = scope.ServiceProvider.GetService<ICommonInternalToolsUnitOfWork>();
            await _auditHelper.AddAuditAsync($"Scheduler - {internalTools.ContextId}", "System", "SYSTEM");
            this.CancellationTokenSource = new CancellationTokenSource();
            this.ResetFlag = new ManualResetEventSlim();

            if (_recipeRunnerSettings.RunScheduler == false)
            {
                this.ResetFlag.Set();
                return;
            }

            try
            {
                await _auditHelper.AddAuditAsync("Recipe Scheduler Initialized.", "System", "SYSTEM");

                while (this.CancellationTokenSource.IsCancellationRequested == false)
                {
                    List<IScheduledItem> scheduledItems = await internalTools.TaskScheduledItems.GetScheduledItemsAsync();
                    scheduledItems
                        .ForEach(async schedule =>
                        {
                            using IServiceScope innerScope = _serviceProvider.CreateScope();
                            using ICommonInternalToolsUnitOfWork innerInternalTools = innerScope.ServiceProvider.GetService<ICommonInternalToolsUnitOfWork>();

                            try
                            {
                                //--Instead of .Where(item => item.IsReady) so we can capture exceptions
                                //--from the IsReady property.
                                //if (schedule.IsReady)
                                if (_scheduledItemValidator.IsReady(schedule))
                                {
                                    await innerInternalTools.TaskRecipeQueueList
                                        .InsertTaskRecipeAsync(schedule.TaskRecipeTypeId);//, schedule.TaskRecipeData);
                                    await innerInternalTools.TaskScheduledItems
                                        .UpdateLastRanTimeAsync(schedule.ID, schedule.LastRanTime.Value);
                                }
                            }
                            catch (SchedulerConfigurationException ex)
                            {
                                await innerInternalTools.TaskScheduledItems.InActivateByIdAsync(schedule.ID, _overlord.CancellationTokenSource.Token);
                                await LogAndNotifyScheduleErrorAsync(schedule.ScheduleName, $"Scheduled Item: {schedule.ScheduleName} was configured incorrectly.", ex);
                            }
                            catch (Exception ex)
                            {
                                await innerInternalTools.TaskScheduledItems.InActivateByIdAsync(schedule.ID, _overlord.CancellationTokenSource.Token);
                                await LogAndNotifyScheduleErrorAsync(schedule.ScheduleName, $"Scheduled Item: {schedule.ScheduleName} caused an exception.", ex);
                            }
                            finally
                            {
                                await innerInternalTools.CompleteAsync();
                            }
                        });

                    await Task.Delay(_recipeRunnerSettings.SchedulerMillisecondsWaitInterval, _overlord.CancellationTokenSource.Token);
                }
            }
            catch (OperationCanceledException)
            {
                //--Ignore cancellation token exception
            }
            catch (Exception ex)
            {
                await _generalTools.WritePhysicalFileExceptionAsync(ex, "Recipe Scheduler");
                await _generalTools.SendSystemWatcherSMSMessageAsync($"The Recipe Scheduler could not start up and shut down the application. ERROR: {ex.Message}");

                _logger.LogError(ex, "RecipeScheduler - StartAsync");
                await _auditHelper.AddAuditAsync("The Recipe Scheduler encountered an error and requests a shutdown of the application.", "System", "SYSTEM");
                _overlord.CancellationTokenSource.Cancel();
            }
            finally
            {
                await _auditHelper.AddAuditAsync("Recipe Scheduler shut down.", "System", "SYSTEM");
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
