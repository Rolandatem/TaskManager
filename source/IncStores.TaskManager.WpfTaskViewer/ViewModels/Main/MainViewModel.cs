using IncStores.TaskManager.DataLayer.DTOs.IncStores;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Common;
using IncStores.TaskManager.WpfTaskViewer.Views.Main;
using IncStores.TaskManager.WpfTaskViewer.Views.Monitors;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipe;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipeQueue;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskScheduler;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MenuItem = IncStores.TaskManager.WpfTaskViewer.Models.MenuItem;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Main
{
    public interface IMainViewModel
    {
        #region "Properties"
        string WindowTitle { get; set; }
        string StatusText { get; set; }
        ObservableCollection<MenuItem> MainMenu { get; set; }
        bool MainFormIsBusy { get; set; }
        bool MainMenuAndStatusBarAreVisible { get; set; }
        UserControl CurrentInterface { get; set; }
        UserControl CurrentDialog { get; set; }
        AppUser AppUser { get; }
        ISharedCommunicatorViewModel SharedHubCommunicator { get; }
        #endregion

        #region "Public Methods"
        Task LoginSuccessAsync(AppUser appUser);
        Task DisplayMaintenanceWindowScreenAsync(DateTime startTime, DateTime endTime);
        #endregion

        #region "ICommands"
        IAsyncCommand ExitAppCommand { get; }

        //--System Tools
        //----SignalR Server
        IAsyncCommand CloseSignalRConnectionCommand { get; }
        IAsyncCommand OpenSignalRConnectionCommand { get; }
        //----Check for Updates
        IAsyncCommand CheckForUpdatesCommand { get; }

        //--Tools
        IAsyncCommand OpenTaskRecipeQueueListCommand { get; }
        IAsyncCommand OpenAddTaskRecipeQueueRequestCommand { get; }

        IAsyncCommand OpenTaskSchedulerCurrentScheduleCommand { get; }
        IAsyncCommand OpenAddNewTaskScheduleCommand { get; }
       
        IAsyncCommand OpenTaskRecipeTypeListCommand { get; }
        IAsyncCommand OpenAddNewTaskRecipeTypeCommand { get; }

        //--Monitors
        IAsyncCommand OpenLiveMonitorCommand { get; }
        IAsyncCommand OpenDBAuditLogMonitorCommand { get; }
        IAsyncCommand OpenDBErrorLogMonitorCommand { get; }
        #endregion
    }

    internal class MainViewModelDesign : IMainViewModel
    {
        #region "Form Properties"
        public string WindowTitle { get; set; } = "TaskManager Viewer - Design";
        public string StatusText { get; set; } = "[STATUS_TEXT]";
        public ObservableCollection<MenuItem> MainMenu { get; set; } = new ObservableCollection<MenuItem>()
        {
            new MenuItem()
            {
                Text = "SignalR Server",
                IsParent = true,
                Children = new ObservableCollection<MenuItem>()
                {
                    new MenuItem()
                    {
                        Text = "Open Connection"
                    },
                    new MenuItem()
                    {
                        Text = "Close Connection"
                    }
                }
            },
            new MenuItem()
            {
                Text = "Tools",
                IsParent = true,
                Children = new ObservableCollection<MenuItem>()
                {
                    new MenuItem()
                    {
                        Text = "Tool 1"
                    },
                    new MenuItem()
                    {
                        Text = "Tool 2"
                    }
                }
            },
            new MenuItem()
            {
                Text = "Exit",
                IsParent = true,
                Command = "ExitAppCommand"
            }
        };
        public bool MainFormIsBusy { get; set; } = false;
        public bool MainMenuAndStatusBarAreVisible { get; set; } = true;
        public UserControl CurrentInterface { get; set; } = new LoginView();
        public UserControl CurrentDialog { get; set; } = null;
        public AppUser AppUser
        {
            get => new AppUser()
            {
                UserName = "Design User"
            };
        }
        public ISharedCommunicatorViewModel SharedHubCommunicator { get; } = null;
        #endregion

        #region "Public Methods"
        public Task LoginSuccessAsync(AppUser appUser) => Task.CompletedTask;
        public Task DisplayMaintenanceWindowScreenAsync(DateTime startTime, DateTime endTime) => Task.CompletedTask;
        #endregion

        #region "Relay Commands"
        public IAsyncCommand ExitAppCommand { get; set; }

        //--System Tools
        //----SignalR Server
        public IAsyncCommand CloseSignalRConnectionCommand { get; }
        public IAsyncCommand OpenSignalRConnectionCommand { get; }
        //----Check for Updates
        public IAsyncCommand CheckForUpdatesCommand { get; }

        //--Tools
        public IAsyncCommand OpenTaskRecipeQueueListCommand { get; }
        public IAsyncCommand OpenAddTaskRecipeQueueRequestCommand { get; }

        public IAsyncCommand OpenTaskSchedulerCurrentScheduleCommand { get; }
        public IAsyncCommand OpenAddNewTaskScheduleCommand { get; }

        public IAsyncCommand OpenTaskRecipeTypeListCommand { get; }
        public IAsyncCommand OpenAddNewTaskRecipeTypeCommand { get; }

        //--Monitors
        public IAsyncCommand OpenLiveMonitorCommand { get; }
        public IAsyncCommand OpenDBAuditLogMonitorCommand { get; }
        public IAsyncCommand OpenDBErrorLogMonitorCommand { get; }
        #endregion
    }

    internal class MainViewModel : BaseViewModel, IMainViewModel
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly ISharedCommunicatorViewModel _sharedCommunicatorViewModel = null;
        readonly IHostEnvironment _env = null;
        AppUser _appUser = null;
        #endregion

        #region "Constructor"
        public MainViewModel(
            IServiceProvider serviceProvider,
            ISharedCommunicatorViewModel sharedCommunicatorViewModel,
            LoginView loginView,
            AppUser appUser,
            IHostEnvironment env)
            : base(serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _sharedCommunicatorViewModel = sharedCommunicatorViewModel;
            _env = env;
            _appUser = appUser;
            this.CurrentInterface = loginView;
            this.StatusText = env.EnvironmentName;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private string _windowTitle = String.Empty;
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = $"Task Manager Viewer ({_env.EnvironmentName}) {(value != String.Empty ? "-" : "")} {value}";
                RaisePropertyChanged();
            }
        }

        private string _statusText = String.Empty;
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<MenuItem> _mainMenu = null;
        public ObservableCollection<MenuItem> MainMenu
        {
            get => _mainMenu;
            set
            {
                _mainMenu = value;
                RaisePropertyChanged();
            }
        }

        private bool _mainFormIsBusy = false;
        public bool MainFormIsBusy
        {
            get => _mainFormIsBusy;
            set
            {
                _mainFormIsBusy = value;
                RaisePropertyChanged();
            }
        }

        private bool _mainMenuAndStatusBarAreVisible = false;
        public bool MainMenuAndStatusBarAreVisible
        {
            get => _mainMenuAndStatusBarAreVisible;
            set
            {
                _mainMenuAndStatusBarAreVisible = value;
                RaisePropertyChanged();
            }
        }

        private UserControl _currentInterface = null;
        public UserControl CurrentInterface
        {
            get => _currentInterface;
            set
            {
                _currentInterface = value;
                RaisePropertyChanged();
            }
        }

        private UserControl _currentDialog = null;
        public UserControl CurrentDialog
        {
            get => _currentDialog;
            set
            {
                _currentDialog = value;
                RaisePropertyChanged();
            }
        }

        public AppUser AppUser
        {
            get => _appUser;
            set
            {
                _appUser = value;
                RaisePropertyChanged();
            }
        }

        public ISharedCommunicatorViewModel SharedHubCommunicator => _sharedCommunicatorViewModel;
        #endregion

        #region "Private Methods"
        private async Task<ObservableCollection<MenuItem>> LoadMenuAsync()
        {
            try
            {
                string mainMenuJsonString = await File.ReadAllTextAsync("settings/mainmenu.json");
                return JsonConvert.DeserializeObject<ObservableCollection<MenuItem>>(mainMenuJsonString);
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex, "Load Menu Error");
                return null;
            }
        }
        #endregion

        #region "Public Methods"
        public async Task StartAsync()
        {
            try
            {
                this.WindowTitle = "";
                this.MainMenu = await LoadMenuAsync();
                RegisterCommands();

                //--Hook into connected status of signal r service.
                this.SharedHubCommunicator.ConnectToSignalRServerEvent += async (obj, e) =>
                {
                    if (e.IsConnected)
                    {
                        await base.LoadInterfaceAsync<LiveStatusMonitorView>();
                    }
                    else
                    {
                        //--Do not show disconnected view if the disconnect was caused my maintenance window.
                        if (this.SharedHubCommunicator.IsTaskManagerUnderMaintenanceWindow)
                        { return; }

                        await base.LoadInterfaceAsync<DisconnectedView>();
                    }
                };
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        public async Task LoginSuccessAsync(AppUser appUser)
        {
            try
            {
                this.AppUser = appUser;
                this.MainMenuAndStatusBarAreVisible = true;
                this.StatusText = "Login Successful.";
                //await base.LoadInterfaceAsync<LiveStatusMonitorView>();
                await _sharedCommunicatorViewModel.InitAsync();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        public async Task DisplayMaintenanceWindowScreenAsync(DateTime startTime, DateTime endTime)
        {
            this.SharedHubCommunicator.IsTaskManagerUnderMaintenanceWindow = true;
            await base.LoadInterfaceAsync<MaintenanceWindowUnderwayView>((vmObject) =>
            {
                IMaintenanceWindowUnderwayViewModel vm = vmObject as IMaintenanceWindowUnderwayViewModel;
                vm.StartTime = startTime;
                vm.EndTime = endTime;
            });
        }
        #endregion

        #region "Commands"
        public IAsyncCommand ExitAppCommand { get; private set; }
        public IAsyncCommand CloseSignalRConnectionCommand { get; private set; }
        public IAsyncCommand OpenSignalRConnectionCommand { get; private set; }
        public IAsyncCommand CheckForUpdatesCommand { get; private set; }
        public IAsyncCommand OpenTaskRecipeQueueListCommand { get; private set; }
        public IAsyncCommand OpenAddTaskRecipeQueueRequestCommand { get; private set; }
        public IAsyncCommand OpenTaskSchedulerCurrentScheduleCommand { get; private set; }
        public IAsyncCommand OpenAddNewTaskScheduleCommand { get; private set; }
        public IAsyncCommand OpenTaskRecipeTypeListCommand { get; private set; }
        public IAsyncCommand OpenAddNewTaskRecipeTypeCommand { get; private set; }
        public IAsyncCommand OpenLiveMonitorCommand { get; private set; }
        public IAsyncCommand OpenDBAuditLogMonitorCommand { get; private set; }
        public IAsyncCommand OpenDBErrorLogMonitorCommand { get; private set; }

        private void RegisterCommands()
        {
            this.ExitAppCommand = new AsyncCommand(OnExitCommand);
            this.CloseSignalRConnectionCommand = new AsyncCommand(OnCloseSignalRConnectionCommand);
            this.OpenSignalRConnectionCommand = new AsyncCommand(OnOpenSignalRConnectionCommand);
            this.CheckForUpdatesCommand = new AsyncCommand(OnCheckForUpdatesCommand);
            this.OpenTaskRecipeQueueListCommand = new AsyncCommand(OnOpenTaskRecipeQueueListCommand);
            this.OpenAddTaskRecipeQueueRequestCommand = new AsyncCommand(OnOpenAddTaskRecipeQueueRequestCommand);
            this.OpenTaskSchedulerCurrentScheduleCommand = new AsyncCommand(OnOpenTaskSchedulerCurrentScheduleCommand);
            this.OpenAddNewTaskScheduleCommand = new AsyncCommand(OnOpenAddNewTaskScheduleCommand);
            this.OpenTaskRecipeTypeListCommand = new AsyncCommand(OnOpenTaskRecipeTypeListCommand);
            this.OpenAddNewTaskRecipeTypeCommand = new AsyncCommand(OnOpenAddNewTaskRecipeTypeCommand);
            this.OpenLiveMonitorCommand = new AsyncCommand(OnOpenLiveMonitorCommand);
            this.OpenDBAuditLogMonitorCommand = new AsyncCommand(OnOpenDBAuditLogMonitorCommand);
            this.OpenDBErrorLogMonitorCommand = new AsyncCommand(OnOpenDBErrorLogMonitorCommand);
        }
        private async Task OnExitCommand()
        {
            try
            {
                //Environment.Exit(0);
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        private async Task OnCloseSignalRConnectionCommand()
        {
            try
            {
                if (await _sharedCommunicatorViewModel.CloseConnectionAsync())
                {
                    await base.ShowInfoDialogAsync("Manual Close of Task Manager Hub connection successful.");
                }
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnOpenSignalRConnectionCommand()
        {
            try
            {
                if (await _sharedCommunicatorViewModel.ReconnectConnectionAsync())
                {
                    await base.ShowInfoDialogAsync("Manual Open of Task Manager Hub connection successful.");
                }
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnCheckForUpdatesCommand()
        {
            try
            {
                await base.ShowInfoDialogAsync("Placeholder command for when ClickOnce ApplicationDeployment becomes available. " +
                    "This appears to not be available currently for .NET 5" +
                    Environment.NewLine +
                    Environment.NewLine +
                    "https://github.com/dotnet/deployment-tools/issues/27");
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnOpenTaskRecipeQueueListCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<TaskRecipeQueueMainView>();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnOpenAddTaskRecipeQueueRequestCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<UpsertTaskRecipeRequestView>();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnOpenTaskSchedulerCurrentScheduleCommand()
        {
            try
            {
                //this.CurrentInterface = _serviceProvider.GetService<TaskSchedulerMainView>();
                await base.LoadInterfaceAsync<TaskSchedulerMainView>();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        private async Task OnOpenAddNewTaskScheduleCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<UpsertScheduledTaskView>();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        private async Task OnOpenTaskRecipeTypeListCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<TaskRecipeMainView>();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        private async Task OnOpenAddNewTaskRecipeTypeCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<UpsertRecipeTypeView>();
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
        }
        private async Task OnOpenLiveMonitorCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<LiveStatusMonitorView>();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnOpenDBAuditLogMonitorCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<DBAuditLogMonitorView>();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        private async Task OnOpenDBErrorLogMonitorCommand()
        {
            try
            {
                await base.LoadInterfaceAsync<DBErrorLogMonitorView>();
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
        }
        #endregion
    }
}
