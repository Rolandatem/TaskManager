using IncStores.TaskManager.Core.ViewModels;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Common;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Main;
using IncStores.TaskManager.WpfTaskViewer.Views.Common;
using IncStores.TaskManager.WpfTaskViewer.Views.Main;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels
{
    internal abstract class BaseViewModel : NotifiableClass
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        IMainViewModel _mainVM = null;
        MainView _mainView = null;
        #endregion

        #region "Private Properties"
        private MainView MainView
        {
            get
            {
                if (_mainView == null)
                { _mainView = _serviceProvider.GetService<MainView>(); }

                return _mainView;
            }
        }
        #endregion

        #region "Public Properties"
        public Application App { get => Application.Current; }
        public Task Init { get; set; }
        public bool FormIsBusy
        {
            set => this.MainVM.MainFormIsBusy = value;
        }
        public IMainViewModel MainVM
        {
            get
            {
                if (_mainVM == null)
                {
                    _mainVM = _serviceProvider.GetService<IMainViewModel>();
                }

                return _mainVM;
            }
        }
        #endregion

        #region "Constructor"
        public BaseViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        #endregion

        #region "Methods"
        public virtual Task StopAsync() => Task.CompletedTask;
        public Task ShowInfoDialogAsync(string message, string title = "Info")
        {
            IInfoDialogViewModel vm = _serviceProvider.GetService<IInfoDialogViewModel>();
            vm.Message = message;
            vm.Title = title;
            InfoDialogView view = new InfoDialogView()
            {
                DataContext = vm
            };
            this.MainVM.CurrentDialog = view;
            return Task.CompletedTask;
        }
        public Task ShowErrorDialogAsync(Exception ex, string title = "Error")
        {
            IErrorDialogViewModel vm = _serviceProvider.GetService<IErrorDialogViewModel>();
            vm.ErrorException = ex;
            vm.Title = title;
            ErrorDialogView view = new ErrorDialogView()
            {
                DataContext = vm
            };
            this.MainVM.CurrentDialog = view;
            return Task.CompletedTask;
        }
        public Task ShowPromptDialogAsync(string prompt, string yesText = "Yes", string noText = "No", Action yesCallback = null, Action noCallback = null)
        {
            IPromptDialogViewModel vm = _serviceProvider.GetService<IPromptDialogViewModel>();
            vm.Prompt = prompt;
            vm.YesText = yesText;
            vm.NoText = noText;
            vm.YesCallback = () =>
            {
                yesCallback?.Invoke();
                this.MainVM.CurrentDialog = null;
            };
            vm.NoCallback = () =>
            {
                noCallback?.Invoke();
                this.MainVM.CurrentDialog = null;
            };
            PromptDialogView view = new PromptDialogView()
            {
                DataContext = vm
            };
            this.MainVM.CurrentDialog = view;
            return Task.CompletedTask;
        }
        public async Task LoadInterfaceAsync<T>(Action<object> predicate = null) where T: UserControl
        {
            try
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    T view = _serviceProvider.GetService<T>();

                    //--Stop the current interface
                    if (this.MainVM.CurrentInterface.DataContext is BaseViewModel)
                    {
                        await ((BaseViewModel)this.MainVM.CurrentInterface.DataContext).StopAsync();
                    }

                    //--Load new interface
                    this.MainVM.CurrentInterface = view;

                    //--Initiate viewmodel if it is a BaseViewModel
                    if (view.DataContext != null && view.DataContext is BaseViewModel)
                    { await ((BaseViewModel)view.DataContext).Init; }
                    
                    predicate?.Invoke(view.DataContext);
                });
            }
            catch (Exception ex)
            {
                await this.ShowErrorDialogAsync(new Exception($"Failed to load screen: {typeof(T).Name}.", ex));
            }
        }
        public void SetTitle(string title) => this.MainVM.WindowTitle = title;
        public void SetStatus(string status) => this.MainVM.StatusText = status;
        #endregion
    }
}
