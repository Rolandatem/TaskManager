using IncStores.TaskManager.DataLayer.DTOs.IncStores;
using IncStores.TaskManager.WpfTaskViewer.ViewModels.Common;
using System;

namespace IncStores.TaskManager.WpfTaskViewer.ViewModels.SystemTray
{
    public interface ISystemTrayPopupViewModel
    {
        #region "Properties"
        AppUser AppUser { get; }
        ISharedCommunicatorViewModel SharedHubCommunicator { get; }
        #endregion
    }

    internal class SystemTrayPopupViewModelDesign : ISystemTrayPopupViewModel
    {
        #region "Properties"
        public AppUser AppUser { get; }
        public ISharedCommunicatorViewModel SharedHubCommunicator { get; }
        #endregion
    }

    internal class SystemTrayPopupViewModel : BaseViewModel, ISystemTrayPopupViewModel
    {
        #region "Member Variables"
        readonly ISharedCommunicatorViewModel _sharedHubCommunicator = null;
        readonly AppUser _appUser = null;
        #endregion

        #region "Constructor"
        public SystemTrayPopupViewModel(
            IServiceProvider serviceProvider,
            AppUser appUser,
            ISharedCommunicatorViewModel sharedHubCommunicator)
            : base(serviceProvider)
        {
            _appUser = appUser;
            _sharedHubCommunicator = sharedHubCommunicator;
        }
        #endregion

        #region "Form Properties"
        public AppUser AppUser => _appUser;
        public ISharedCommunicatorViewModel SharedHubCommunicator => _sharedHubCommunicator;
        #endregion
    }
}
