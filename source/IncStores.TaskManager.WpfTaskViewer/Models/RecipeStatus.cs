using IncStores.TaskManager.Core.Events.Models;
using IncStores.TaskManager.Core.ViewModels;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WpfTaskViewer.Models
{
    public class RecipeStatus : NotifiableClass
    {
        #region "Constructors"
        public RecipeStatus() { }
        public RecipeStatus(int recipeId, int progress, bool isIndeterminate = false)
        {
            this.RecipeId = recipeId;
            this.Progress = progress;
            this.IsIndeterminate = IsIndeterminate;
        }
        public RecipeStatus(string recipeName, int recipeId, int progress, bool isIndeterminate = false)
            : this(recipeId, progress, isIndeterminate)
        {
            this.RecipeName = recipeName;
        }
        #endregion

        #region "Form Properties"
        private string _recipeName = "Recipe";
        public string RecipeName
        {
            get => _recipeName;
            set
            {
                _recipeName = value;
                RaisePropertyChanged();
            }
        }

        private int _recipeId = 0;
        public int RecipeId
        {
            get => _recipeId;
            set
            {
                _recipeId = value;
                RaisePropertyChanged();
            }
        }

        private int _progress = 0;
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                RaisePropertyChanged();
            }
        }

        private bool _isIndeterminate = false;
        public bool IsIndeterminate
        {
            get => _isIndeterminate;
            set
            {
                _isIndeterminate = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<TaskStatus> _taskStatuses = new ObservableCollection<TaskStatus>();
        public ObservableCollection<TaskStatus> TaskStatuses
        {
            get => _taskStatuses;
            set
            {
                _taskStatuses = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region "Public Methods"
        public Task UpdateTaskProgressAsync(HeartbeatTaskProgressUpdateEventArgs e)
        {
            TaskStatus taskStatus = this.TaskStatuses
                .FirstOrDefault(task => task.TaskId == e.TaskId);

            if (taskStatus == null)
            {
                taskStatus = new TaskStatus(e.TaskId, 0);
                this.TaskStatuses.Add(taskStatus);
            }

            taskStatus.Progress = e.Progress;
            return Task.CompletedTask;
        }
        #endregion
    }
}
