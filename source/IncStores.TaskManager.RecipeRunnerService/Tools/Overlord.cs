using IncStores.TaskManager.RecipeRunnerService.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public class Overlord
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly IHostApplicationLifetime _hostApplicationLifetime = null;

        List<Task> _runningToolTaskHolder = new List<Task>();
        List<IRecipeRunnerTool> _runningTools = new List<IRecipeRunnerTool>();
        #endregion

        #region "Constructor"
        public Overlord(
            IServiceProvider serviceProvider,
            IHostApplicationLifetime hostApplicationLifetime)
        {
            _serviceProvider = serviceProvider;
            _hostApplicationLifetime = hostApplicationLifetime;

            //--Set application token source
            this.CancellationTokenSource = new CancellationTokenSource();
            this.CancellationTokenSource.Token.Register(async () =>
            {
                await ShutdownSystemToolsAsync(true);

                //--In case an application shutdown is requested internally.
                _hostApplicationLifetime.StopApplication();
            });
        }
        #endregion

        #region "Public Properties"
        public CancellationTokenSource CancellationTokenSource { get; set; } = null;

        public WaitHandle[] AllToolResetFlags => _runningTools
            .Select(tool => tool.ResetFlag.WaitHandle)
            .ToArray();

        public WaitHandle[] NonMaintenanceWindowToolFlags => _runningTools
            .Where(tool => tool is IMaintenanceWindow == false)
            .Select(tool => tool.ResetFlag.WaitHandle)
            .ToArray();
        #endregion

        public async Task StartToolAsync<T>()
        {
            await Task.Yield();

            IRecipeRunnerTool tool = _serviceProvider.GetService<T>() as IRecipeRunnerTool;
            _runningTools.Add(tool);
            _runningToolTaskHolder.Add(tool.StartAsync());
        }

        public async Task StartSystemToolsAsync(bool includeMaintenanceWindow = false)
        {
            await this.StartToolAsync<IRecipeQueueWatcher>();
            await this.StartToolAsync<IRecipeQueueRunnerCollection>();
            await this.StartToolAsync<IRecipeScheduler>();
            await this.StartToolAsync<ISignalRServerPinger>();
            await this.StartToolAsync<IHealthMonitor>();

            if (includeMaintenanceWindow)
            {
                await this.StartToolAsync<IMaintenanceWindow>();
            }
        }

        public async Task ShutdownSystemToolsAsync(bool includeMaintenanceWindow = false)
        {
            await Task.Yield();

            var query = _runningTools;
            if (includeMaintenanceWindow == false)
            {
                query = query
                    .Where(tool => tool is IMaintenanceWindow == false)
                    .ToList();
            }

            //query.ForEach(tool => tool.CancellationTokenSource.Cancel());
            List<Task> shutdownList = query
                .Select(tool => tool.StopAsync())
                .ToList();
            await Task.WhenAll(shutdownList);

            WaitHandle.WaitAll(includeMaintenanceWindow
                ? this.AllToolResetFlags
                : this.NonMaintenanceWindowToolFlags, 10000);

            await this.CleanoutListsAsync();
        }

        public async Task CleanoutListsAsync()
        {
            await Task.Yield();

            _runningTools = _runningTools
                .Where(tool => tool.ResetFlag.IsSet == false)
                .ToList();

            _runningToolTaskHolder = _runningToolTaskHolder
                .Where(task => task.IsCompleted == false)
                .ToList();
        }

        public async Task WaitWhileToolsAreRunningAsync()
        {
            while (_runningToolTaskHolder.Count() > 0)
            {
                //await Task.Yield();
                await Task.Delay(1000, this.CancellationTokenSource.Token);
            }
        }
    }
}
