using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DisplayName("Expiration alert rules")]
    [DefaultProperty(nameof(DisplayName))]
    [ImageName("BO_Scheduler")]
    public class ExpirationAlertRule : BaseObject
    {
        public const int DefaultExpiringSoonDays = 30;
        public const int DefaultExtensionApplicationRequiredDays = 90;

        [RuleRequiredField]
        [RuleUniqueValue]
        [ModelDefault("AllowEdit", "False")]
        public virtual string BusinessObjectKey { get; set; }

        [RuleRequiredField]
        public virtual string DisplayName { get; set; }

        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        [ToolTip("Number of days before ExpirationDate when the record enters the Expiring state.")]
        public virtual int ExpiringSoonDays { get; set; } = DefaultExpiringSoonDays;

        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0,
            TargetCriteria = "ExtensionApplicationRequiredDays Is Not Null")]
        [ToolTip("Optional. When set, extension application should start within this many days before expiration (Visa, WorkPermitItem).")]
        public virtual int? ExtensionApplicationRequiredDays { get; set; }
    }
}
