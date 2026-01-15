using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using System;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Common
{
    public interface IErrorDialogViewModel
    {
        #region "Properties"
        string Title { get; set; }
        string ErrorMessage { get; }
        string ExtendedMessage { get; }
        string ShowHideExtendedInfoText { get; }
        bool ShowExtendedInfo { get; set; }
        Exception ErrorException { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand ShowHideExtendedInfoCommand { get; }
        IAsyncCommand CloseCommand { get; }
        #endregion
    }

    internal class ErrorDialogViewModelDesign : IErrorDialogViewModel
    {
        #region "Properties"
        public string Title { get; set; } = "[TITLE]";
        public string ErrorMessage { get => this.ErrorException.Message; }
        public string ExtendedMessage
        {
            get
            {
                bool isMainException = true;
                Func<Exception, string, string> tunnelEx = null;
                tunnelEx = (e, msg) =>
                {
                    msg += $"{(isMainException ? "[EXCEPTION MESSAGE]:" : "[INNER EXCEPTION]:")}{Environment.NewLine}";
                    isMainException = false;

                    msg += $@"{e.Message}{Environment.NewLine}{Environment.NewLine}[STACK TRACE]:{e.StackTrace}{Environment.NewLine}---------------{Environment.NewLine}";

                    if (e.InnerException == null) { return msg; }

                    return tunnelEx(e.InnerException, msg);
                };

                return tunnelEx(this.ErrorException, String.Empty);
            }
        }
        public string ShowHideExtendedInfoText { get; } = "Hide Extended Info";
        public bool ShowExtendedInfo { get; set; } = true;

        public Exception ErrorException
        {
            get => new Exception("[EXCEPTION_MESSAGE]",
                   new Exception("[INNER_EXCEPTION_MESSAGE]"));
            set => _ = value;
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand ShowHideExtendedInfoCommand { get; set; }
        public IAsyncCommand CloseCommand { get; set; }
        #endregion
    }

    internal class ErrorDialogViewModel : BaseViewModel, IErrorDialogViewModel
    {
        #region "Member Variables"

        #endregion

        #region "Constructor"
        public ErrorDialogViewModel(
            IServiceProvider serviceProvider)
            : base(serviceProvider) 
        {
            RegisterCommands();
        }
        #endregion

        #region "Form Properties"
        private string _title = String.Empty;
        public string Title
        {
            get => _title.ToUpper();
            set
            {
                _title = value;
                RaisePropertyChanged();
            }
        }

        public string ErrorMessage { get => this.ErrorException.Message; }

        public string ExtendedMessage
        {
            get
            {
                bool isMainException = true;
                Func<Exception, string, string> tunnelEx = null;
                tunnelEx = (e, msg) =>
                {
                    msg += $"{(isMainException ? "[EXCEPTION MESSAGE]:" : "[INNER EXCEPTION]:")}{Environment.NewLine}";
                    isMainException = false;

                    msg += $@"{e.Message}{Environment.NewLine}{Environment.NewLine}[STACK TRACE]:{Environment.NewLine}{e.StackTrace}{Environment.NewLine}---------------{Environment.NewLine}";

                    if (e.InnerException == null) { return msg; }

                    return tunnelEx(e.InnerException, msg);
                };

                return tunnelEx(this.ErrorException, String.Empty);
            }
        }

        public string ShowHideExtendedInfoText
        {
            get => $"{(this.ShowExtendedInfo ? "Hide" : "Show")} Extended Info";
        }

        private bool _showExtendedInfo = false;
        public bool ShowExtendedInfo
        {
            get => _showExtendedInfo;
            set
            {
                _showExtendedInfo = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(ShowHideExtendedInfoText));
            }
        }
        #endregion

        #region "Public Properties"
        public Exception ErrorException { get; set; }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand ShowHideExtendedInfoCommand { get; private set; }
        public IAsyncCommand CloseCommand { get; private set; }

        private void RegisterCommands()
        {
            this.ShowHideExtendedInfoCommand = new AsyncCommand(OnShowHideExtendedInfoCommand);
            this.CloseCommand = new AsyncCommand(OnCloseCommand);
        }
        private Task OnCloseCommand()
        {
            base.MainVM.CurrentDialog = null;
            return Task.CompletedTask;
        }
        private Task OnShowHideExtendedInfoCommand()
        {
            this.ShowExtendedInfo = !this.ShowExtendedInfo;
            return Task.CompletedTask;
        }
        #endregion
    }
}
