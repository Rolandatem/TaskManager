using IncStores.TaskManager.Core.Tools;
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
    public interface IRecipeQueueRunnerCollection : IRecipeRunnerTool
    {
        List<IRecipeQueueWorker> RecipeRunnerList { get; set; }
    }

    internal class RecipeQueueWorkerCollection : IRecipeQueueRunnerCollection
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly ILogger<RecipeQueueWorkerCollection> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly IGeneralTools _generalTools = null;
        readonly Overlord _overlord = null;
        readonly RecipeRunnerSettings _recipeRunnerSettings = null;

        int _workerNumberIncrementor = 0;
        #endregion

        #region "Constructor"
        public RecipeQueueWorkerCollection(
            IServiceProvider serviceProvider,
            ILogger<RecipeQueueWorkerCollection> logger,
            IAuditHelper auditHelper,
            IGeneralTools generalTools,
            Overlord overlord,
            IOptions<RecipeRunnerSettings> recipeRunnerSettings)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _auditHelper = auditHelper;
            _generalTools = generalTools;
            _recipeRunnerSettings = recipeRunnerSettings.Value;
            _overlord = overlord;
        }
        #endregion

        public List<IRecipeQueueWorker> RecipeRunnerList { get; set; } = new List<IRecipeQueueWorker>();

        #region "IRecipeRunnerTool"
        public CancellationTokenSource CancellationTokenSource { get; set; }
        public ManualResetEventSlim ResetFlag { get; set; }
        public async Task StartAsync()
        {
            try
            {
                this.CancellationTokenSource = new CancellationTokenSource();
                this.ResetFlag = new ManualResetEventSlim();

                await _auditHelper.AddAuditAsync("Recipe Queue Runner Collection Initialized.", "System", "SYSTEM");

                //--Spin up initial recipe runners
                for (int x = 0; x < _recipeRunnerSettings.RecipeWorkerLimit; x++)
                {
                    await AddRecipeQueueRunnerAsync();
                }
            }
            catch (Exception ex)
            {
                await _generalTools.WritePhysicalFileExceptionAsync(ex, "Recipe Queue Worker Collection");
                await _generalTools.SendSystemWatcherSMSMessageAsync($"The Recipe Queue Worker Collection could not start up and shut down the application. ERROR: {ex.Message}");

                _logger.LogError(ex, "RecipeQueueRunnerCollection - StartAsync");
                await _auditHelper.AddAuditAsync("The Recipe Queue Runner Collection Task encountered an error and requests a shutdown of the application.", "System", "SYSTEM");
                _overlord.CancellationTokenSource.Cancel();
            }
        }
        public async Task StopAsync()
        {
            await _auditHelper.AddAuditAsync("Recipe Queue Runner Collection - Stopping Runners...", "System", "SYSTEM");

            WaitHandle[] runnerWaitHandles = RecipeRunnerList
                .Select(runner => runner.ShutdownFlag.WaitHandle)
                .ToArray();

            List<Task> _runnerStopTaskList = RecipeRunnerList
                .Select(runner => runner.StopAsync())
                .ToList();

            WaitHandle.WaitAll(runnerWaitHandles, 10000);

            //--Clear stopped recipe workers. It makes sense to just removeAll(), but mathematically(?) I think it would
            //--be appropriate to clear by Task.IsCompleted flag.
            this.RecipeRunnerList = this.RecipeRunnerList
                .Where(item => item.RunningTask.IsCompleted == false)
                .ToList();

            await _auditHelper.AddAuditAsync("Recipe Queue Runner Collection - Runners Stopped Successfully.", "System", "SYSTEM");
            this.ResetFlag.Set();
        }
        #endregion

        public async Task AddRecipeQueueRunnerAsync(string initiator = "System")
        {
            await _auditHelper.AddAuditAsync("New Recipe Worker requested...", initiator, "SYSTEM");
            IRecipeQueueWorker runner = _serviceProvider.GetService<IRecipeQueueWorker>();
            runner.WorkerNumber = ++_workerNumberIncrementor;
            RecipeRunnerList.Add(runner);
            //_recipeRunnerTaskContainer.Add(runner.StartAsync());
            runner.RunningTask = runner.StartAsync();
            await _auditHelper.AddAuditAsync($"{runner.WorkerName} Initialized.", initiator, "SYSTEM");
        }
    }
}
