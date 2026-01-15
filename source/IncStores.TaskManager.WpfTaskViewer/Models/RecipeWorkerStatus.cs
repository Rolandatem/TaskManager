using IncStores.TaskManager.Core.Events.Models;
using IncStores.TaskManager.Core.ViewModels;
using System;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.Models
{
    public class RecipeWorkerStatus : NotifiableClass
    {
        #region "Constructors"
        public RecipeWorkerStatus() { }
        public RecipeWorkerStatus(int workerNumber)
        {
            this.WorkerNumber = workerNumber;
        }
        #endregion

        #region "Form Properties"
        private int _workerNumber = 0;
        public int WorkerNumber
        {
            get => _workerNumber;
            set
            {
                _workerNumber = value;
                RaisePropertyChanged();
            }
        }

        private string _workerStatus = String.Empty;
        public string WorkerStatus
        {
            get => this.RecipeStatus != null
                ? "Working"
                : "Idle";
        }

        private RecipeStatus _recipeStatus = null;
        public RecipeStatus RecipeStatus
        {
            get => _recipeStatus;
            set
            {
                _recipeStatus = value;
                RaisePropertyChanged();
                RaisePropertyChanged("WorkerStatus");
            }
        }
        #endregion

        #region "Public Methods"
        public Task UpdateRecipeProgressAsync(HeartbeatRecipeProgressUpdateEventArgs e)
        {
            if (this.RecipeStatus == null)
            {
                this.RecipeStatus = new RecipeStatus()
                {
                    RecipeId = e.RecipeId,
                    Progress = 0
                };
            }

            //--Since a status update has been made, this is not indeterminate, so change
            //--the value in case it was originally set (i.e. recipes that do not have
            //--progress update code).
            this.RecipeStatus.IsIndeterminate = false;

            this.RecipeStatus.Progress = e.Progress;
            return Task.CompletedTask;
        }
        #endregion
    }
}
