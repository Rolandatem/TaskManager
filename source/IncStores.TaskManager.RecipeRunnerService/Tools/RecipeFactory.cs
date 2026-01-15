using IncStores.TaskManager.Core.Enumerations;
using IncStores.TaskManager.Core.Recipes.Interfaces;
using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.DataLayer.Models.InternalTools;
using IncStores.TaskManager.DataLayer.UnitsOfWork.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace IncStores.TaskManager.RecipeRunnerService.Tools
{
    public interface IRecipeFactory
    {
        Task<IRecipe> CreateRecipeFromRequestAsync(TaskRecipeQueueItem recipeQueueItem, IServiceScope scope);
        Task SetAsync(TaskRecipeQueueItem recipe, TaskStatusTypeEnum newStatus, string initiator);
    }

    internal class RecipeFactory : IRecipeFactory
    {
        #region "Member Variables"
        readonly IServiceProvider _serviceProvider = null;
        readonly ILogger<RecipeFactory> _logger = null;
        readonly IAuditHelper _auditHelper = null;
        #endregion

        #region "Constructor"
        public RecipeFactory(
            IServiceProvider serviceProvider,
            ILogger<RecipeFactory> logger,
            IAuditHelper auditHelper)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _auditHelper = auditHelper;
        }
        #endregion

        public async Task<IRecipe> CreateRecipeFromRequestAsync(TaskRecipeQueueItem recipeQueueItem, IServiceScope scope)
        {
            string recipeLabel = $"{recipeQueueItem.ID}:{recipeQueueItem.TaskRecipeType.StringKey}";

            try
            {
                //--Since recipes will be able to come from nuget packages, namespaces may be different
                //--between recipes.
                //IRecipe recipe = _serviceProvider
                IRecipe recipe = scope.ServiceProvider
                    .GetServices<IRecipe>()
                    .FirstOrDefault(item =>
                    {
                        string type = item.GetType().ToString();
                        int idx = type.LastIndexOf(".");
                        return type.Substring(idx + 1, (type.Length - idx) - 1).ToLower() ==
                            recipeQueueItem.TaskRecipeType.StringKey.ToLower();
                    });

                if (recipe == null) { throw new Exception("Unknown recipe."); }

                recipe.InputData = recipeQueueItem.Data;
                recipe.ID = recipeQueueItem.ID;
                await SetAsync(recipeQueueItem, TaskStatusTypeEnum.Working, "RecipeFactory - Create");
                await _auditHelper.AddAuditAsync($"Recipe Factory created worker Recipe for {recipeLabel}.", "RecipeFactory - Create", "SYSTEM");

                return recipe;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await _auditHelper.AddAuditAsync($"Could not create recipe for: {recipeLabel}", "RecipeFactory - Create", "SYSTEM");
                throw;
            }
        }

        public async Task SetAsync(TaskRecipeQueueItem recipe, TaskStatusTypeEnum newStatus, string initiator)
        {
            string recipeLabel = $"{recipe.ID}:{recipe.TaskRecipeType.StringKey}";

            try
            {
                using IServiceScope scope = _serviceProvider.CreateScope();
                using ICommonInternalToolsUnitOfWork internalTools = scope.ServiceProvider.GetService<ICommonInternalToolsUnitOfWork>();
                TaskStatusType newStatusType = await internalTools.TaskStatusTypes.GetTaskStatusTypeByEnumAsync(newStatus);
                await internalTools.TaskRecipeQueueList.SetStatusAsync(recipe.ID, newStatusType);
                await internalTools.CompleteAsync();
                await _auditHelper.AddAuditAsync($"Updated the status of Task Recipe Queue Item [{recipeLabel}] to {newStatus}", initiator, "SYSTEM");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                await _auditHelper.AddAuditAsync($"Could not set Recipe status for Recipe {recipeLabel}.{Environment.NewLine}{ex.Message}", initiator, "SYSTEM");
            }
        }
    }
}
