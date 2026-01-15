using IncStores.TaskManager.Core.Models;
using IncStores.TaskManager.DataLayer.DTOs.InternalTools;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipe;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipe
{
    public interface IUpsertRecipeTypeViewModel
    {
        #region "Properties"
        TaskRecipeType UpsertRecipeType { get; set; }
        ObservableCollection<PrimitiveWrapper<string>> EmailList { get; set; }
        ObservableCollection<FaultNotificationSMS> SMSNotificationList { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand SaveCommand { get; }
        IAsyncCommand CloseCommand { get; }
        #endregion

        #region "Public Methods"
        Task LoadForEditAsync(TaskRecipeType recipe);
        #endregion
    }

    internal class UpsertRecipeTypeViewModelDesign : IUpsertRecipeTypeViewModel
    {
        #region "Properties"
        public TaskRecipeType UpsertRecipeType { get; set; } = new TaskRecipeType()
        {
            ID = 0,
            StringKey = "[STRING_KEY]",
            Name = "[RECIPE_NAME]",
            IsActive = true,
            EmailNotificationList = "email1@gmail.com;email2@gmail.com",
            SMSNotificationList = new List<FaultNotificationSMS>()
            {
                new FaultNotificationSMS() { Name = "Person1", PhoneNumber = "555-555-1212" },
                new FaultNotificationSMS() { Name = "Person2", PhoneNumber = "555-555-1234" }
            }
        };
        public ObservableCollection<PrimitiveWrapper<string>> EmailList
        {
            get => new ObservableCollection<PrimitiveWrapper<string>>(new List<PrimitiveWrapper<string>>()
            {
                new PrimitiveWrapper<string>("steve.martinez@incstores.com"),
                new PrimitiveWrapper<string>("chris.boyce@incstores.com")
            });
            set { }
        }
        public ObservableCollection<FaultNotificationSMS> SMSNotificationList
        {
            get => new ObservableCollection<FaultNotificationSMS>(new List<FaultNotificationSMS>()
            {
                new FaultNotificationSMS() { Name = "Steve", PhoneNumber = "5555551212" },
                new FaultNotificationSMS() { Name = "Chris", PhoneNumber = "5555551000" }
            });
            set { }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand SaveCommand { get; }
        public IAsyncCommand CloseCommand { get; }
        #endregion

        #region "Public Methods"
        public Task LoadForEditAsync(TaskRecipeType recipe) => Task.CompletedTask;
        #endregion
    }

    internal class UpsertRecipeTypeViewModel : BaseViewModel, IUpsertRecipeTypeViewModel
    {
        #region "Member Variables"
        readonly ICommonInternalToolsUnitOfWork _internalTools = null;
        #endregion

        #region "Constructor"
        public UpsertRecipeTypeViewModel(
            IServiceProvider serviceProvider,
            ICommonInternalToolsUnitOfWork internalTools)
            : base(serviceProvider)
        {
            _internalTools = internalTools;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private TaskRecipeType _upsertRecipeType = new TaskRecipeType();
        public TaskRecipeType UpsertRecipeType
        {
            get => _upsertRecipeType;
            set
            {
                _upsertRecipeType = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<PrimitiveWrapper<string>> _emailList = new ObservableCollection<PrimitiveWrapper<string>>();
        public ObservableCollection<PrimitiveWrapper<string>> EmailList
        {
            get => _emailList;
            set
            {
                _emailList = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<FaultNotificationSMS> _smsNotificationList = new ObservableCollection<FaultNotificationSMS>();
        public ObservableCollection<FaultNotificationSMS> SMSNotificationList
        {
            get => _smsNotificationList;
            set
            {
                _smsNotificationList = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Private Methods"
        private async Task StartAsync()
        {
            try
            {
                base.SetTitle("New Recipe Type");
                base.SetStatus("New Recipe Task Screen Opened.");
                RegisterCommands();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        private bool CanSaveRecipeTask() =>
            String.IsNullOrWhiteSpace(this.UpsertRecipeType.StringKey) == false &&
            String.IsNullOrWhiteSpace(this.UpsertRecipeType.Name) == false;
        private async Task SaveNewTaskRecipeTypeAsync()
        {
            this.UpsertRecipeType.EmailNotificationList = String.Join(";", this.EmailList.Select(email => email.Value));
            this.UpsertRecipeType.SMSNotificationList = this.SMSNotificationList.ToList();
            _internalTools.TaskRecipeTypes.DbSet.Add(this.UpsertRecipeType);
            await _internalTools.CompleteAsync();
            await base.ShowInfoDialogAsync("Task Recipe Type Saved.");
            base.SetStatus("New Task Recipe Type Created.");
            await OnCloseCommand();
        }
        private async Task SaveUpdatedTaskRecipeTypeAsync()
        {
            TaskRecipeType recipe = _internalTools
                .TaskRecipeTypes.DbSet
                .FirstOrDefault(recipe => recipe.ID == this.UpsertRecipeType.ID);
            if (recipe == null) { throw new Exception($"The system could not find the task recipe with id: [{this.UpsertRecipeType.ID}]."); }

            recipe.StringKey = this.UpsertRecipeType.StringKey;
            recipe.Name = this.UpsertRecipeType.Name;
            recipe.IsActive = this.UpsertRecipeType.IsActive;
            recipe.EmailNotificationList = String.Join(";", this.EmailList.Select(email => email.Value));
            recipe.SMSNotificationList = this.SMSNotificationList.ToList();

            await _internalTools.CompleteAsync();
            await base.ShowInfoDialogAsync("Task Recipe Type Saved.");
            base.SetStatus("Task Recipe Type Updated.");
            await OnCloseCommand();
        }
        private async Task<ObservableCollection<PrimitiveWrapper<string>>> ConvertEmailStringToObservableStringWrapperAsync(string emailList)
        {
            return await Task.FromResult(
                String.IsNullOrWhiteSpace(emailList)
                    ? new ObservableCollection<PrimitiveWrapper<string>>()
                    : new ObservableCollection<PrimitiveWrapper<string>>(emailList
                        .Split(";")
                        .Select(email => new PrimitiveWrapper<string>(email))));
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand SaveCommand { get; private set; }
        public IAsyncCommand CloseCommand { get; private set; }

        private void RegisterCommands()
        {
            this.SaveCommand = new AsyncCommand(OnSaveCommand, CanSaveRecipeTask);
            this.CloseCommand = new AsyncCommand(OnCloseCommand);
        }

        private async Task OnSaveCommand()
        {
            try
            {
                base.FormIsBusy = true;
                if (this.UpsertRecipeType.ID == 0)
                {
                    await SaveNewTaskRecipeTypeAsync();
                }
                else
                {
                    await SaveUpdatedTaskRecipeTypeAsync();
                }
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
        private async Task OnCloseCommand() => await base.LoadInterfaceAsync<TaskRecipeMainView>();
        #endregion

        #region "Public Methods"
        public async Task LoadForEditAsync(TaskRecipeType recipe)
        {
            this.UpsertRecipeType = recipe;
            this.EmailList = await ConvertEmailStringToObservableStringWrapperAsync(recipe.EmailNotificationList);
            this.SMSNotificationList = new ObservableCollection<FaultNotificationSMS>(recipe.SMSNotificationList);
            base.SetStatus("Edit Task Recipe Type Opened.");
        }
        #endregion
    }
}
