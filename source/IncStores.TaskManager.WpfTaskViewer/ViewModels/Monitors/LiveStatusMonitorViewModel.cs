using IncStores.TaskManager.WpfTaskViewer.Models;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Common;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TaskStatus = IncStores.TaskManager.WpfTaskViewer.Models.TaskStatus;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Monitors
{
    public interface ILiveStatusMonitorViewModel
    {
        #region "Properties"
        ISharedCommunicatorViewModel SharedHubCommunicator { get; }
        #endregion
    }

    internal class LiveStatusMonitorViewModelDesign : ILiveStatusMonitorViewModel
    {
        #region "Properties"
        public ISharedCommunicatorViewModel SharedHubCommunicator
        {
            get => new SharedCommunicatorViewModel()
            {
                AuditLog = new ObservableCollection<AuditLogItem>()
                {
                    new AuditLogItem("message 1 asd asd asd asd asd asd asd asd asd asd asd asd asdasd asdas", "initiator 1", "group key 1", DateTime.Now),
                    new AuditLogItem("message 2", "initiator 2", "group key 2", DateTime.Now.AddMinutes(5))
                },
                SelectedFilterGroupKey = "ALL",
                ErrorLog = new ObservableCollection<ErrorLogItem>()
                {
                    new ErrorLogItem(LogLevel.Information, "information 1"),
                    new ErrorLogItem(LogLevel.Debug, "debug 1"),
                    new ErrorLogItem(LogLevel.Warning, "warning 1"),
                    new ErrorLogItem(LogLevel.Critical, "critical 1"),
                    new ErrorLogItem(LogLevel.Error, "error 1")
                },
                RecipeWorkerStatuses = new ObservableCollection<RecipeWorkerStatus>()
                {
                    new RecipeWorkerStatus(1)
                    {
                        RecipeStatus = new RecipeStatus() { RecipeId = 1111, Progress = 50}
                    },
                    new RecipeWorkerStatus(2)
                    {
                        RecipeStatus = new RecipeStatus()
                        {
                            RecipeName = "named recipe",
                            RecipeId = 2222,
                            IsIndeterminate = true,
                            TaskStatuses = new ObservableCollection<TaskStatus>()
                            {
                                new TaskStatus("named task", Guid.NewGuid(), 0, true),
                                new TaskStatus(Guid.NewGuid(), 0),
                                new TaskStatus(Guid.NewGuid(), 50),
                                new TaskStatus(Guid.NewGuid(), 100)
                            }
                        }
                    },
                    new RecipeWorkerStatus(3) { }
                }
            };
        }
        #endregion
    }

    internal class LiveStatusMonitorViewModel : BaseViewModel, ILiveStatusMonitorViewModel
    {
        #region "Member Variables"
        readonly ISharedCommunicatorViewModel _sharedHubCommunicator = null;
        #endregion

        #region "Constructor"
        public LiveStatusMonitorViewModel(
            IServiceProvider serviceProvider,
            ISharedCommunicatorViewModel sharedHubCommunicator)
            : base(serviceProvider)
        {
            _sharedHubCommunicator = sharedHubCommunicator;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        public ISharedCommunicatorViewModel SharedHubCommunicator => _sharedHubCommunicator;
        #endregion

        private Task StartAsync()
        {
            base.SetTitle("Live Status Monitor");
            base.SetStatus("Live Status Monitor opened.");
            return Task.CompletedTask;
        }
    }
}
