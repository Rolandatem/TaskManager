using IncStores.TaskManager.Core.Enumerations;
using IncStores.TaskManager.DataLayer.DTOs.IncStores;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipeQueue;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipeQueue
{
    public interface IUpsertTaskRecipeRequestViewModel
    {
        #region "Properties"
        int? ID { get; }
        ObservableCollection<TaskRecipeType> RecipeTypes { get; }
        TaskRecipeType SelectedRecipeType { get; set; }
        ObservableCollection<TaskStatusType> StatusTypes { get; }
        TaskStatusType SelectedStatusType { get; set; }
        string RecipeData { get; set; }
        DateTime? StartDate { get; set; }
        #endregion

        #region "Public Methods"
        Task LoadForEditAsync(int recipeId);
        #endregion

        #region "Relay Commands"
        IAsyncCommand SaveTaskRecipeRequestCommand { get; }
        IAsyncCommand CloseCommand { get; }
        #endregion
    }

    internal class UpsertTaskRecipeRequestViewModelDesign : IUpsertTaskRecipeRequestViewModel
    {
        #region "Properties"
        public int? ID { get; } = null;
        public ObservableCollection<TaskRecipeType> RecipeTypes =>
            new ObservableCollection<TaskRecipeType>()
            {
                        new TaskRecipeType() { StringKey = "ALL" },
                        new TaskRecipeType() { StringKey = "RECIPE 1" },
                        new TaskRecipeType() { StringKey = "RECIPE 2" }
            };
        public TaskRecipeType SelectedRecipeType { get; set; } = new TaskRecipeType() { StringKey = "--SELECT--" };
        public ObservableCollection<TaskStatusType> StatusTypes =>
            new ObservableCollection<TaskStatusType>()
            {
                new TaskStatusType() { StringKey = "ALL" },
                new TaskStatusType() { StringKey = "STATUS 1" },
                new TaskStatusType() { StringKey = "STATUS 2" }
            };
        public TaskStatusType SelectedStatusType { get; set; } = new TaskStatusType() { StringKey = "--SELECT--" };
        public string RecipeData { get; set; } = "[RECIPE_DATA]";
        public DateTime? StartDate { get; set; } = DateTime.Now;
        #endregion

        #region "Public Methods"
        public Task LoadForEditAsync(int recipeId) => Task.CompletedTask;
        #endregion

        #region "Relay Commands"
        public IAsyncCommand SaveTaskRecipeRequestCommand { get; }
        public IAsyncCommand CloseCommand { get; set; }
        #endregion
    }

    internal class UpsertTaskRecipeRequestViewModel : BaseViewModel, IUpsertTaskRecipeRequestViewModel
    {
        #region "Member Variables"
        readonly ICommonInternalToolsUnitOfWork _internalTools = null;
        #endregion

        #region "Constructor"
        public UpsertTaskRecipeRequestViewModel(
            IServiceProvider serviceProvider,
            ICommonInternalToolsUnitOfWork internalTools)
            : base(serviceProvider)
        {
            _internalTools = internalTools;
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

        private ObservableCollection<TaskRecipeType> _recipeTypes = new ObservableCollection<TaskRecipeType>();
        public ObservableCollection<TaskRecipeType> RecipeTypes
        {
            get => _recipeTypes;
            set
            {
                _recipeTypes = value;
                _recipeTypes.Insert(0, new TaskRecipeType() { StringKey = "--SELECT--" });
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

        private ObservableCollection<TaskStatusType> _statusTypes = new ObservableCollection<TaskStatusType>();
        public ObservableCollection<TaskStatusType> StatusTypes
        {
            get => _statusTypes;
            set
            {
                _statusTypes = value;
                _statusTypes.Insert(0, new TaskStatusType() { StringKey = "--SELECT--" });
                RaisePropertyChanged();
            }
        }

        private TaskStatusType _selectedStatusType = null;
        public TaskStatusType SelectedStatusType
        {
            get => _selectedStatusType;
            set
            {
                _selectedStatusType = value;
                RaisePropertyChanged();
            }
        }

        private string _recipeData = String.Empty;
        public string RecipeData
        {
            get => _recipeData;
            set
            {
                _recipeData = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _startDate = null;
        public DateTime? StartDate
        {
            get => _startDate;
            set
            {
                _startDate = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Public Methods"
        public async Task LoadForEditAsync(int id)
        {
            try
            {
                //--Retrieve recipe request
                TaskRecipeQueueItem recipe = _internalTools.TaskRecipeQueueList.DbSet.Find(id);
                if (recipe == null)
                { throw new Exception($"Could not find recipe request with id: {id}."); }

                this.ID = recipe.ID;
                this.SelectedRecipeType = recipe.TaskRecipeType;
                this.SelectedStatusType = recipe.TaskStatusType;
                this.RecipeData = recipe.Data;
                this.StartDate = recipe.StartDate;
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand SaveTaskRecipeRequestCommand { get; private set; }
        public IAsyncCommand CloseCommand { get; private set; }

        private void RegisterCommands()
        {
            this.SaveTaskRecipeRequestCommand = new AsyncCommand(OnSaveTaskRecipeRequestCommandAsync, CanSave);
            this.CloseCommand = new AsyncCommand(OnCloseCommandAsync);
        }

        private async Task OnSaveTaskRecipeRequestCommandAsync()
        {
            try
            {
                if (this.ID.HasValue == false)
                {
                    //--New Request
                    await CreateTaskRecipeQueueRequestAsync();
                }
                else
                {
                    //--Update
                    //----Retrieve recipe request.
                    TaskRecipeQueueItem recipe = _internalTools.TaskRecipeQueueList.DbSet.Find(this.ID.Value);

                    //----Check if this is not in queued status. If not, prompt the user if they want to
                    //----continue with the update.
                    if (recipe.TaskStatusType == await _internalTools.TaskStatusTypes.GetTaskStatusTypeByEnumAsync(TaskStatusTypeEnum.Queued))
                    { await UpdateTaskRecipeQueueRequestAsync(recipe); }
                    else
                    {
                        await base.ShowPromptDialogAsync(
                            "This recipe request is not in Queued status so it has already run. Are you sure you want to update the request anyway?",
                            yesCallback: async () => await UpdateTaskRecipeQueueRequestAsync(recipe));
                    }
                }
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }

        private async Task OnCloseCommandAsync()
        {
            try
            {
                await base.LoadInterfaceAsync<TaskRecipeQueueMainView>();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        #endregion

        #region "Private Methods"
        private bool CanSave() =>
            this.SelectedRecipeType?.StringKey != "--SELECT--" &&
            this.SelectedStatusType?.StringKey != "--SELECT--";
        private async Task LoadRecipeTypesAsync()
        {
            this.RecipeTypes = new ObservableCollection<TaskRecipeType>(await _internalTools.TaskRecipeTypes.DbSet
                .Where(item => item.IsActive && item.IsDeleted == false)
                .OrderBy(item => item.StringKey)
                .ToListAsync());
            this.SelectedRecipeType = this.RecipeTypes.First();
        }
        private async Task LoadStatusTypesAsync()
        {
            this.StatusTypes = new ObservableCollection<TaskStatusType>(await _internalTools.TaskStatusTypes.DbSet
                .Where(item => item.IsActive && item.IsDeleted == false)
                .ToListAsync());
            this.SelectedStatusType = this.StatusTypes.First(item => item.StringKey == "QUEUED");
        }
        private async Task UpdateTaskRecipeQueueRequestAsync(TaskRecipeQueueItem recipe)
        {
            recipe.TaskRecipeTypeId = this.SelectedRecipeType.ID;
            recipe.TaskStatusTypeId = this.SelectedStatusType.ID;
            recipe.Data = this.RecipeData;
            recipe.StartDate = this.StartDate;
            await _internalTools.CompleteAsync();

            base.SetStatus($"Task Recipe Request: {recipe.ID} updated successfully.");
            await OnCloseCommandAsync();
        }
        private async Task CreateTaskRecipeQueueRequestAsync()
        {
            if (this.SelectedRecipeType.StringKey == "--SELECT--" ||
                this.SelectedStatusType.StringKey == "--SELECT--")
            {
                await base.ShowInfoDialogAsync("Both Recipe Type and Status are required.");
                return;
            }

            TaskRecipeQueueItem newItem = new TaskRecipeQueueItem()
            {
                TaskRecipeTypeId = this.SelectedRecipeType.ID,
                TaskStatusTypeId = this.SelectedStatusType.ID,
                Data = this.RecipeData,
                StartDate = this.StartDate
            };

            _internalTools.TaskRecipeQueueList.DbSet.Add(newItem);
            await _internalTools.CompleteAsync();

            base.SetStatus($"Task Recipe Request: {newItem.ID} created successfully.");
            await OnCloseCommandAsync();
        }
        #endregion

        private async Task StartAsync()
        {
            try
            {
                base.FormIsBusy = true;
                base.SetTitle("Add Task Recipe Queue Request");
                base.SetStatus("Opened Add Task Recipe Queue Request.");
                RegisterCommands();
                await LoadRecipeTypesAsync();
                await LoadStatusTypesAsync();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
            finally { base.FormIsBusy = false; }
        }
    }
}
