using IncStores.TaskManager.Core.ViewModels;
using System;

namespace IncStores.TaskManager.WpfTaskViewer.Models
{
    public class TaskStatus : NotifiableClass
    {
        #region "Constructors"
        public TaskStatus() { }
        public TaskStatus(Guid taskId, int progress, bool isIndeterminate = false)
        {
            this.TaskId = taskId;
            this.Progress = progress;
            this.IsIndeterminate = isIndeterminate;
        }
        public TaskStatus(string taskName, Guid taskId, int progress, bool isIndeterminate = false)
            : this(taskId, progress, isIndeterminate)
        {
            this.TaskName = taskName;
        }
        #endregion

        #region "Form Properties"
        private string _taskName = "Recipe Task";
        public string TaskName
        {
            get => _taskName;
            set
            {
                _taskName = value;
                RaisePropertyChanged();
            }
        }

        private Guid _taskId = Guid.Empty;
        public Guid TaskId
        {
            get => _taskId;
            set
            {
                _taskId = value;
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
        #endregion
    }
}
