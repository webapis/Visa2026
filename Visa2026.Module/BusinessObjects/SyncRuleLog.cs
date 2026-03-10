using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    public enum SyncRuleLogStatus
    {
        Info,
        Success,
        Warning,
        Error
    }

    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(Date))]
    public class SyncRuleLog : BaseObject
    {
        public virtual DateTime Date { get; set; } = DateTime.Now;

        [RuleRequiredField]
        public virtual SyncRule SyncRule { get; set; }

        public virtual SyncRuleLogStatus Status { get; set; }

        [MaxLength(255)]
        public virtual string SourceObjectId { get; set; }

        [FieldSize(DevExpress.Xpo.SizeAttribute.Unlimited)]
        public virtual string Message { get; set; }

        [FieldSize(DevExpress.Xpo.SizeAttribute.Unlimited)]
        public virtual string Details { get; set; }
    }
}