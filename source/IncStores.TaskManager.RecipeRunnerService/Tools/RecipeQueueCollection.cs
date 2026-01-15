using IncStores.TaskManager.DataLayer.Models.InternalTools;
using Nito.AsyncEx;
using System.Collections.Concurrent;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IRecipeQueueCollection
    {
        AsyncCollection<TaskRecipeQueueItem> RecipeList { get; }
        ConcurrentDictionary<int, int> QueuedRecipeIdList { get; }
    }

    internal class RecipeQueueCollection : IRecipeQueueCollection
    {
        #region "Public Properties"
        public AsyncCollection<TaskRecipeQueueItem> RecipeList { get; } = new AsyncCollection<TaskRecipeQueueItem>(new ConcurrentQueue<TaskRecipeQueueItem>());
        public ConcurrentDictionary<int, int> QueuedRecipeIdList { get; } = new ConcurrentDictionary<int, int>();
        #endregion
    }
}
