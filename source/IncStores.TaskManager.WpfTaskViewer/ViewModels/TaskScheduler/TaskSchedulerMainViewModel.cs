using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.Models.InternalTools.ScheduledItem;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskScheduler;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskScheduler
{
    public interface ITaskSchedulerMainViewModel
    {
        #region "Properties"
        TaskScheduledItem SelectedTaskSchedule { get; set; }
        ObservableCollection<TaskScheduledItem> TaskSchedule { get; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand AddScheduledTaskCommand { get; }
        IAsyncCommand RefreshScheduleCommand { get; }
        IAsyncCommand DeleteScheduledTaskCommand { get; }
        IAsyncCommand EditScheduleCommand { get; }
        #endregion
    }

    internal class TaskSchedulerMainViewModelDesign : ITaskSchedulerMainViewModel
    {
        #region "Properties"
        public TaskScheduledItem SelectedTaskSchedule { get; set; }
        public ObservableCollection<TaskScheduledItem> TaskSchedule
        {
            get
            {
                return new ObservableCollection<TaskScheduledItem>()
                {
                    new HourlyScheduledItem()
                    {
                        ScheduleName = "[SCHEDULE_NAME_1]",
                        TaskRecipeType = new TaskRecipeType()
                        {
                            ID = 1,
                            StringKey = "[RECIPE_TYPE_KEY]"
                        },
                        IterationDays = "Monday,Wednesday,Saturday,Sunday",
                        Frequency = "Hourly",
                        StartTime = DateTime.Now,
                        LastRanTime = DateTime.Now.AddHours(-1),
                        IsActive = true,
                        CreatedBy = "[CREATED_BY]"
                    }
                };
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand AddScheduledTaskCommand { get; }
        public IAsyncCommand RefreshScheduleCommand { get; }
        public IAsyncCommand DeleteScheduledTaskCommand { get; }
        public IAsyncCommand EditScheduleCommand { get; }
        #endregion
    }

    internal class TaskSchedulerMainViewModel : BaseViewModel, ITaskSchedulerMainViewModel
    {
        #region "Member Variables"
        readonly ICommonInternalToolsUnitOfWork _internalTools = null;
        #endregion

        #region "Constructor"
        public TaskSchedulerMainViewModel(
            IServiceProvider serviceProvider,
            ICommonInternalToolsUnitOfWork internalTools)
            : base(serviceProvider) 
        {
            _internalTools = internalTools;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private TaskScheduledItem _selectedTaskSchedule = null;
        public TaskScheduledItem SelectedTaskSchedule
        {
            get => _selectedTaskSchedule;
            set
            {
                _selectedTaskSchedule = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<TaskScheduledItem> _taskSchedule = null;
        public ObservableCollection<TaskScheduledItem> TaskSchedule
        {
            get => _taskSchedule;
            set
            {
                _taskSchedule = value;
                base.SetStatus($"{_taskSchedule.Count} record(s) found.");
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Private Methods"
        #endregion

        #region "Relay Commands"
        public IAsyncCommand AddScheduledTaskCommand { get; private set; }
        public IAsyncCommand RefreshScheduleCommand { get; private set; }
        public IAsyncCommand DeleteScheduledTaskCommand { get; private set; }
        public IAsyncCommand EditScheduleCommand { get; private set; }

        private void RegisterCommands()
        {
            this.AddScheduledTaskCommand = new AsyncCommand(OnAddScheduledTaskCommand);
            this.RefreshScheduleCommand = new AsyncCommand(OnRefreshScheduleCommand);
            this.DeleteScheduledTaskCommand = new AsyncCommand(OnDeleteScheduledTaskCommand);
            this.EditScheduleCommand = new AsyncCommand(OnEditScheduleCommand);
        }

        private async Task OnAddScheduledTaskCommand() => await base.LoadInterfaceAsync<UpsertScheduledTaskView>();
        private async Task OnRefreshScheduleCommand()
        {
            try
            {
                base.FormIsBusy = true;
                this.TaskSchedule = new ObservableCollection<TaskScheduledItem>(
                    await _internalTools.TaskScheduledItems.GetCurrentScheduledTasksAsync());
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
            finally
            {
                this.FormIsBusy = false;
            }
        }
        private async Task OnDeleteScheduledTaskCommand()
        {
            if (this.SelectedTaskSchedule != null)
            {
                await base.ShowPromptDialogAsync($"Are you sure you want to delete [{this.SelectedTaskSchedule.ScheduleName}]?",
                    yesCallback: async () =>
                    {
                        try
                        {
                            base.FormIsBusy = true;
                            await _internalTools.TaskScheduledItems
                                .SoftDeleteByIdAsync(this.SelectedTaskSchedule.ID);
                            await _internalTools.CompleteAsync();
                            await OnRefreshScheduleCommand();
                            base.SetStatus($"[{this.SelectedTaskSchedule.ScheduleName}] deleted successfully.");
                        }
                        catch (Exception ex)
                        {
                            await base.ShowErrorDialogAsync(ex);
                        }
                        finally
                        {
                            this.FormIsBusy = false;
                        }
                    });
            }
        }
        private async Task OnEditScheduleCommand()
        {
            if (this.SelectedTaskSchedule != null)
            {
                await base.LoadInterfaceAsync<UpsertScheduledTaskView>(async (vmObject) =>
                {
                    IUpsertScheduledTaskViewModel vm = vmObject as IUpsertScheduledTaskViewModel;
                    await vm.LoadForEditAsync(this.SelectedTaskSchedule);
                });
            }
        }
        #endregion

        public async Task StartAsync()
        {
            base.SetTitle("Scheduled Tasks");
            base.SetStatus("Opened Scheduled Tasks.");
            RegisterCommands();
            await OnRefreshScheduleCommand();
        }
    }
}
