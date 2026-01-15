using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Interfaces
{
    public interface IRecipeRunnerTool
    {
        CancellationTokenSource CancellationTokenSource { get; set; }
        ManualResetEventSlim ResetFlag { get; set; }

        Task StartAsync();
        Task StopAsync();
    }
}
