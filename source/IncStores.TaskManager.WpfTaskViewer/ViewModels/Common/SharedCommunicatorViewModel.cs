using IncStores.TaskManager.Core.Events.Models;
using IncStores.TaskManager.WpfTaskViewer.Events.Args;
using IncStores.TaskManager.WpfTaskViewer.Models;
using IncStores.TaskManager.WpfTaskViewer.SignalR;
using IncStores.TaskManager.WpfTaskViewer.Tools.Enumerations;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TaskStatus = IncStores.TaskManager.WpfTaskViewer.Models.TaskStatus;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Common
{
    public interface ISharedCommunicatorViewModel
    {
        #region "Events"
        event EventHandler<IsConnectedToSignalRServerEventArgs> ConnectToSignalRServerEvent;
        void OnConnectToSignalRServer(IsConnectedToSignalRServerEventArgs e);
        #endregion

        #region "Properties"
        PingStatus PingStatus { get; set; }
        DateTime? LastPingSuccess { get; }
        string SelectedFilterGroupKey { get; }
        bool ShowInformation { get; }
        bool ShowDebug { get; }
        bool ShowCritical { get; }
        bool ShowWarning { get; }
        bool ShowError { get; }
        bool IsConnectedToSignalRServer { get; set; }
        bool IsTaskManagerUnderMaintenanceWindow { get; set; }
        ObservableCollection<ErrorLogItem> ErrorLog { get; }
        ObservableCollection<ErrorLogItem> FilteredErrorLog { get; }
        ObservableCollection<AuditLogItem> AuditLog { get; }
        ObservableCollection<AuditLogItem> FilteredAuditLog { get; }
        ObservableCollection<string> AuditLogGroupKeys { get; }
        ObservableCollection<RecipeWorkerStatus> RecipeWorkerStatuses { get; }
        #endregion

        #region "Methods"
        Task InitAsync();
        Task RequestRecipeWorkersAsync();
        Task<bool> CloseConnectionAsync();
        Task<bool> ReconnectConnectionAsync();
        #endregion

        #region "Health Monitoring"
        Task RegisterRecipeAsync(HeartbeatRegisterRecipeEventArgs e);
        Task UpdateRecipeProgressAsync(HeartbeatRecipeProgressUpdateEventArgs e);
        Task RecipeProgressCompleteAsync(HeartbeatRecipeProgressCompleteEventArgs e);

        Task RegisterTaskAsync(HeartbeatRegisterTaskEventArgs e);
        Task UpdateTaskProgressAsync(HeartbeatTaskProgressUpdateEventArgs e);
        #endregion
    }

    internal class SharedCommunicatorViewModelDesign : ISharedCommunicatorViewModel
    {
        #region "Events"
        public event EventHandler<IsConnectedToSignalRServerEventArgs> ConnectToSignalRServerEvent = null;
        public void OnConnectToSignalRServer(IsConnectedToSignalRServerEventArgs e) { }
        #endregion

        #region "Properties"
        public PingStatus PingStatus { get; set; } = PingStatus.Success;
        public DateTime? LastPingSuccess { get; set; } = DateTime.Now;
        public string SelectedFilterGroupKey { get; set; }
        public bool ShowInformation { get => false; }
        public bool ShowDebug { get => false; }
        public bool ShowCritical { get => false; }
        public bool ShowWarning { get => false; }
        public bool ShowError { get => false; }
        public bool IsConnectedToSignalRServer { get; set; } = false;
        public bool IsTaskManagerUnderMaintenanceWindow { get; set; } = false;
        public ObservableCollection<ErrorLogItem> ErrorLog { get; }
        public ObservableCollection<ErrorLogItem> FilteredErrorLog => new ObservableCollection<ErrorLogItem>();
        public ObservableCollection<AuditLogItem> AuditLog { get; set; }
        public ObservableCollection<AuditLogItem> FilteredAuditLog { get; }
        public ObservableCollection<string> AuditLogGroupKeys { get; set; }
        public ObservableCollection<RecipeWorkerStatus> RecipeWorkerStatuses { get; set; }
        #endregion

        #region "Methods"
        public Task InitAsync() => Task.CompletedTask;
        public Task RequestRecipeWorkersAsync() => Task.CompletedTask;
        public Task<bool> CloseConnectionAsync() => Task.FromResult(true);
        public Task<bool> ReconnectConnectionAsync() => Task.FromResult(true);
        #endregion

        #region "Health Monitoring"
        public Task RegisterRecipeAsync(HeartbeatRegisterRecipeEventArgs e) => Task.CompletedTask;
        public Task UpdateRecipeProgressAsync(HeartbeatRecipeProgressUpdateEventArgs e) => Task.CompletedTask;
        public Task RecipeProgressCompleteAsync(HeartbeatRecipeProgressCompleteEventArgs e) => Task.CompletedTask;

        public Task RegisterTaskAsync(HeartbeatRegisterTaskEventArgs e) => Task.CompletedTask;
        public Task UpdateTaskProgressAsync(HeartbeatTaskProgressUpdateEventArgs e) => Task.CompletedTask;
        #endregion
    }

    internal class SharedCommunicatorViewModel : BaseViewModel, ISharedCommunicatorViewModel
    {
        #region "Events"
        public event EventHandler<IsConnectedToSignalRServerEventArgs> ConnectToSignalRServerEvent;
        public void OnConnectToSignalRServer(IsConnectedToSignalRServerEventArgs e)
        {
            ConnectToSignalRServerEvent?.Invoke(this, e);
        }
        #endregion

        #region "Member Variables"
        readonly List<Task> _runningTasks = new List<Task>();
        readonly ITaskManagerRecipeHubConsumer _taskManagerRecipeHubConsumer = null;
        CancellationTokenSource _pingCheckerCancellationTokenSource { get; set; }
        Task _pingCheckerTaskHolder { get; set; }
        #endregion

        #region "Constructor"
        public SharedCommunicatorViewModel(
            IServiceProvider serviceProvider,
            ITaskManagerRecipeHubConsumer taskManagerRecipeHubConsumer)
            : base(serviceProvider)
        {
            _taskManagerRecipeHubConsumer = taskManagerRecipeHubConsumer;

            //--Link consumer so that server calls can access properties of this
            //--view model.
            _taskManagerRecipeHubConsumer.SharedConsumer = this;

            this.AuditLog.CollectionChanged += (sender, e) =>
            {
                RaisePropertyChanged("AuditLogGroupKeys");
                RaisePropertyChanged("FilteredAuditLog");
            };
            this.ErrorLog.CollectionChanged += (sender, e) => RaisePropertyChanged("FilteredErrorLog");
        }
        public SharedCommunicatorViewModel() : base(null) { } //--For design purposes.
        #endregion

        #region "Form Properties"
        private PingStatus _pingStatus = PingStatus.Failed;
        public PingStatus PingStatus
        {
            get => _pingStatus;
            set
            {
                _pingStatus = value;

                if (_pingStatus == PingStatus.Success)
                { 
                    this.LastPingSuccess = DateTime.Now;

                    //--Start Ping checker since we got our first 'Success' response.
                    Task.Run(async () => await StartOrResetPingCheckerAsync());
                }

                RaisePropertyChanged();
            }
        }

        private DateTime? _lastPingSuccess = null;
        public DateTime? LastPingSuccess
        {
            get => _lastPingSuccess;
            set
            {
                _lastPingSuccess = value;
                RaisePropertyChanged();
            }
        }

        private string _selectedFilterGroupKey = "ALL";
        public string SelectedFilterGroupKey
        {
            get => _selectedFilterGroupKey;
            set
            {
                _selectedFilterGroupKey = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredAuditLog");
            }
        }

        private bool _showInformation = false;
        public bool ShowInformation
        {
            get => _showInformation;
            set
            {
                _showInformation = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredErrorLog");
            }
        }

        private bool _showDebug = false;
        public bool ShowDebug
        {
            get => _showDebug;
            set
            {
                _showDebug = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredErrorLog");
            }
        }

        private bool _showCritical = true;
        public bool ShowCritical
        {
            get => _showCritical;
            set
            {
                _showCritical = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredErrorLog");
            }
        }

        private bool _showWarning = false;
        public bool ShowWarning
        {
            get => _showWarning;
            set
            {
                _showWarning = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredErrorLog");
            }
        }

        private bool _showError = true;
        public bool ShowError
        {
            get => _showError;
            set
            {
                _showError = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredErrorLog");
            }
        }

        private bool _isConnectedToSignalRServer = false;
        public bool IsConnectedToSignalRServer
        {
            get => _isConnectedToSignalRServer;
            set
            {
                _isConnectedToSignalRServer = value;
                OnConnectToSignalRServer(new IsConnectedToSignalRServerEventArgs(_isConnectedToSignalRServer));
                RaisePropertyChanged();
            }
        }

        private bool _isTaskManagerUnderMaintenanceWindow = false;
        public bool IsTaskManagerUnderMaintenanceWindow
        {
            get => _isTaskManagerUnderMaintenanceWindow;
            set
            {
                _isTaskManagerUnderMaintenanceWindow = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<ErrorLogItem> _errorLog = new ObservableCollection<ErrorLogItem>();
        public ObservableCollection<ErrorLogItem> ErrorLog
        {
            get => _errorLog;
            set
            {
                _errorLog = value;
                if (_errorLog.Count > 200)
                {
                    _errorLog = new ObservableCollection<ErrorLogItem>(_errorLog.TakeLast(200));
                }
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredErrorLog");
            }
        }

        public ObservableCollection<ErrorLogItem> FilteredErrorLog
        {
            get
            {
                var errorLog = this.ErrorLog
                  .Where(item =>
                      (this.ShowInformation && item.LogLevel == LogLevel.Information) ||
                      (this.ShowDebug && item.LogLevel == LogLevel.Debug) ||
                      (this.ShowCritical && item.LogLevel == LogLevel.Critical) ||
                      (this.ShowWarning && item.LogLevel == LogLevel.Warning) ||
                      (this.ShowError && item.LogLevel == LogLevel.Error));

                return new ObservableCollection<ErrorLogItem>(errorLog
                    .OrderByDescending(item => item.NotifiedDate)
                    .ToList());
            }
        }

        private ObservableCollection<AuditLogItem> _auditLog = new ObservableCollection<AuditLogItem>();
        public ObservableCollection<AuditLogItem> AuditLog
        {
            get => _auditLog;
            set
            {
                _auditLog = value;
                if (_auditLog.Count > 200)
                {
                    _auditLog = new ObservableCollection<AuditLogItem>(_auditLog.TakeLast(200));
                }
                RaisePropertyChanged();
                RaisePropertyChanged("AuditLogGroupKeys");
                RaisePropertyChanged("FilteredAuditLog");
            }
        }

        public ObservableCollection<AuditLogItem> FilteredAuditLog
        {
            get
            {
                var auditLog = this.SelectedFilterGroupKey == "ALL"
                    ? this.AuditLog
                    : this.AuditLog
                        .Where(item => item.GroupKey == this.SelectedFilterGroupKey);

                return new ObservableCollection<AuditLogItem>(auditLog
                    .OrderByDescending(item => item.AuditDateTime)
                    .ToList());
            }
        }

        public ObservableCollection<string> AuditLogGroupKeys
        {
            get
            {
                ObservableCollection<string> keys = new ObservableCollection<string>(
                    this.AuditLog
                        .Select(item => item.GroupKey)
                        .Distinct()
                        .OrderBy(item => item)
                        .ToList());
                keys.Insert(0, "ALL");
                return keys;
            }
        }

        private ObservableCollection<RecipeWorkerStatus> _recipeWorkerStatuses = new ObservableCollection<RecipeWorkerStatus>();
        public ObservableCollection<RecipeWorkerStatus> RecipeWorkerStatuses
        {
            get => _recipeWorkerStatuses;
            set
            {
                _recipeWorkerStatuses = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Private Methods"
        private Task StartOrResetPingCheckerAsync()
        {
            if (_pingCheckerCancellationTokenSource != null)
            {
                //--Cancel current checker.
                _pingCheckerCancellationTokenSource.Cancel();

                //--Wait for current ping checker to end.
                while (_pingCheckerTaskHolder.IsCompleted == false) { }
            }

            //--Start ping checker.
            _pingCheckerCancellationTokenSource = new CancellationTokenSource();
            _pingCheckerTaskHolder = PingCheckerAsync();

            return Task.CompletedTask;
        }
        private async Task PingCheckerAsync()
        {
            try
            {
                await Task.Delay(30000, _pingCheckerCancellationTokenSource.Token);

                //--If we get to this point, then we haven't received a ping from the server,
                //--alert user.
                this.PingStatus = PingStatus.Failed;
                await _taskManagerRecipeHubConsumer.CloseConnectionAsync();

                //--Don't toast user if disconnect was caused by maintenance window.
                if (this.IsTaskManagerUnderMaintenanceWindow)
                { return; }

                new ToastContentBuilder()
                    .AddText("TaskViewer Alert!")
                    .AddText("The TaskViewer has not received a ping/keep-alive signal from the server. It may be down.")
                    .Show();
            }
            catch (OperationCanceledException)
            {
                //--Ignore cancellation exception
            }
        }
        #endregion

        #region "Health Monitoring"
        private Task<RecipeWorkerStatus> FindRecipeWorkerStatusAsync(int workerNumber)
        {
            RecipeWorkerStatus recipeWorkerStatus = this.RecipeWorkerStatuses
                .FirstOrDefault(item => item.WorkerNumber == workerNumber);

            if (recipeWorkerStatus == null)
            {
                recipeWorkerStatus = new RecipeWorkerStatus(workerNumber);
                this.RecipeWorkerStatuses.Add(recipeWorkerStatus);
            }

            return Task.FromResult(recipeWorkerStatus);
        }
        private Task<RecipeStatus> FindRecipeStatusAsync(int recipeId)
        {
            RecipeWorkerStatus recipeWorkerStatus = this.RecipeWorkerStatuses
                .Where(worker => 
                    worker.RecipeStatus != null &&
                    worker.RecipeStatus.RecipeId == recipeId)
                .FirstOrDefault();

            if (recipeWorkerStatus == null)
            { throw new Exception($"There is no worker processing recipe id: {recipeId}."); }

            return Task.FromResult(recipeWorkerStatus.RecipeStatus);
        }
        public async Task RegisterRecipeAsync(HeartbeatRegisterRecipeEventArgs e)
        {
            RecipeWorkerStatus recipeWorkerStatus = await FindRecipeWorkerStatusAsync(e.WorkerNumber);
            recipeWorkerStatus.RecipeStatus = new RecipeStatus(e.RecipeName, e.RecipeId, 0, e.IsIndeterminate);
        }
        public async Task UpdateRecipeProgressAsync(HeartbeatRecipeProgressUpdateEventArgs e)
        {
            RecipeWorkerStatus recipeWorkerStatus = await FindRecipeWorkerStatusAsync(e.WorkerNumber);
            await recipeWorkerStatus.UpdateRecipeProgressAsync(e);
        }
        public async Task RecipeProgressCompleteAsync(HeartbeatRecipeProgressCompleteEventArgs e)
        {
            RecipeWorkerStatus recipeWorkerStatus = await FindRecipeWorkerStatusAsync(e.WorkerNumber);
            recipeWorkerStatus.RecipeStatus = null;
        }
        
        public async Task RegisterTaskAsync(HeartbeatRegisterTaskEventArgs e)
        {
            RecipeStatus recipeStatus = await FindRecipeStatusAsync(e.RecipeId);
            recipeStatus.TaskStatuses.Add(new TaskStatus(e.TaskName, e.TaskId, 0, e.IsIndeterminate));
        }
        public async Task UpdateTaskProgressAsync(HeartbeatTaskProgressUpdateEventArgs e)
        {
            RecipeStatus recipeStatus = await FindRecipeStatusAsync(e.RecipeId);
            await recipeStatus.UpdateTaskProgressAsync(e);
        }
        #endregion

        #region "Public Methods"
        public async Task InitAsync()
        {
            await _taskManagerRecipeHubConsumer.InitAsync();
            //await RequestRecipeWorkersAsync();
        }

        public async Task RequestRecipeWorkersAsync()
        {
            List<int> recipeWorkerNumberList = await _taskManagerRecipeHubConsumer.GetRecipeWorkerNumberListAsync();
            this.RecipeWorkerStatuses.Clear();
            this.RecipeWorkerStatuses = new ObservableCollection<RecipeWorkerStatus>(recipeWorkerNumberList
                .Select(workerNumber => new RecipeWorkerStatus(workerNumber)));
        }
        public async Task<bool> CloseConnectionAsync()
        {
            return await _taskManagerRecipeHubConsumer.CloseConnectionAsync();
        }
        public async Task<bool> ReconnectConnectionAsync()
        {
            _runningTasks.RemoveAll(item => item.IsCompleted);
            bool isReconnected = await _taskManagerRecipeHubConsumer.ReconnectConnectionAsync();

            return isReconnected;
        }
        #endregion
    }
}
