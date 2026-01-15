using IncStores.TaskManager.DataLayer.DTOs.InternalTools;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipeQueue;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipeQueue
{
    public interface ITaskRecipeQueueMainViewModel
    {
        #region "Properties"
        ObservableCollection<TaskRecipeQueueItemDTO> TaskRecipeQueueList { get; }
        ObservableCollection<TaskRecipeQueueItemDTO> FilteredTaskRecipeQueueList { get; }
        TaskRecipeQueueItemDTO SelectedRecipeQueueItem { get; set; }
        string AutoRefreshLabel { get; }

        ObservableCollection<string> RecipeTypes { get; }
        string SelectedRecipeType { get; set; }
        ObservableCollection<string> StatusTypes { get; }
        string SelectedStatusType { get; set; }

        DateTime? StartDateBegin { get; set; }
        DateTime? StartDateEnd { get; set; }
        DateTime? CreatedDateBegin { get; set; }
        DateTime? CreatedDateEnd { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand RefreshNowCommand { get; }
        IAsyncCommand AddRecipeQueueItemRequestCommand { get; }
        IAsyncCommand EditRecipeQueueItemRequestCommand { get; }
        IAsyncCommand CancelRecipeQueueItemRequestCommand { get; }
        #endregion
    }

    internal class TaskRecipeQueueMainViewModelDesign : ITaskRecipeQueueMainViewModel
    {
        #region "Properties"
        public ObservableCollection<TaskRecipeQueueItemDTO> TaskRecipeQueueList =>
            new ObservableCollection<TaskRecipeQueueItemDTO>()
            {
                new TaskRecipeQueueItemDTO() { ID = 1, RecipeType = "[RECIPE_TYPE_1]", Status = "[STATUS_TYPE_1]", CreatedBy = "[CREATED_BY_1]", CreatedDate = DateTime.Now },
                new TaskRecipeQueueItemDTO() { ID = 2, RecipeType = "[RECIPE_TYPE_2]", Status = "[STATUS_TYPE_2]", CreatedBy = "[CREATED_BY_2]", CreatedDate = DateTime.Now, RecipeData = "[SOME_STRING_DATA]", StartDate = DateTime.Now.AddDays(1) }
            };
        public ObservableCollection<TaskRecipeQueueItemDTO> FilteredTaskRecipeQueueList => this.TaskRecipeQueueList;
        public TaskRecipeQueueItemDTO SelectedRecipeQueueItem { get; set; }
        public string AutoRefreshLabel => "Refresh in 5...";
        public ObservableCollection<string> RecipeTypes => new ObservableCollection<string>() { "ALL", "[RECIPE_TYPE_1]", "[RECIPE_TYPE_2]" };
        public string SelectedRecipeType { get; set; } = "ALL";
        public ObservableCollection<string> StatusTypes => new ObservableCollection<string>() { "ALL", "[STATUS_TYPE_1]", "[STATUS_TYPE_2]" };
        public string SelectedStatusType { get; set; } = "ALL";
        public DateTime? StartDateBegin { get; set; } = DateTime.Parse("12/12/2020");
        public DateTime? StartDateEnd { get; set; } = DateTime.Parse("12/12/2020");
        public DateTime? CreatedDateBegin { get; set; } = DateTime.Parse("12/12/2020");
        public DateTime? CreatedDateEnd { get; set; } = DateTime.Parse("12/12/2020");
        #endregion

        #region "Relay Commands"
        public IAsyncCommand RefreshNowCommand { get; }
        public IAsyncCommand AddRecipeQueueItemRequestCommand { get; }
        public IAsyncCommand EditRecipeQueueItemRequestCommand { get; }
        public IAsyncCommand CancelRecipeQueueItemRequestCommand { get; }
        #endregion
    }

    internal class TaskRecipeQueueMainViewModel : BaseViewModel, ITaskRecipeQueueMainViewModel
    {
        #region "Member Variables"
        readonly ICommonInternalToolsUnitOfWork _internalTools = null;
        Task _backgroundTaskRequestorTask = null;

        CancellationTokenSource _cancelAutoRefreshTokenSource = null;
        #endregion

        #region "Constuctor"
        public TaskRecipeQueueMainViewModel(
            IServiceProvider serviceProvider,
            ICommonInternalToolsUnitOfWork internalTools)
            : base(serviceProvider)
        {
            _internalTools = internalTools;
            base.Init = StartAsync();
        }
        #endregion

        #region "Private Properties"
        private List<TaskRecipeType> taskRecipeTypes = new List<TaskRecipeType>();
        private List<TaskStatusType> taskStatusTypes = new List<TaskStatusType>();
        #endregion

        #region "Form Properties"
        private ObservableCollection<TaskRecipeQueueItemDTO> _taskRecipeQueueList = new ObservableCollection<TaskRecipeQueueItemDTO>();
        public ObservableCollection<TaskRecipeQueueItemDTO> TaskRecipeQueueList
        {
            get => _taskRecipeQueueList;
            set
            {
                _taskRecipeQueueList = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskRecipeQueueList");
            }
        }

        public ObservableCollection<TaskRecipeQueueItemDTO> FilteredTaskRecipeQueueList
        {
            get
            {
                var query = this.TaskRecipeQueueList.AsQueryable();

                if (this.SelectedRecipeType != "ALL")
                { query = query.Where(item => item.RecipeType == this.SelectedRecipeType); }

                if (this.SelectedStatusType != "ALL")
                { query = query.Where(item => item.Status == this.SelectedStatusType); }

                if (this.StartDateBegin.HasValue && this.StartDateEnd.HasValue)
                {
                    query = query.Where(item =>
                        item.StartDate.HasValue &&
                        item.StartDate.Value.Date >= this.StartDateBegin.Value.Date &&
                        item.StartDate.Value.Date <= this.StartDateEnd.Value.Date);
                }

                if (this.CreatedDateBegin.HasValue && this.CreatedDateEnd.HasValue)
                {
                    query = query.Where(item =>
                        item.CreatedDate.Date >= this.CreatedDateBegin.Value.Date &&
                        item.CreatedDate.Date <= this.CreatedDateEnd.Value.Date);
                }

                List<TaskRecipeQueueItemDTO> result = query.ToList();
                if (result.Count == this.TaskRecipeQueueList.Count)
                { base.SetStatus($"{result.Count} record(s) found."); }
                else
                { base.SetStatus($"Filtered to {result.Count} record(s) from {this.TaskRecipeQueueList.Count}."); }
                return new ObservableCollection<TaskRecipeQueueItemDTO>(result);
            }
        }

        private TaskRecipeQueueItemDTO _selectedRecipeQueueItem = null;
        public TaskRecipeQueueItemDTO SelectedRecipeQueueItem
        {
            get => _selectedRecipeQueueItem;
            set
            {
                _selectedRecipeQueueItem = value;
                RaisePropertyChanged();
            }
        }

        private string _autoRefreshLabel = String.Empty;
        public string AutoRefreshLabel
        {
            get => _autoRefreshLabel;
            set
            {
                _autoRefreshLabel = value;
                RaisePropertyChanged();
            }
        }

        public ObservableCollection<string> RecipeTypes
        {
            get
            {
                ObservableCollection<string> recipeStringKeys = new ObservableCollection<string>(
                    this.taskRecipeTypes
                        .Select(item => item.StringKey)
                        .ToList());
                recipeStringKeys.Insert(0, "ALL");

                return recipeStringKeys;
            }
        }

        private string _selectedRecipeType = "ALL";
        public string SelectedRecipeType
        {
            get => _selectedRecipeType;
            set
            {
                _selectedRecipeType = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskRecipeQueueList");
            }
        }

        public ObservableCollection<string> StatusTypes
        {
            get
            {
                ObservableCollection<string> recipeStatusKeys = new ObservableCollection<string>(
                    this.taskStatusTypes
                        .Select(item => item.StringKey)
                        .ToList());
                recipeStatusKeys.Insert(0, "ALL");

                //--Set to QUEUED by default
                if (recipeStatusKeys.Count > 1)
                { this.SelectedStatusType = recipeStatusKeys.First(status => status == "QUEUED"); }

                return recipeStatusKeys;
            }
        }

        private string _selectedStatusType = "ALL";
        public string SelectedStatusType
        {
            get => _selectedStatusType;
            set
            {
                _selectedStatusType = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskRecipeQueueList");
            }
        }

        private DateTime? _startDateBegin = null;
        public DateTime? StartDateBegin
        {
            get => _startDateBegin;
            set
            {
                _startDateBegin = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskRecipeQueueList");
            }
        }

        private DateTime? _startDateEnd = null;
        public DateTime? StartDateEnd
        {
            get => _startDateEnd;
            set
            {
                _startDateEnd = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskRecipeQueueList");
            }
        }

        private DateTime? _createdDateBegin = null;
        public DateTime? CreatedDateBegin
        {
            get => _createdDateBegin;
            set
            {
                _createdDateBegin = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskRecipeQueueList");
            }
        }

        private DateTime? _createdDateEnd = null;
        public DateTime? CreatedDateEnd
        {
            get => _createdDateEnd;
            set
            {
                _createdDateEnd = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskRecipeQueueList");
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand RefreshNowCommand { get; private set; }
        public IAsyncCommand AddRecipeQueueItemRequestCommand { get; private set; }
        public IAsyncCommand EditRecipeQueueItemRequestCommand { get; private set; }
        public IAsyncCommand CancelRecipeQueueItemRequestCommand { get; private set; }

        private void RegisterCommands()
        {
            this.RefreshNowCommand = new AsyncCommand(OnRefreshNowCommandAsync);
            this.AddRecipeQueueItemRequestCommand = new AsyncCommand(OnAddRecipeQueueItemRequestCommandAsync);
            this.EditRecipeQueueItemRequestCommand = new AsyncCommand(OnEditRecipeQueueItemRequestCommandAsync);
            this.CancelRecipeQueueItemRequestCommand = new AsyncCommand(OnCancelRecipeQueueItemRequestCommandAsync);
        }

        private async Task OnRefreshNowCommandAsync()
        {
            try
            {
                _cancelAutoRefreshTokenSource.Cancel();
                await _backgroundTaskRequestorTask;
                _backgroundTaskRequestorTask = BackgroundTaskRecipeQueueRequestorAsync();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnAddRecipeQueueItemRequestCommandAsync() => await base.LoadInterfaceAsync<UpsertTaskRecipeRequestView>();
        private async Task OnEditRecipeQueueItemRequestCommandAsync()
        {
            if (this.SelectedRecipeQueueItem != null)
            {
                await base.LoadInterfaceAsync<UpsertTaskRecipeRequestView>(async (vmObject) =>
                {
                    IUpsertTaskRecipeRequestViewModel vm = vmObject as IUpsertTaskRecipeRequestViewModel;
                    await vm.LoadForEditAsync(this.SelectedRecipeQueueItem.ID);
                });
            }
        }
        private async Task OnCancelRecipeQueueItemRequestCommandAsync()
        {
            if (this.SelectedRecipeQueueItem != null && this.SelectedRecipeQueueItem.Status == "QUEUED")
            {
                await base.ShowPromptDialogAsync($"Are you sure you want to cancel this recipe? ID: {this.SelectedRecipeQueueItem.ID}",
                    yesCallback: async () =>
                    {
                        await _internalTools.TaskRecipeQueueList
                            .CancelRecipeQueueItemRequestAsync(this.SelectedRecipeQueueItem.ID);
                        await _internalTools.CompleteAsync();
                        base.SetStatus($"Recipe Queue Request ID: {this.SelectedRecipeQueueItem.ID} canceled.");
                        await OnRefreshNowCommandAsync();
                    });
            }
        }
        #endregion

        #region "Private Methods"
        private async Task BackgroundTaskRecipeQueueRequestorAsync()
        {
            try
            {
                _cancelAutoRefreshTokenSource = new CancellationTokenSource();
                while (_cancelAutoRefreshTokenSource.IsCancellationRequested == false)
                {
                    this.FormIsBusy = true;

                    //--Task recipe type
                    TaskRecipeType searchTaskRecipeType = null;
                    if (this.SelectedRecipeType != "ALL")
                    { searchTaskRecipeType = this.taskRecipeTypes.FirstOrDefault(item => item.StringKey == this.SelectedRecipeType); }

                    //--Task Status Type
                    TaskStatusType searchTaskStatusType = null;
                    if (this.SelectedStatusType != "ALL")
                    { searchTaskStatusType = this.taskStatusTypes.FirstOrDefault(item => item.StringKey == this.SelectedStatusType); }

                    this.TaskRecipeQueueList = new ObservableCollection<TaskRecipeQueueItemDTO>(await _internalTools.TaskRecipeQueueList
                        .GetTaskRecipeQueueDataAsync(
                            searchTaskRecipeType?.ID,
                            searchTaskStatusType?.ID,
                            this.StartDateBegin,
                            this.StartDateEnd,
                            this.CreatedDateBegin,
                            this.CreatedDateEnd));
                    this.FormIsBusy = false;

                    for (int x = 20; x > 0; x--)
                    {
                        this.AutoRefreshLabel = $"Refreshing in {x}...";
                        await Task.Delay(1000, _cancelAutoRefreshTokenSource.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                //--Do nothing
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
        private async Task LoadRecipeTypesAsync()
        {
            this.taskRecipeTypes = await _internalTools.TaskRecipeTypes.GetActiveRecipeTypesAsync();
            RaisePropertyChanged("RecipeTypes");
        }
        private async Task LoadStatusTypesAsync()
        {
            this.taskStatusTypes = await _internalTools.TaskStatusTypes.DbSet
                .Where(item => item.IsActive && item.IsDeleted == false)
                .ToListAsync();
            RaisePropertyChanged("StatusTypes");
        }
        #endregion

        public async Task StartAsync()
        {
            try
            {
                base.SetTitle("Task Recipe Queue List");
                base.SetStatus("Task Recipe Queue List opened.");
                RegisterCommands();
                await LoadRecipeTypesAsync();
                await LoadStatusTypesAsync();

                _backgroundTaskRequestorTask = BackgroundTaskRecipeQueueRequestorAsync();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        public override Task StopAsync()
        {
            _cancelAutoRefreshTokenSource.Cancel();
            return Task.CompletedTask;
        }
    }
}
