using Incstores.Common.Extensions;
using Incstores.Notification.Interfaces;
using Incstores.Notification.Models;
using IncStores.TaskManager.Core.Enumerations;
using IncStores.TaskManager.Core.Events;
using IncStores.TaskManager.Core.Recipes.Interfaces;
using IncStores.TaskManager.Core.Results;
using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IRecipeQueueWorker
    {
        int WorkerNumber { get; set; }
        string WorkerName { get; }
        ManualResetEventSlim ShutdownFlag { get; }
        Task RunningTask { get; set; }

        Task StartAsync();
        Task StopAsync();
    }

    internal class RecipeQueueWorker : IRecipeQueueWorker
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly ILogger<RecipeQueueWorker> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        readonly IGeneralTools _recipeRunnerTools = null;
        readonly IRecipeQueueCollection _recipeQueue = null;
        readonly IRecipeFactory _recipeFactory = null;
        readonly ITwilioUtil _twilioUtil = null;
        readonly ISendBasicEmail _sendBasicEmail = null;
        readonly HeartbeatMediator _heartbeatMediator = null;

        readonly CancellationTokenSource _localCancelTokenSource = new CancellationTokenSource();
        #endregion

        #region "Constructor"
        public RecipeQueueWorker(
            IServiceProvider serviceProvider,
            ILogger<RecipeQueueWorker> logger,
            IAuditHelper auditHelper,
            IGeneralTools recipeRunnerTools,
            IRecipeQueueCollection recipeQueue,
            IRecipeFactory recipeFactory,
            ITwilioUtil twilioUtil,
            ISendBasicEmail sendBasicEmail,
            HeartbeatMediator heartbeatMediator)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _auditHelper = auditHelper;
            _recipeRunnerTools = recipeRunnerTools;
            _recipeQueue = recipeQueue;
            _recipeFactory = recipeFactory;
            _twilioUtil = twilioUtil;
            _sendBasicEmail = sendBasicEmail;
            _heartbeatMediator = heartbeatMediator;
        }
        #endregion

        #region "Public Properties"
        public int WorkerNumber { get; set; }
        public string WorkerName => $"Recipe Worker #{this.WorkerNumber}";
        public ManualResetEventSlim ShutdownFlag { get; set; } = new ManualResetEventSlim();
        public Task RunningTask { get; set; }
        #endregion

        #region "Private Methods"
        private async Task SendFaultNotificationsAsync(TaskRecipeQueueItem recipeQueueItem, string faultMessage)
        {
            if (recipeQueueItem.TaskRecipeType.NotifyPersonnel)
            {
                string notificationMessage = $"Task Recipe ID: {recipeQueueItem.ID} faulted with the following message: {faultMessage}";

                //--Send SMS notifications.
                if (recipeQueueItem.TaskRecipeType.SMSNotificationList.Any())
                {
                    await _twilioUtil.BroadcastNotificationAsync(
                        recipeQueueItem.TaskRecipeType.SMSNotificationList.Select(item => item.PhoneNumber).ToList(),
                        notificationMessage);
                }

                //--Send Email notifications
                if (String.IsNullOrWhiteSpace(recipeQueueItem.TaskRecipeType.EmailNotificationList) == false)
                {
                    EmailRequest emailRequest = new EmailRequest()
                    {
                        ToList = recipeQueueItem.TaskRecipeType.EmailNotificationList,
                        From = "taskmanager@incstores.com",
                        Subject = $"Recipe Fault on {recipeQueueItem.TaskRecipeType.Name}",
                        Body = notificationMessage
                    };
                    await _sendBasicEmail.SendEmail(emailRequest);
                }
            }
        }
        #endregion

        public async Task StartAsync()
        {
            //--This try/catch is for the startup of the runner.
            try
            {
                while (_localCancelTokenSource.IsCancellationRequested == false)
                {
                    TaskRecipeQueueItem recipeQueueItem = null;
                    DateTime startTime = DateTime.Now;
                    string recipeLabel = String.Empty;

                    //--Secondary try/catch for the runner loop
                    try
                    {
                        //--Wait for a recipe to be added to the queue.
                        recipeQueueItem = await _recipeQueue.RecipeList.TakeAsync(_localCancelTokenSource.Token);
                        recipeLabel = $"{recipeQueueItem.ID}:{recipeQueueItem.TaskRecipeType.StringKey}";
                        await _auditHelper.AddAuditAsync($"Recipe [{recipeLabel}] taken from queue.", this.WorkerName, "SYSTEM");

                        //--Start Timer
                        startTime = DateTime.Now;

                        //--Create Scope
                        using IServiceScope scope = _serviceProvider.CreateScope();

                        //--Generate Recipe class from factory.
                        IRecipe recipe = await _recipeFactory.CreateRecipeFromRequestAsync(recipeQueueItem, scope);
                        _recipeQueue.QueuedRecipeIdList.TryRemove(recipeQueueItem.ID, out _);

                        //--Let recipe know which worker is doing the process for progress tracking.
                        recipe.WorkerNumber = this.WorkerNumber;
                        await _heartbeatMediator.RaiseRegisterRecipeAsync(this.WorkerNumber, recipeQueueItem.TaskRecipeType.StringKey, recipe.ID, true);

                        //--Run
                        IResult result = await recipe.RunAsync();
                        await _heartbeatMediator.RaiseRecipeProgressCompleteAsync(this.WorkerNumber);

                        if (result.IsSuccessful && result.IsValid)
                        {
                            await _recipeFactory.SetAsync(recipeQueueItem, TaskStatusTypeEnum.Completed, this.WorkerName);
                            await _auditHelper.AddAuditAsync($"Completed Recipe [{recipeLabel}] successfully.", this.WorkerName, "SYSTEM");
                        }
                        else if (result.IsSuccessful == false && result.IsValid)
                        {
                            await _recipeFactory.SetAsync(recipeQueueItem, TaskStatusTypeEnum.Faulted, this.WorkerName);
                            await _auditHelper.AddAuditAsync($"Recipe [{recipeLabel}] did not complete, the requirements were valid, but the operation was not successful.{Environment.NewLine}{result.Message}", this.WorkerName, "SYSTEM");
                            await SendFaultNotificationsAsync(recipeQueueItem, result.Message);
                        }
                        else
                        {
                            await _recipeFactory.SetAsync(recipeQueueItem, TaskStatusTypeEnum.Faulted, this.WorkerName);
                            await _auditHelper.AddAuditAsync($"Recipe [{recipeLabel}] was not successful and resulted in a faulted state.{Environment.NewLine}{result.Message}", this.WorkerName, "SYSTEM");
                            await SendFaultNotificationsAsync(recipeQueueItem, result.Message);
                        }

                        //--Notify completed regardless of result
                        await _auditHelper.AddAuditAsync($"Recipe {recipeQueueItem.TaskRecipeType.StringKey} attempt has completed in {DateTime.Now - startTime:hh\\:mm\\:ss}", this.WorkerName, "SYSTEM");
                    }
                    catch (Exception ex) when (ex is OperationCanceledException == false)
                    {
                        //--This first general exception catch is to make sure that the worker doesn't shutdown on simply
                        //--an exception occurring. Record, fail the task, and move on to the next one.
                        await _recipeFactory.SetAsync(recipeQueueItem, TaskStatusTypeEnum.Faulted, this.WorkerName);
                        _logger.LogError(ex, $"Recipe Failed id: {recipeLabel}, worker: {this.WorkerName}");
                        await _auditHelper.AddAuditAsync($"Recipe worker {this.WorkerName} had an exception occurr. Logged, set request to faulted state and is moving on to the next request.", this.WorkerName, "SYSTEM");
                        await SendFaultNotificationsAsync(recipeQueueItem, ex.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //--Notify shutdown of worker (due to cancellationtoken).
                await _auditHelper.AddAuditAsync($"{this.WorkerName} shutdown.", "System", "SYSTEM");
            }
            catch (Exception ex)
            {
                //--Catch startup error.
                _logger.LogError(ex, $"{this.WorkerName} - StartupAsync");
                await _auditHelper.AddAuditAsync($"{this.WorkerName} failed to startup:{Environment.NewLine}{ex.DetailedMessage()}", "System", "SYSTEM");
                await _recipeRunnerTools.SendSystemWatcherSMSMessageAsync($"{this.WorkerName} has shutdown.");
            }
            finally
            {
                this.ShutdownFlag.Set();
            }
        }
        public Task StopAsync()
        {
            _localCancelTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
