using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.RecipeRunnerService.Interfaces;
using IncStores.TaskManager.RecipeRunnerService.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IRecipeQueueWatcher : IRecipeRunnerTool { }

    internal class RecipeQueueWatcher : IRecipeQueueWatcher
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly ILogger<RecipeQueueWatcher> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly IGeneralTools _generalTools = null;
        readonly Overlord _overlord = null;
        readonly IRecipeQueueCollection _recipeQueue = null;
        readonly RecipeRunnerSettings _recipeRunnerSettings = null;
        #endregion

        #region "Constructor"
        public RecipeQueueWatcher(
            IServiceProvider serviceProvider,
            ILogger<RecipeQueueWatcher> logger,
            IAuditHelper auditHelper,
            IGeneralTools generalTools,
            Overlord overlord,
            IRecipeQueueCollection recipeQueue,
            IOptions<RecipeRunnerSettings> recipeRunnerSettings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _auditHelper = auditHelper;
            _generalTools = generalTools;
            _overlord = overlord;
            _recipeRunnerSettings = recipeRunnerSettings.Value;
            _recipeQueue = recipeQueue;
        }
        #endregion

        #region "IRecipeRunnerTool"
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ManualResetEventSlim ResetFlag { get; set; }
        public async Task StartAsync()
        {
            using IServiceScope scope = _serviceProvider.CreateScope();
            using ICommonInternalToolsUnitOfWork internalTools = scope.ServiceProvider.GetService<ICommonInternalToolsUnitOfWork>();
            this.CancellationTokenSource = new CancellationTokenSource();
            this.ResetFlag = new ManualResetEventSlim();

            await _auditHelper.AddAuditAsync($"Watcher - {internalTools.ContextId}", "System", "SYSTEM");

            try
            {
                await _auditHelper.AddAuditAsync("Recipe Watcher Initialized.", "System", "SYSTEM");

                while (this.CancellationTokenSource.IsCancellationRequested == false)
                {
                    try
                    {
                        List<int> queuedIdsToExclude = _recipeQueue.QueuedRecipeIdList.Keys.ToList();
                        List<TaskRecipeQueueItem> newRecipeRequests = await internalTools.TaskRecipeQueueList.GetNewQueuedItemsAsync(queuedIdsToExclude);
                        newRecipeRequests.ForEach(async recipe =>
                        {
                            await _auditHelper.AddAuditAsync($"Requested Recipe [{recipe.TaskRecipeType.Name}] found and loaded.", "Recipe Watcher", "SYSTEM");
                            _recipeQueue.QueuedRecipeIdList.TryAdd(recipe.ID, recipe.ID);
                            await _recipeQueue.RecipeList.AddAsync(recipe);
                        });

                        await Task.Delay(_recipeRunnerSettings.RecipeWatcherMillisecondsWaitInterval, this.CancellationTokenSource.Token);
                    }
                    catch (Exception ex) when (ex.Message.ToLower().Contains("shutdown is in progress"))
                    {
                        //--Currently iterated at a time when the SQL server is being reset.
                        //--Wait 5 seconds for the server to restart and try again, or the application shuts down normally.
                        await Task.Delay(5000, this.CancellationTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //--Ignore cancellation token exception
            }
            catch (Exception ex)
            {
                await _generalTools.WritePhysicalFileExceptionAsync(ex, "Recipe Queue Watcher");
                await _generalTools.SendSystemWatcherSMSMessageAsync($"The Recipe Queue Watcher could not start up and shut down the application. ERROR: {ex.Message}");

                _logger.LogError(ex, "RecipeQueueWatcher - StartAsync");
                await _auditHelper.AddAuditAsync("The Recipe Watcher Task encountered an error and requests a shutdown of the application.", "System", "SYSTEM");
                _overlord.CancellationTokenSource.Cancel();
            }
            finally
            {
                await _auditHelper.AddAuditAsync("Recipe Watcher shut down.", "System", "SYSTEM");
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
