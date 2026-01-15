using IncStores.TaskManager.DataLayer.DTOs.IncStores;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using IncStores.TaskManager.WpfTaskViewer.Tools.RelayCommands;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.Main
{
    public interface ILoginViewModel
    {
        #region "Properties"
        string UserName { get; set; }
        string Password { get; set; }
        string LoginMessage { get; set; }

        Action<string> UpdateUIPassword { get; set; }
        #endregion

        #region "Public Methods"
        void CheckUserSecretsForLogin();
        #endregion

        #region "Relay Command"
        IAsyncCommand LoginCommand { get; }
        #endregion
    }

    internal class LoginViewModelDesign : ILoginViewModel
    {
        #region "Properties"
        public string UserName { get; set; } = "[USER_NAME]";
        public string Password { get; set; } = "[PASSWORD]";
        public string LoginMessage { get; set; } = "[LOGIN_MESSAGE]";
        
        public Action<string> UpdateUIPassword { get; set; }
        #endregion

        #region "Public Methods"
        public void CheckUserSecretsForLogin() { }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand LoginCommand { get; }
        #endregion
    }

    internal class LoginViewModel : BaseViewModel, ILoginViewModel
    {
        #region "Member Variables"
        readonly ICommonIncStoresUnitOfWork _incStores = null;
        readonly IConfiguration _config = null;
        #endregion

        #region "Constructor"
        public LoginViewModel(
            IServiceProvider serviceProvider,
            ICommonIncStoresUnitOfWork incStores,
            IConfiguration config)
            : base(serviceProvider) 
        {
            _incStores = incStores;
            _config = config;

            RegisterCommands();
        }
        #endregion

        #region "Form Properties"
        private string _userName = String.Empty;
        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                RaisePropertyChanged();
            }
        }

        private string _loginMessage = String.Empty;
        public string LoginMessage
        {
            get => _loginMessage;
            set
            {
                _loginMessage = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Private Properties"
        private bool CanLoginAttempt()
        {
            return String.IsNullOrWhiteSpace(this.UserName) == false &&
                String.IsNullOrWhiteSpace(this.Password) == false;
        }
        #endregion

        #region "Public Properties"
        public Action<string> UpdateUIPassword { get; set; }
        public string Password { get; set; }
        #endregion

        #region "Public Methods"
        public void CheckUserSecretsForLogin()
        {
            this.UserName = _config["UserLogin:UserName"];
            this.UpdateUIPassword(_config["UserLogin:Password"]);
        }
        #endregion

        #region "Relay Commands"
        public IAsyncCommand LoginCommand { get; private set; }

        private void RegisterCommands()
        {
            this.LoginCommand = new AsyncCommand(OnLoginCommand, CanLoginAttempt);
        }
        private async Task OnLoginCommand()
        {
            try
            {
                base.FormIsBusy = true;

                AppUser appUser = await _incStores.Users.ValidateLogin(this.UserName, this.Password);
                if (appUser == null)
                { this.LoginMessage = "Invalid User Name or Password!"; }
                else
                {
                    await base.MainVM.LoginSuccessAsync(appUser);
                }
            }
            catch (Exception ex)
            {
                await base.ShowErrorDialogAsync(ex);
            }
            finally
            {
                this.FormIsBusy = false;
            }
        }
        #endregion
    }
}
