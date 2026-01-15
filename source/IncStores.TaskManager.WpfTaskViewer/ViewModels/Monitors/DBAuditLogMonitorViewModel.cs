using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Models;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Monitors
{
    public interface IDBAuditLogMonitorViewModel
    {
        #region "Properties"
        ObservableCollection<AuditLogItem> AuditLog { get; }
        ObservableCollection<AuditLogItem> FilteredAuditLog { get; }
        ObservableCollection<string> GroupKeys { get; }
        string SelectedGroupKey { get; set; }
        int MaxRecordsRequested { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand RefreshAuditLogCommand { get; }
        #endregion
    }

    internal class DBAuditLogMonitorViewModelDesign : IDBAuditLogMonitorViewModel
    {
        #region "Properties"
        public ObservableCollection<AuditLogItem> AuditLog 
        {
            get
            {
                return new ObservableCollection<AuditLogItem>()
                {
                    new AuditLogItem("message 1", "initiator 1", "groupkey 1", DateTime.Now),
                    new AuditLogItem("message 2", "initiator 2", "groupkey 2", DateTime.Now)
                };
            }
        }
        public ObservableCollection<AuditLogItem> FilteredAuditLog { get; }
        public ObservableCollection<string> GroupKeys { get; }
        public string SelectedGroupKey { get; set; } = "ALL";
        public int MaxRecordsRequested { get; set; } = 200;
        #endregion

        #region "Relay Commands"
        public IAsyncCommand RefreshAuditLogCommand { get; }
        #endregion
    }

    internal class DBAuditLogMonitorViewModel : BaseViewModel, IDBAuditLogMonitorViewModel
    {
        #region "Member Variables"
        readonly ICommonInternalToolsUnitOfWork _internalTools = null;
        readonly IHostEnvironment _hostEnvironment = null;
        #endregion

        #region "Constructor"
        public DBAuditLogMonitorViewModel(
            IServiceProvider serviceProvider,
            ICommonInternalToolsUnitOfWork internalTools,
            IHostEnvironment hostEnvironment)
            : base(serviceProvider)
        {
            _internalTools = internalTools;
            _hostEnvironment = hostEnvironment;
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private ObservableCollection<AuditLogItem> _auditLog = new ObservableCollection<AuditLogItem>();
        public ObservableCollection<AuditLogItem> AuditLog
        {
            get => _auditLog;
            set
            {
                _auditLog = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredAuditLog");
                RaisePropertyChanged("GroupKeys");
            }
        }

        public ObservableCollection<AuditLogItem> FilteredAuditLog
        {
            get
            {
                var query = this.AuditLog.AsQueryable();

                if (this.SelectedGroupKey != "ALL")
                { query = query.Where(item => item.GroupKey == this.SelectedGroupKey); }

                List<AuditLogItem> result = query.ToList();

                if (result.Count == this.AuditLog.Count)
                { base.SetStatus($"{result.Count} record(s) found."); }
                else
                { base.SetStatus($"Filtered {result.Count} record(s) from {this.AuditLog.Count}."); }

                return new ObservableCollection<AuditLogItem>(result);
            }
        }

        public ObservableCollection<string> GroupKeys
        {
            get
            {
                ObservableCollection<string> keys = new ObservableCollection<string>(
                    this.AuditLog
                        .Select(item => item.GroupKey)
                        .Distinct()
                        .OrderBy(item => item)
                        .ToList());
                keys.Insert(0, "ALL");
                return keys;
            }
        }

        private string _selectedGroupKey = "ALL";
        public string SelectedGroupKey
        {
            get => _selectedGroupKey;
            set
            {
                _selectedGroupKey = value;
                RaisePropertyChanged();
                RaisePropertyChanged("FilteredAuditLog");
            }
        }

        private int _maxRecordsRequested = 200;
        public int MaxRecordsRequested
        {
            get => _maxRecordsRequested;
            set
            {
                _maxRecordsRequested = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand RefreshAuditLogCommand { get; private set; }

        private void RegisterCommands()
        {
            this.RefreshAuditLogCommand = new AsyncCommand(OnRefreshAuditLogCommand);
        }

        private async Task OnRefreshAuditLogCommand()
        {
            try
            {
                base.FormIsBusy = true;
                List<AuditHistory> auditLog = await _internalTools.AuditHistory
                    .GetAuditHistoryAsync(_hostEnvironment.EnvironmentName, this.MaxRecordsRequested);
                this.AuditLog = new ObservableCollection<AuditLogItem>(auditLog
                    .Select(item => new AuditLogItem()
                    {
                        AuditDateTime = item.AuditDateTime,
                        GroupKey = item.GroupKey,
                        Initiator = item.Initiator,
                        Message = item.Message
                    })
                    .ToList());
            }
            catch (Exception ex) { await base.ShowErrorDialogAsync(ex); }
            finally { base.FormIsBusy = false; }
        }
        #endregion

        public async Task StartAsync()
        {
            base.SetTitle("DB Audit Log Monitor");
            base.SetStatus("Opened DB Audit Log Monitor");
            RegisterCommands();
            await OnRefreshAuditLogCommand();
        }
    }
}
