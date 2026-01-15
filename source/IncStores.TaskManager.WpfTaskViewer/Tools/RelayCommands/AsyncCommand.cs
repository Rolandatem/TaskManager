using IncStores.TaskManager.WpfTaskViewer.Tools.ExtensionMethods;
using IncStores.TaskManager.WpfTaskViewer.Tools.General;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands
{
    //-- https://johnthiriet.com/mvvm-going-async-with-async-command/

    public interface IAsyncCommand : ICommand
    {
        Task ExecuteAsync();
        bool CanExecute();
    }

    public interface IAsyncCommand<T> : ICommand
    {
        Task ExecuteAsync(T parameter);
        bool CanExecute(T parameter);
    }

    internal class AsyncCommand : IAsyncCommand
    {
        #region "Member Variables"
        bool _isExecuting = false;
        readonly Func<Task> _execute = null;
        readonly Func<bool> _canExecute = null;
        readonly IErrorHandler _errorHandler = null;
        #endregion

        #region "Events"
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
        #endregion

        #region "Constructor"
        public AsyncCommand(
            Func<Task> execute,
            Func<bool> canExecute = null,
            IErrorHandler errorHandler = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }
        #endregion

        public void RaiseCanExecuteChanged() => CommandManager.InvalidateRequerySuggested();
        public bool CanExecute() => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async Task ExecuteAsync()
        {
            if (CanExecute())
            {
                try
                {
                    _isExecuting = true;
                    await _execute();
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        #region "Explicit Implementations - for MVVM Binding"
        bool ICommand.CanExecute(object parameter) => CanExecute();
        void ICommand.Execute(object parameter) => ExecuteAsync().FireAndForgetSafeAsync(_errorHandler);
        #endregion
    }

    internal class AsyncCommand<T> : IAsyncCommand<T>
    {
        #region "Member Variables"
        bool _isExecuting = false;
        readonly Func<T, Task> _execute = null;
        readonly Func<T, bool> _canExecute = null;
        readonly IErrorHandler _errorHandler = null;
        #endregion

        #region "Events"
        public event EventHandler CanExecuteChanged;
        #endregion

        #region "Constructor"
        public AsyncCommand(
            Func<T, Task> execute,
            Func<T, bool> canExecute = null,
            IErrorHandler errorHandler = null)
        {
            _execute = execute;
            _canExecute = canExecute;
            _errorHandler = errorHandler;
        }
        #endregion

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        public bool CanExecute(T parameter) => !_isExecuting == false && (_canExecute?.Invoke(parameter) ?? true);

        public async Task ExecuteAsync(T parameter)
        {
            if (CanExecute(parameter))
            {
                try
                {
                    _isExecuting = true;
                    await _execute(parameter);
                }
                finally
                {
                    _isExecuting = false;
                }
            }

            RaiseCanExecuteChanged();
        }

        #region "Explicit Implementation - for MVVM Binding"
        bool ICommand.CanExecute(object parameter) => CanExecute((T)parameter);
        void ICommand.Execute(object parameter) => ExecuteAsync((T)parameter).FireAndForgetSafeAsync(_errorHandler);
        #endregion
    }
}
