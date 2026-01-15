using IncStores.TaskManager.Core.Enumerations;
using IncStores.TaskManager.Core.Tools.Converters;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.Models.InternalTools.ScheduledItem;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskScheduler;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskScheduler
{
    public interface IUpsertScheduledTaskViewModel
    {
        #region "Properties"
        int? ID { get; set; }
        string ScheduleName { get; set; }
        ObservableCollection<TaskRecipeType> RecipeTypeList { get; set; }
        TaskRecipeType SelectedRecipeType { get; set; }
        bool MondayIsSelected { get; set; }
        bool TuesdayIsSelected { get; set; }
        bool WednesdayIsSelected { get; set; }
        bool ThursdayIsSelected { get; set; }
        bool FridayIsSelected { get; set; }
        bool SaturdayIsSelected { get; set; }
        bool SundayIsSelected { get; set; }
        ObservableCollection<string> FrequencyList { get; set; }
        string SelectedFrequency { get; set; }
        DateTime? StartTime { get; set; }
        DateTime? EndTime { get; set; }
        DateTime? LastRanTime { get; set; }
        bool IsActive { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand SaveCommand { get; }
        IAsyncCommand CloseCommand { get; }
        #endregion

        #region "Public Methods"
        Task LoadForEditAsync(TaskScheduledItem task);
        #endregion
    }

    internal class UpsertScheduledTaskViewModelDesign : IUpsertScheduledTaskViewModel
    {
        #region "Properties"
        public int? ID { get; set; } = null;
        public string ScheduleName { get; set; } = "[SCHEDULE_NAME]";
        public ObservableCollection<TaskRecipeType> RecipeTypeList { get; set; } = new ObservableCollection<TaskRecipeType>()
        {
            new TaskRecipeType() { ID = 0, StringKey = "[TASK_RECIPE_TYPE_1]" },
            new TaskRecipeType() { ID = 1, StringKey = "[TASK_RECIPE_TYPE_2]" }
        };
        public TaskRecipeType SelectedRecipeType { get => this.RecipeTypeList.First(); set { } }
        public bool MondayIsSelected { get; set; } = true;
        public bool TuesdayIsSelected { get; set; } = true;
        public bool WednesdayIsSelected { get; set; } = true;
        public bool ThursdayIsSelected { get; set; } = true;
        public bool FridayIsSelected { get; set; } = true;
        public bool SaturdayIsSelected { get; set; } = false;
        public bool SundayIsSelected { get; set; } = false;
        public ObservableCollection<string> FrequencyList { get; set; } = new ObservableCollection<string>()
        { "Hourly", "Daily" };
        public string SelectedFrequency { get => this.FrequencyList.First(); set { } }
        public DateTime? StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; } = DateTime.Now.AddDays(1);
        public DateTime? LastRanTime { get; set; } = null;
        public bool IsActive { get; set; } = true;
        #endregion

        #region "Relay Commands"
        public IAsyncCommand SaveCommand { get; }
        public IAsyncCommand CloseCommand { get; }
        #endregion

        #region "Public Methods"
        public Task LoadForEditAsync(TaskScheduledItem task) => Task.CompletedTask;
        #endregion
    }

    internal class UpsertScheduledTaskViewModel : BaseViewModel, IUpsertScheduledTaskViewModel
    {
        #region "Member Variables"
        readonly ICommonInternalToolsUnitOfWork _internalTools = null;
        readonly IStringConverters _stringConverters = null;
        #endregion

        #region "Constructor"
        public UpsertScheduledTaskViewModel(
            IServiceProvider serviceProvider,
            ICommonInternalToolsUnitOfWork internalTools,
            IStringConverters stringConverters)
            : base(serviceProvider) 
        {
            _internalTools = internalTools;
            _stringConverters = stringConverters;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private int? _id = null;
        public int? ID
        {
            get => _id;
            set
            {
                _id = value;
                RaisePropertyChanged();
            }
        }

        private string _scheduleName = String.Empty;
        public string ScheduleName
        {
            get => _scheduleName;
            set
            {
                _scheduleName = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<TaskRecipeType> _recipeTypeList = new ObservableCollection<TaskRecipeType>();
        public ObservableCollection<TaskRecipeType> RecipeTypeList
        {
            get => _recipeTypeList;
            set
            {
                _recipeTypeList = value;
                RaisePropertyChanged();
            }
        }

        private TaskRecipeType _selectedRecipeType = null;
        public TaskRecipeType SelectedRecipeType
        {
            get => _selectedRecipeType;
            set
            {
                _selectedRecipeType = value;
                RaisePropertyChanged();
            }
        }

        private bool _mondayIsSelected = false;
        public bool MondayIsSelected
        {
            get => _mondayIsSelected;
            set
            {
                _mondayIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _tuesdayIsSelected = false;
        public bool TuesdayIsSelected
        {
            get => _tuesdayIsSelected;
            set
            {
                _tuesdayIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _wednesdayIsSelected = false;
        public bool WednesdayIsSelected
        {
            get => _wednesdayIsSelected;
            set
            {
                _wednesdayIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _thursdayIsSelected = false;
        public bool ThursdayIsSelected
        {
            get => _thursdayIsSelected;
            set
            {
                _thursdayIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _fridayIsSelected = false;
        public bool FridayIsSelected
        {
            get => _fridayIsSelected;
            set
            {
                _fridayIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _saturdayIsSelected = false;
        public bool SaturdayIsSelected
        {
            get => _saturdayIsSelected;
            set
            {
                _saturdayIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private bool _sundayIsSelected = false;
        public bool SundayIsSelected
        {
            get => _sundayIsSelected;
            set
            {
                _sundayIsSelected = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<string> _frequencyList = new ObservableCollection<string>();
        public ObservableCollection<string> FrequencyList
        {
            get => _frequencyList;
            set
            {
                _frequencyList = value;
                RaisePropertyChanged();
            }
        }

        private string _selectedFrequency = null;
        public string SelectedFrequency
        {
            get => _selectedFrequency;
            set
            {
                _selectedFrequency = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _startTime = null;
        public DateTime? StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _endTime = null;
        public DateTime? EndTime
        {
            get => _endTime;
            set
            {
                _endTime = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _lastRanTime = null;
        public DateTime? LastRanTime
        {
            get => _lastRanTime;
            set
            {
                _lastRanTime = value;
                RaisePropertyChanged();
            }
        }

        private bool _isActive = true;
        public bool IsActive
        {
            get => _isActive;
            set
            {
                _isActive = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Private Methods"
        private async Task StartAsync()
        {
            try
            {
                base.FormIsBusy = true;
                base.SetTitle("New Scheduled Task");
                base.SetStatus("Add New Scheduled Task Opened.");
                RegisterCommands();
                await LoadRecipeTypesAsync();
                await LoadFrequencyTypesAsync();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
            finally
            {
                base.FormIsBusy = false;
            }
        }
        private async Task LoadRecipeTypesAsync()
        {
            this.RecipeTypeList = new ObservableCollection<TaskRecipeType>(
                await _internalTools.TaskRecipeTypes.GetActiveRecipeTypesAsync());
            this.RecipeTypeList.Insert(0, new TaskRecipeType()
            {
                ID = -1,
                StringKey = "--SELECT--"
            });
            this.SelectedRecipeType = this.RecipeTypeList.First();
        }
        private Task LoadFrequencyTypesAsync()
        {
            this.FrequencyList = new ObservableCollection<string>(
                Enum.GetNames<TaskScheduleFrequencyEnum>());
            this.FrequencyList.Insert(0, "--SELECT--");
            this.SelectedFrequency = this.FrequencyList.First();
            return Task.CompletedTask;
        }
        private bool CanSaveTaskSchedule()
        {
            bool canSave =
                String.IsNullOrWhiteSpace(this.ScheduleName) == false
                && this.SelectedRecipeType != this.RecipeTypeList.First()
                && this.SelectedFrequency != this.FrequencyList.First()
                && this.StartTime != null
                && (
                        this.MondayIsSelected ||
                        this.TuesdayIsSelected ||
                        this.WednesdayIsSelected ||
                        this.ThursdayIsSelected ||
                        this.FridayIsSelected ||
                        this.SaturdayIsSelected ||
                        this.SundayIsSelected
                   );

            return canSave;
        }
        private async Task SaveNewTaskScheduleAsync()
        {
            TaskScheduledItem scheduledItem = new TaskScheduledItem()
            {
                ScheduleName = this.ScheduleName,
                TaskRecipeTypeId = this.SelectedRecipeType.ID,
                IterationDays = await BuildIterationDaysAsync(),
                Frequency = this.SelectedFrequency,
                StartTime = this.StartTime.Value,
                EndTime = this.EndTime,
                IsActive = this.IsActive
            };

            _internalTools.TaskScheduledItems.DbSet.Add(scheduledItem);
            await _internalTools.CompleteAsync();
            this.ID = scheduledItem.ID;
            await base.ShowInfoDialogAsync("Task Schedule Saved.");
            base.SetStatus("New Scheduled Task Created.");
        }
        private async Task SaveUpdatedTaskScheduleAsync()
        {
            TaskScheduledItem scheduledTask = _internalTools
                .TaskScheduledItems.DbSet
                .FirstOrDefault(task => task.ID == this.ID.Value);
            if (scheduledTask == null) { throw new Exception($"The system could not find the scheduled task for id: {this.ID}"); }

            scheduledTask.ScheduleName = this.ScheduleName;
            scheduledTask.TaskRecipeTypeId = this.SelectedRecipeType.ID;
            scheduledTask.IterationDays = await BuildIterationDaysAsync();
            scheduledTask.Frequency = this.SelectedFrequency;
            scheduledTask.StartTime = this.StartTime.Value;
            scheduledTask.EndTime = this.EndTime;
            scheduledTask.IsActive = this.IsActive;

            await _internalTools.CompleteAsync();
            await base.ShowInfoDialogAsync("Task Schedule Saved.");
            base.SetStatus("Task Scheduled Updated.");
        }
        private Task<string> BuildIterationDaysAsync()
        {
            string returnVal = String.Empty;
            if (this.MondayIsSelected) { returnVal += "Monday,"; }
            if (this.TuesdayIsSelected) { returnVal += "Tuesday,"; }
            if (this.WednesdayIsSelected) { returnVal += "Wednesday,"; }
            if (this.ThursdayIsSelected) { returnVal += "Thursday,"; }
            if (this.FridayIsSelected) { returnVal += "Friday,"; }
            if (this.SaturdayIsSelected) { returnVal += "Saturday,"; }
            if (this.SundayIsSelected) { returnVal += "Sunday,"; }

            return Task.FromResult(returnVal.Substring(0, returnVal.Length -1));
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand SaveCommand { get; private set; }
        public IAsyncCommand CloseCommand { get; private set; }

        private void RegisterCommands()
        {
            this.SaveCommand = new AsyncCommand(OnSaveCommand, CanSaveTaskSchedule);
            this.CloseCommand = new AsyncCommand(OnCloseCommand);
        }

        private async Task OnSaveCommand()
        {
            try
            {
                base.FormIsBusy = true;
                if (this.ID == null)
                {
                    await SaveNewTaskScheduleAsync();
                }
                else
                {
                    await SaveUpdatedTaskScheduleAsync();
                }

                await OnCloseCommand();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
            finally
            {
                base.FormIsBusy = false;
            }
        }
        private async Task OnCloseCommand() => await base.LoadInterfaceAsync<TaskSchedulerMainView>();
        #endregion

        #region "Public Methods"
        public Task LoadForEditAsync(TaskScheduledItem task)
        {
            this.ID = task.ID;
            this.ScheduleName = task.ScheduleName;
            this.SelectedRecipeType = this.RecipeTypeList
                .First(recipe => recipe.ID == task.TaskRecipeTypeId);
            this.SelectedFrequency = this.FrequencyList
                .First(f => f.ToLower() == task.Frequency.ToLower());
            this.StartTime = task.StartTime;
            this.EndTime = task.EndTime;
            this.IsActive = task.IsActive;

            List<DayOfWeek> iterationDays = _stringConverters
                .ConvertDelimitedStringOfStringValuesToEnumList<DayOfWeek>(task.IterationDays, ",");
            this.MondayIsSelected = iterationDays.Contains(DayOfWeek.Monday);
            this.TuesdayIsSelected = iterationDays.Contains(DayOfWeek.Tuesday);
            this.WednesdayIsSelected = iterationDays.Contains(DayOfWeek.Wednesday);
            this.ThursdayIsSelected = iterationDays.Contains(DayOfWeek.Thursday);
            this.FridayIsSelected = iterationDays.Contains(DayOfWeek.Friday);
            this.SaturdayIsSelected = iterationDays.Contains(DayOfWeek.Saturday);
            this.SundayIsSelected = iterationDays.Contains(DayOfWeek.Sunday);

            base.SetStatus("Edit Task Schedule Opened.");

            return Task.CompletedTask;
        }
        #endregion
    }
}
