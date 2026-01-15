namespace IncStores.TaskManager.RecipeRunnerService.Models
{
    public class RecipeRunnerSettings
    {
        public int RecipeWorkerLimit { get; set; }
        public int RecipeWatcherMillisecondsWaitInterval { get; set; }
        public int SchedulerMillisecondsWaitInterval { get; set; }
        public bool RunScheduler { get; set; }
        public bool RunMaintenanceWindow { get; set; }
    }
}
