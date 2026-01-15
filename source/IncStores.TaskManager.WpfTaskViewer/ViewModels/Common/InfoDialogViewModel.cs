using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using System;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Common
{
    public interface IInfoDialogViewModel
    {
        #region "Properties"
        string Title { get; set; }
        string Message { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand CloseCommand { get; }
        #endregion
    }

    internal class InfoDialogViewModelDesign : IInfoDialogViewModel
    {
        #region "Properties"
        public string Title { get; set; } = "[TITLE]";
        public string Message { get; set; } = "[MESSAGE]";
        #endregion

        #region "Relay Commands"
        public IAsyncCommand CloseCommand { get; }
        #endregion
    }

    internal class InfoDialogViewModel : BaseViewModel, IInfoDialogViewModel
    {
        #region "Member Variables"

        #endregion

        #region "Constructors"
        public InfoDialogViewModel(
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

        private string _message = String.Empty;
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand CloseCommand { get; private set; }

        private void RegisterCommands()
        {
            this.CloseCommand = new AsyncCommand(OnCloseCommand);
        }
        private Task OnCloseCommand()
        {
            base.MainVM.CurrentDialog = null;
            return Task.CompletedTask;
        }
        #endregion
    }
}
