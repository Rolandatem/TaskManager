using Incstores.Common.Settings;
using IncStores.TaskManager.Core.Tools;
using IncStores.TaskManager.RecipeRunnerService.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.Data;
using System.Threading.Tasks;

namespace IncStores.TaskManager.WindowsServiceHost.Tools
{
    public class ServiceAuditHelper : IAuditHelper
    {
        #region "Member Variables"
        readonly IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> _taskManagerRecipeHub = null;
        readonly IHostEnvironment _hostEnvironment = null;
        readonly ConnectionStrings _connectionStrings = null;
        #endregion

        #region "Constructor"
        public ServiceAuditHelper(
            IHubContext<TaskManagerRecipeHub, ITaskManagerRecipeHub> taskManagerRecipeHub,
            IHostEnvironment hostEnvironment,
            IOptions<ConnectionStrings> connectionStrings)
        {
            _taskManagerRecipeHub = taskManagerRecipeHub;
            _hostEnvironment = hostEnvironment;
            _connectionStrings = connectionStrings.Value;
        }
        #endregion

        #region "Private Methods"
        private async Task RecordAuditAsync(string message, string initiator, string groupKey, DateTime? auditDateTime, string additionalAuditData)
        {
            using SqlConnection con = new SqlConnection(_connectionStrings.InternalToolsDB);
            con.Open();
            using SqlCommand cmd = con.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = @"
                INSERT INTO AuditHistory
                    (ApplicationName, Environment, GroupKey, Message, AuditDateTime, Initiator, AdditionalAuditData, CreatedBy, CreatedDate)
                VALUES
                    ('TaskManager', @Environment, @GroupKey, @Message, @AuditDateTime, @Initiator, @AdditionalAuditData, @CreatedBy, GETDATE())";
            cmd.Parameters.AddRange(new SqlParameter[]
            {
                new SqlParameter("@Environment", _hostEnvironment.EnvironmentName),
                new SqlParameter("@GroupKey", groupKey),
                new SqlParameter("@Message", message),
                new SqlParameter("@AuditDateTime", auditDateTime ?? DateTime.Now),
                new SqlParameter("@Initiator", initiator),
                new SqlParameter("@AdditionalAuditData", additionalAuditData ?? String.Empty),
                new SqlParameter("@CreatedBy", Environment.UserName)
            });
            await cmd.ExecuteNonQueryAsync();
        }
        #endregion

        public async Task AddAuditAsync(string message, string initiator, string groupKey = null, DateTime? auditDateTime = null, string additionalAuditData = null)
        {
            await _taskManagerRecipeHub.Clients.All.OnAuditLogEntryAsync(
                message,
                initiator,
                groupKey: groupKey,
                auditDateTime: auditDateTime ?? DateTime.Now);

            await RecordAuditAsync(message, initiator, groupKey, auditDateTime, additionalAuditData);
        }
    }
}
