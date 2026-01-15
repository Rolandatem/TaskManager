using IncStores.TaskManager.DataLayer.DTOs.InternalTools;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipe;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipe
{
    public interface ITaskRecipeMainViewModel
    {
        #region "Properties"
        TaskRecipeType SelectedTaskRecipeType { get; set; }
        ObservableCollection<TaskRecipeType> TaskRecipeList { get; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand AddRecipeTypeCommand { get; }
        IAsyncCommand RefreshRecipeTypesCommand { get; }
        IAsyncCommand DeleteRecipeTypeCommand { get; }
        IAsyncCommand EditRecipeTypeCommand { get; }
        #endregion
    }

    internal class TaskRecipeMainViewModelDesign : ITaskRecipeMainViewModel
    {
        #region "Properties"
        public TaskRecipeType SelectedTaskRecipeType { get; set; }
        public ObservableCollection<TaskRecipeType> TaskRecipeList
        {
            get
            {
                return new ObservableCollection<TaskRecipeType>()
                {
                    new TaskRecipeType()
                    {
                        ID = 1,
                        StringKey = "[STRING_KEY_1]",
                        Name = "[RECIPE_1]",
                        IsActive = true,
                        CreatedBy = "[CREATED_BY_1]",
                        EmailNotificationList = "email1@gmail.com;email2@gmail.com"
                    },
                    new TaskRecipeType()
                    {
                        ID = 2,
                        StringKey = "[STRING_KEY_2]",
                        Name = "[RECIPE_2]",
                        IsActive = false,
                        CreatedBy = "[CREATED_BY_2]",
                        SMSNotificationList = new List<FaultNotificationSMS>()
                        {
                            new FaultNotificationSMS() { Name = "Person 1", PhoneNumber = "555-555-1212" },
                            new FaultNotificationSMS() { Name = "Person 2", PhoneNumber = "555-555-1234" }
                        }
                    }
                };
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand AddRecipeTypeCommand { get; }
        public IAsyncCommand RefreshRecipeTypesCommand { get; }
        public IAsyncCommand DeleteRecipeTypeCommand { get; }
        public IAsyncCommand EditRecipeTypeCommand { get; }
        #endregion
    }

    internal class TaskRecipeMainViewModel : BaseViewModel, ITaskRecipeMainViewModel
    {
        #region "Member Variables"
        readonly ICommonInternalToolsUnitOfWork _internalTools = null;
        #endregion

        #region "Constructor"
        public TaskRecipeMainViewModel(
            IServiceProvider serviceProvider,
            ICommonInternalToolsUnitOfWork internalTools)
            : base(serviceProvider)
        {
            _internalTools = internalTools;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private TaskRecipeType _selectedTaskRecipeType = null;
        public TaskRecipeType SelectedTaskRecipeType
        {
            get => _selectedTaskRecipeType;
            set
            {
                _selectedTaskRecipeType = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<TaskRecipeType> _taskRecipeList = null;
        public ObservableCollection<TaskRecipeType> TaskRecipeList
        {
            get => _taskRecipeList;
            set
            {
                _taskRecipeList = value;
                base.SetStatus($"{_taskRecipeList.Count} record(s) found.");
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand AddRecipeTypeCommand { get; private set; }
        public IAsyncCommand RefreshRecipeTypesCommand { get; private set; }
        public IAsyncCommand DeleteRecipeTypeCommand { get; private set; }
        public IAsyncCommand EditRecipeTypeCommand { get; private set; }

        private void RegisterCommands()
        {
            this.AddRecipeTypeCommand = new AsyncCommand(OnAddRecipeTypeCommand);
            this.RefreshRecipeTypesCommand = new AsyncCommand(OnRefreshRecipeTypesCommand);
            this.DeleteRecipeTypeCommand = new AsyncCommand(OnDeleteRecipeTypeCommand);
            this.EditRecipeTypeCommand = new AsyncCommand(OnEditRecipeTypeCommand);
        }

        private async Task OnAddRecipeTypeCommand() => await base.LoadInterfaceAsync<UpsertRecipeTypeView>();
        private async Task OnRefreshRecipeTypesCommand()
        {
            try
            {
                base.FormIsBusy = true;
                this.TaskRecipeList = new ObservableCollection<TaskRecipeType>(
                    await _internalTools
                        .TaskRecipeTypes
                        .GetAllActive()
                        .ToListAsync());
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
        private async Task OnDeleteRecipeTypeCommand()
        {
            if (this.SelectedTaskRecipeType != null)
            {
                await base.ShowPromptDialogAsync($"Are you sure you want to delete [{this.SelectedTaskRecipeType.StringKey}]?",
                    yesCallback: async () =>
                    {
                        try
                        {
                            base.FormIsBusy = true;
                            await _internalTools.TaskRecipeTypes
                                .SoftDeleteByIdAsync(this.SelectedTaskRecipeType.ID);
                            await _internalTools.CompleteAsync();
                            await OnRefreshRecipeTypesCommand();
                            base.SetStatus($"[{this.SelectedTaskRecipeType.StringKey}] deleted successfully.");
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
        private async Task OnEditRecipeTypeCommand()
        {
            if (this.SelectedTaskRecipeType != null)
            {
                await base.LoadInterfaceAsync<UpsertRecipeTypeView>(async (vmObject) =>
                {
                    IUpsertRecipeTypeViewModel vm = vmObject as IUpsertRecipeTypeViewModel;
                    await vm.LoadForEditAsync(this.SelectedTaskRecipeType);
                });
            }
        }
        #endregion

        public async Task StartAsync()
        {
            base.SetTitle("Task Recipe Types");
            base.SetStatus("Opened Task Recipe Types.");
            RegisterCommands();
            await OnRefreshRecipeTypesCommand();
        }
    }
}
