using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using System;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Common
{
    public interface IPromptDialogViewModel
    {
        #region "Properties"
        string Prompt { get; set; }
        string YesText { get; set; }
        string NoText { get; set; }
        #endregion

        #region "Callbacks"
        Action YesCallback { get; set; }
        Action NoCallback { get; set; }
        #endregion

        #region "Relay Commands"
        IAsyncCommand YesCommand { get; }
        IAsyncCommand NoCommand { get; }
        #endregion
    }

    internal class PromptDialogViewModelDesign : IPromptDialogViewModel
    {
        #region "Properties"
        public string Prompt { get; set; } = "[PROMPT_MESSAGE]";
        public string YesText { get; set; } = "Yes";
        public string NoText { get; set; } = "No";
        #endregion

        #region "Callbacks"
        public Action YesCallback { get; set; } = () => { };
        public Action NoCallback { get; set; } = () => { };
        #endregion

        #region "Relay Commands"
        public IAsyncCommand YesCommand { get; }
        public IAsyncCommand NoCommand { get; }
        #endregion
    }

    internal class PromptDialogViewModel : BaseViewModel, IPromptDialogViewModel
    {
        #region "Constructor"
        public PromptDialogViewModel(
            IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            RegisterCommands();
            this.NoCallback = async () =>
            {
                await CloseDialogAsync();
            };
        }
        #endregion

        #region "Form Properties"
        private string _prompt = String.Empty;
        public string Prompt
        {
            get => _prompt;
            set
            {
                _prompt = value;
                RaisePropertyChanged();
            }
        }

        private string _yesText = "Yes";
        public string YesText
        {
            get => _yesText;
            set
            {
                _yesText = value;
                RaisePropertyChanged();
            }
        }

        private string _noText = "No";
        public string NoText
        {
            get => _noText;
            set
            {
                _noText = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand YesCommand { get; private set; }
        public IAsyncCommand NoCommand { get; private set; }

        private void RegisterCommands()
        {
            this.YesCommand = new AsyncCommand(OnYesCommand);
            this.NoCommand = new AsyncCommand(OnNoCommand);
        }

        private async Task OnYesCommand()
        {
            await Task.Run(YesCallback);
        }
        private async Task OnNoCommand()
        {
            await Task.Run(NoCallback);
        }
        #endregion

        #region "Callbacks"
        public Action YesCallback { get; set; } = () => { };
        public Action NoCallback { get; set; } = () => { };
        #endregion

        private Task CloseDialogAsync()
        {
            base.MainVM.CurrentDialog = null;
            return Task.CompletedTask;
        }
    }
}
