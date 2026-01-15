using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.RecipeRunnerService.Tools;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService
{
    public class RecipeRunnerWindowsService : BackgroundService
    {
        #region "Member Variables"
        readonly ILogger<RecipeRunnerWindowsService> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly IGeneralTools _recipeRunnerTools = null;
        //readonly IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> _taskManagerRecipeHub = null;
        readonly Overlord _overlord = null;
        #endregion

        #region "Constructor"
        public RecipeRunnerWindowsService(
            IServiceProvider serviceProvider,
            ILogger<RecipeRunnerWindowsService> logger,
            IAuditHelper auditHelper,
            IGeneralTools recipeRunnerTools,
            //IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> taskManagerRecipeHub,
            Overlord overlord)
        {
            RecipeRunnerWindowsService.ServiceProvider = serviceProvider;
            _logger = logger;
            _auditHelper = auditHelper;
            _recipeRunnerTools = recipeRunnerTools;
            //_taskManagerRecipeHub = taskManagerRecipeHub;
            _overlord = overlord;
        }
        #endregion

        #region "Static Properties"
        public static IServiceProvider ServiceProvider { get; set; }
        #endregion

        #region "Private Methods"
        #endregion

        #region "Background Service Overrides"
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _auditHelper.AddAuditAsync("RecipeRunner Windows Service Started.", Environment.UserName, "SYSTEM");

                //--Register extra cancellation token actions
                stoppingToken.Register(async () =>
                {
                    await _auditHelper.AddAuditAsync("Windows Service shut down requested.", "System", "SYSTEM");
                    //if (_overlord.CancellationTokenSource.Token.CanBeCanceled)
                    //{
                    //    _overlord.CancellationTokenSource.Cancel();
                    //}
                });

                //--Start System tasks
                await _overlord.StartSystemToolsAsync(true);

                await _recipeRunnerTools.SendSystemWatcherSMSMessageAsync("TaskManager has started up.");
                await _overlord.WaitWhileToolsAreRunningAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RecipeRunnerWindowsService - ExecuteAsync");
                await this.StopAsync(stoppingToken);
            }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _auditHelper.AddAuditAsync("Application shut down requested.", "System", "SYSTEM");
                await _recipeRunnerTools.SendSystemWatcherSMSMessageAsync("TaskManager has started shutdown sequence.");

                _overlord.CancellationTokenSource.Cancel();

                //--Wait for all systems to shut down.
                WaitHandle.WaitAll(_overlord.AllToolResetFlags, 10000);

                await _auditHelper.AddAuditAsync("RecipeRunner Service Ended.", Environment.UserName, "SYSTEM");

                await base.StopAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RecipeRunnerWindowsService - StopAsync");
            }
        }
        #endregion
    }
}
