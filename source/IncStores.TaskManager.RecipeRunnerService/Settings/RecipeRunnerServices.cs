using IncStores.TaskManager.RecipeRunnerService.Models;
using IncStores.TaskManager.RecipeRunnerService.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IncStores.TaskManager.RecipeRunnerService.Settings
{
    public static class RecipeRunnerServices
    {
        public static IServiceCollection AddRecipeRunnerServices(this IServiceCollection services, IConfiguration configuration)
        {
            services

                //--AppSettings JSON
                .Configure<RecipeRunnerSettings>(configuration.GetSection("recipeRunnerSettings"))

                //--Tools
                .AddSingleton<Overlord>()
                .AddSingleton<IGeneralTools, GeneralTools>()
                .AddSingleton<IRecipeQueueWatcher, RecipeQueueWatcher>()
                .AddSingleton<IRecipeQueueRunnerCollection, RecipeQueueWorkerCollection>()
                .AddTransient<IRecipeQueueWorker, RecipeQueueWorker>()
                .AddSingleton<IRecipeFactory, RecipeFactory>()
                .AddSingleton<IRecipeScheduler, RecipeScheduler>()
                .AddSingleton<IRecipeQueueCollection, RecipeQueueCollection>()
                .AddSingleton<IMaintenanceWindow, MaintenanceWindow>()
                .AddSingleton<ISignalRServerPinger, SignalRServerPinger>()
                .AddSingleton<IHealthMonitor, HealthMonitor>();

            return services;
        }
    }
}
