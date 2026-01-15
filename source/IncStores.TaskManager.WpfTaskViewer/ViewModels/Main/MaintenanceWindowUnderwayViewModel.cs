using System;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Main
{
    public interface IMaintenanceWindowUnderwayViewModel
    {
        #region "Properties"
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan TimeRemaining { get; }
        #endregion
    }

    internal class MaintenanceWindowUnderwayViewModelDesign : IMaintenanceWindowUnderwayViewModel
    {
        #region "Properties"
        public DateTime StartTime { get; set; } = DateTime.Now.AddHours(-1);
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(1);
        public TimeSpan TimeRemaining { get => this.EndTime - this.StartTime; }
        #endregion
    }

    internal class MaintenanceWindowUnderwayViewModel : BaseViewModel, IMaintenanceWindowUnderwayViewModel
    {
        #region "Member Variables"
        readonly CancellationTokenSource _timeRemainingRefreshCancellationTokenSource = new CancellationTokenSource();

        Task _backgroundTimeRemainingRefreshTaskHolder = null;
        #endregion

        #region "Constructor"
        public MaintenanceWindowUnderwayViewModel(
            IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            base.Init = StartAsync();
        }
        #endregion

        #region "Form Properties"
        private DateTime _startTime = DateTime.Now;
        public DateTime StartTime
        {
            get => _startTime;
            set
            {
                _startTime = value;
                RaisePropertyChanged();
            }
        }

        private DateTime _endTime = DateTime.Now;
        public DateTime EndTime
        {
            get => _endTime;
            set
            {
                //--Add 20 seconds to end time to give the task manager enough time to boot up
                //--before attempting to reconnect.
                _endTime = value.AddSeconds(20);
                RaisePropertyChanged();
            }
        }

        public TimeSpan TimeRemaining => this.EndTime - DateTime.Now;
        #endregion

        #region "Private Methods"
        private async Task BackgroundTimeRemainingRefresher()
        {
            while (_timeRemainingRefreshCancellationTokenSource.IsCancellationRequested == false)
            {
                RaisePropertyChanged("TimeRemaining");
                await Task.Delay(1000, _timeRemainingRefreshCancellationTokenSource.Token);

                if (DateTime.Now >= this.EndTime)
                { _timeRemainingRefreshCancellationTokenSource.Cancel(); }
            }

            await base.MainVM.OpenSignalRConnectionCommand.ExecuteAsync();
        }
        private Task StartAsync()
        {
            _backgroundTimeRemainingRefreshTaskHolder = BackgroundTimeRemainingRefresher();

            return Task.CompletedTask;
        }
        #endregion

        #region "Overrides"
        public override Task StopAsync()
        {
            _timeRemainingRefreshCancellationTokenSource.Cancel();
            return Task.CompletedTask;
        }
        #endregion
    }
}
