using Hardcodet.Wpf.TaskbarNotification;
using Incstores.Common.Settings;
using IncStores.TaskManager.Core.Settings;
using IncStores.TaskManager.DataLayer.DTOs.IncStores;
using IncStores.TaskManager.DataLayer.Settings;
using IncStores.TaskManager.WpfTaskViewer.Settings.Models;
using IncStores.TaskManager.WpfTaskViewer.SignalR;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Common;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Main;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Monitors;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.SystemTray;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipe;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskRecipeQueue;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.TaskScheduler;
using IncStores.TaskManager.WpfTaskViewer.Views.Main;
using IncStores.TaskManager.WpfTaskViewer.Views.Monitors;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipe;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskRecipeQueue;
using IncStores.TaskManager.WpfTaskViewer.Views.TaskScheduler;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MenuItem = System.Windows.Controls.MenuItem;

namespace IncStores.TaskManager.WpfTaskViewer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        #region "Member Variables"
        public IHost _host = null;
        public TaskbarIcon _systemTrayNotifyIcon = null;
        MainView _mainView = null;
        #endregion

        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                _host = CreateHostBuilder().Build();

                _mainView = _host.Services.GetService<MainView>();
                _mainView.Hide();

                SetupSystemTray();

                //await _host.RunAsync();
            }
            catch (Exception ex)
            {

            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _systemTrayNotifyIcon.Dispose();
            base.OnExit(e);
        }

        private static IHostBuilder CreateHostBuilder() =>
            Host.CreateDefaultBuilder()
                .ConfigureHostConfiguration(hostBuilder =>
                {
                    hostBuilder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddEnvironmentVariables(prefix: "ASPNETCORE_");
                })
                .ConfigureAppConfiguration((hostContext, configurationBuilder) =>
                {
                    IHostEnvironment hostEnvironment = hostContext.HostingEnvironment;
                    string env = hostEnvironment.EnvironmentName;

                    configurationBuilder
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("settings/appsettings.json", false, true);

                    //--Development
                    if (env != "Production")
                    {
                        configurationBuilder
                            .AddJsonFile("settings/appsettings.CommonDevelopment.json", false, true);
                    }

                    configurationBuilder
                        .AddJsonFile($"settings/appsettings.{env}.json", false, true);

                    //--User Secrets
                    configurationBuilder
                        .AddUserSecrets<App>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    IConfiguration config = hostContext.Configuration;

                    services
                        //-- Outside Services
                        .AddTaskManagerDataLayerServices()
                        .AddTaskManagerCoreServices(config)

                        //--Option Models
                        .Configure<ConnectionStrings>(config.GetSection("connectionStrings"))
                        .Configure<TaskManagerSignalRService>(config.GetSection("TaskManagerSignalRService"))

                        //--Objects/Models
                        .AddSingleton<AppUser>()

                        //--Hub Consumers
                        .AddSingleton<ITaskManagerRecipeHubConsumer, TaskManagerRecipeHubConsumer>()
                        .AddSingleton<ISharedCommunicatorViewModel, SharedCommunicatorViewModel>()

                        //--Add View/ViewModels
                        .AddTransient<IInfoDialogViewModel, InfoDialogViewModel>()
                        .AddTransient<IErrorDialogViewModel, ErrorDialogViewModel>()
                        .AddTransient<IPromptDialogViewModel, PromptDialogViewModel>()

                        //.AddSingleton<SystemTrayMainView>()
                        .AddSingleton<ISystemTrayPopupViewModel, SystemTrayPopupViewModel>()

                        .AddSingleton<MainView>()
                        .AddSingleton<IMainViewModel, MainViewModel>()
                        .AddSingleton<LoginView>()
                        .AddSingleton<ILoginViewModel, LoginViewModel>()
                        .AddSingleton<DisconnectedView>()
                        .AddSingleton<MaintenanceWindowUnderwayView>()
                        .AddSingleton<IMaintenanceWindowUnderwayViewModel, MaintenanceWindowUnderwayViewModel>()
                        
                        .AddTransient<TaskSchedulerMainView>()
                        .AddTransient<ITaskSchedulerMainViewModel, TaskSchedulerMainViewModel>()
                        .AddTransient<UpsertScheduledTaskView>()
                        .AddTransient<IUpsertScheduledTaskViewModel, UpsertScheduledTaskViewModel>()
                        
                        .AddTransient<TaskRecipeMainView>()
                        .AddTransient<ITaskRecipeMainViewModel, TaskRecipeMainViewModel>()
                        .AddTransient<UpsertRecipeTypeView>()
                        .AddTransient<IUpsertRecipeTypeViewModel, UpsertRecipeTypeViewModel>()

                        .AddTransient<TaskRecipeQueueMainView>()
                        .AddTransient<ITaskRecipeQueueMainViewModel, TaskRecipeQueueMainViewModel>()
                        .AddTransient<UpsertTaskRecipeRequestView>()
                        .AddTransient<IUpsertTaskRecipeRequestViewModel, UpsertTaskRecipeRequestViewModel>()
                        
                        .AddTransient<LiveStatusMonitorView>()
                        .AddTransient<ILiveStatusMonitorViewModel, LiveStatusMonitorViewModel>()
                        .AddTransient<DBAuditLogMonitorView>()
                        .AddTransient<IDBAuditLogMonitorViewModel, DBAuditLogMonitorViewModel>()
                        .AddTransient<DBErrorLogMonitorView>()
                        .AddTransient<IDBErrorLogMonitorViewModel, DBErrorLogMonitorViewModel>();
                });

        private void SetupSystemTray()
        {
            IHostEnvironment env = _host.Services.GetService<IHostEnvironment>();

            _systemTrayNotifyIcon = (TaskbarIcon)FindResource("WPFTaskViewerNotifyIcon");
            _systemTrayNotifyIcon.LeftClickCommand = new AsyncCommand(() =>
            {
                _mainView.Show();
                return Task.CompletedTask;  
            });
            _systemTrayNotifyIcon.ContextMenu = new ContextMenu();
            _systemTrayNotifyIcon.ContextMenu.Items.Add(new MenuItem()
            {
                Header = "Exit TaskViewer",
                //Command = (_mainView.DataContext as IMainViewModel).ExitAppCommand
                Command = new AsyncCommand(() =>
                {
                    Application.Current.Shutdown();
                    return Task.CompletedTask;
                })
            });
            //_systemTrayNotifyIcon.TrayToolTip = _host.Services.GetService<SystemTrayMainView>();
            //_systemTrayNotifyIcon.TrayPopup = _host.Services.GetService<SystemTrayMainView>();

            //--Temporary show environment on tray tool tip until we get a better setup.
            _systemTrayNotifyIcon.ToolTipText = $"ENV: {env.EnvironmentName}";
        }
    }
}
