using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(StateCode))]
    public class BoStateSnapshot : BaseObject
    {
        [RuleRequiredField]
        public virtual string OwnerType { get; set; }

        public virtual Guid OwnerId { get; set; }

        [RuleRequiredField]
        public virtual string StateCode { get; set; }

        [RuleRequiredField]
        public virtual string Severity { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string Reason { get; set; }

        [RuleRequiredField]
        public virtual string RuleVersion { get; set; }

        public virtual DateTime EvaluatedAtUtc { get; set; } = DateTime.UtcNow;

        [Browsable(false)]
        public virtual DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        [Browsable(false)]
        public virtual DateTime? UpdatedOnUtc { get; set; }
    }
}
