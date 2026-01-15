using System;

namespace IncStores.TaskManager.WpfTaskViewer.Models
{
    public class AuditLogItem
    {
        #region "Constructors"
        public AuditLogItem() { }
        public AuditLogItem(string message, string initiator, string groupKey, DateTime? auditDateTime)
        {
            this.Initiator = initiator;
            this.Message = message;
            this.GroupKey = groupKey;
            this.AuditDateTime = auditDateTime;
        }
        #endregion

        #region "Public Properties"
        public DateTime? AuditDateTime { get; set; }
        public string Initiator { get; set; }
        public string Message { get; set; }
        public string GroupKey { get; set; }
        #endregion
    }
}
