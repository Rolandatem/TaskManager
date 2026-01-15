using IncStores.TaskManager.DataLayer.Models.Logging;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Monitors
{
    public interface IDBErrorLogMonitorViewModel
    {
        #region "Properties"
        ObservableCollection<TaskManagerLog> TaskManagerLogs { get; }
        ObservableCollection<TaskManagerLog> FilteredTaskManagerLogs { get; }
        DateTime? DateRangeStart { get; set; }
        DateTime? DateRangeEnd { get; set; }
        bool ShowInformation { get; set; }
        bool ShowDebug { get; set; }
        bool ShowWarning { get; set; }
        bool ShowCritical { get; set; }
        bool ShowError { get; set; }
        ObservableCollection<string> Loggers { get; }
        string SelectedLogger { get; set; }
        string Environment { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand RefreshErrorLogsCommand { get; }
        #endregion
    }

    internal class DBErrorLogMonitorViewModelDesign : IDBErrorLogMonitorViewModel
    {
        #region "Properties"
        public ObservableCollection<TaskManagerLog> TaskManagerLogs
        {
            get
            {
                return new ObservableCollection<TaskManagerLog>()
                {
                    new TaskManagerLog() { Date = DateTime.Now, Environment = "SM", Level = "ERROR", Logger = "Logger 1", Message = "message 1", Exception = "exception 1" },
                    new TaskManagerLog() { Date = DateTime.Now, Environment = "SM", Level = "INFO", Logger = "Logger 2", Message = "message 2", Exception = "exception 2" }
                };
            }
        }
        public ObservableCollection<TaskManagerLog> FilteredTaskManagerLogs { get => this.TaskManagerLogs; }
        public DateTime? DateRangeStart { get; set; } = DateTime.Now;
        public DateTime? DateRangeEnd { get; set; } = DateTime.Now;
        public bool ShowInformation { get; set; } = false;
        public bool ShowDebug { get; set; } = false;
        public bool ShowWarning { get; set; } = false;
        public bool ShowCritical { get; set; } = true;
        public bool ShowError { get; set; } = true;
        public ObservableCollection<string> Loggers
        {
            get
            {
                return new ObservableCollection<string>()
                {
                    "Logger 1",
                    "Logger 2"
                };
            }
        }
        public string SelectedLogger { get; set; } = "ALL";
        public string Environment { get; set; } = "SM";
        #endregion

        #region "Relay Commands"
        public IAsyncCommand RefreshErrorLogsCommand { get; }
        #endregion
    }

    internal class DBErrorLogMonitorViewModel : BaseViewModel, IDBErrorLogMonitorViewModel
    {
        #region "Member Variables"
        readonly ICommonLoggingUnitOfWork _logging = null;
        readonly IHostEnvironment _hostEnvironment = null;
        #endregion

        #region "Constructor"
        public DBErrorLogMonitorViewModel(
            IServiceProvider serviceProvider,
            ICommonLoggingUnitOfWork logging,
            IHostEnvironment hostEnvironment)
            : base(serviceProvider)
        {
            _logging = logging;
            _hostEnvironment = hostEnvironment;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private ObservableCollection<TaskManagerLog> _taskManagerLogs = new ObservableCollection<TaskManagerLog>();
        public ObservableCollection<TaskManagerLog> TaskManagerLogs
        {
            get => _taskManagerLogs;
            set
            {
                _taskManagerLogs = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskManagerLogs");
            }
        }

        private DateTime? _dateRangeStart = null;
        public DateTime? DateRangeStart
        {
            get => _dateRangeStart;
            set
            {
                _dateRangeStart = value;
                RaisePropertyChanged();
            }
        }

        private DateTime? _dateRangeEnd = null;
        public DateTime? DateRangeEnd
        {
            get => _dateRangeEnd;
            set
            {
                _dateRangeEnd = value;
                RaisePropertyChanged();
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
                RaisePropertyChanged("FilteredTaskManagerLogs");
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
                RaisePropertyChanged("FilteredTaskManagerLogs");
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
                RaisePropertyChanged("FilteredTaskManagerLogs");
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
                RaisePropertyChanged("FilteredTaskManagerLogs");
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
                RaisePropertyChanged("FilteredTaskManagerLogs");
            }
        }

        public ObservableCollection<TaskManagerLog> FilteredTaskManagerLogs
        {
            get
            {
                List<TaskManagerLog> result = this.TaskManagerLogs
                .Where(item =>

                    ((this.ShowInformation && item.Level == "INFO") ||
                    (this.ShowDebug && item.Level == "DEBUG") ||
                    (this.ShowWarning && item.Level == "WARN") ||
                    (this.ShowCritical && item.Level == "CRITICAL") ||
                    (this.ShowError && item.Level == "ERROR")) &&

                    (this.SelectedLogger == "ALL" || item.Logger.Contains(this.SelectedLogger)))
                .ToList();

                if (result.Count == this.TaskManagerLogs.Count)
                { base.SetStatus($"{result.Count} record(s) found."); }
                else
                { base.SetStatus($"Filtered {result.Count} record(s) of {this.TaskManagerLogs.Count}"); }

                return new ObservableCollection<TaskManagerLog>(result);
            }
        }

        private ObservableCollection<string> _loggers = new ObservableCollection<string>();
        public ObservableCollection<string> Loggers
        {
            get => _loggers;
            set
            {
                _loggers = value;
                _loggers.Insert(0, "ALL");
                RaisePropertyChanged();
            }
        }

        private string _selectedLogger = "ALL";
        public string SelectedLogger
        {
            get => _selectedLogger;
            set
            {
                _selectedLogger = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredTaskManagerLogs");
            }
        }

        private string _environment = null;
        public string Environment
        {
            get => _environment;
            set
            {
                if (value.Trim() == "") { value = null; }
                _environment = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand RefreshErrorLogsCommand { get; private set; }

        private void RegisterCommands()
        {
            this.RefreshErrorLogsCommand = new AsyncCommand(OnRefreshErrorLogsCommand);
        }

        private async Task OnRefreshErrorLogsCommand()
        {
            try
            {
                base.FormIsBusy = true;
                this.TaskManagerLogs = new ObservableCollection<TaskManagerLog>(
                    await _logging.TaskManagerLogs.GetLogsAsync(
                        dateRangeStart: this.DateRangeStart,
                        dateRangeEnd: this.DateRangeEnd,
                        environment: String.IsNullOrWhiteSpace(this.Environment) ? null : this.Environment));
                this.Loggers = new ObservableCollection<string>(await _logging.TaskManagerLogs.GetLoggersAsync(this.Environment));
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
            finally { this.FormIsBusy = false; }
        }
        #endregion

        public async Task StartAsync()
        {
            base.SetTitle("DB Error Log Monitor");
            base.SetStatus("Opened DB Error Log Monitor");
            RegisterCommands();
            this.Environment = _hostEnvironment.EnvironmentName;
            await OnRefreshErrorLogsCommand();
        }
    }
}
