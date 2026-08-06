using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;
using Visa2026.Module.Documentation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Calendar-day thresholds before <see cref="IExpirationLogic.ExpirationDate"/> for expiring-soon UI states.
    /// Configuration nav lists <see cref="DocumentExpirationAlertConfigurationKeys"/> only; other keys stay seeded for runtime.
    /// </summary>
    [UserDocumentation("administration/configuration/alerts-and-upload-limits", Category = "Administration")]
    [DefaultClassOptions]
    [NavigationItem("Configuration")]
    [DisplayName("Document expiration alerts")]
    [DefaultProperty(nameof(DisplayName))]
    [Appearance(
        "ExpirationAlertRule_HideBusinessObjectKey",
        AppearanceItemType = "ViewItem",
        TargetItems = nameof(BusinessObjectKey),
        Visibility = ViewItemVisibility.Hide,
        Context = "DetailView,ListView,LookupListView")]
    [Appearance(
        "ExpirationAlertRule_HideExtensionDaysUnlessVisaOrWorkPermit",
        AppearanceItemType = "ViewItem",
        TargetItems = nameof(ExtensionApplicationRequiredDays),
        Visibility = ViewItemVisibility.Hide,
        Criteria = "BusinessObjectKey <> 'Visa' And BusinessObjectKey <> 'WorkPermitItem'",
        Context = "DetailView,ListView")]
    [RuleCriteria(
        "ExpirationAlertRuleExtensionOnlyVisaOrWorkPermit",
        DefaultContexts.Save,
        "ExtensionApplicationRequiredDays Is Null Or BusinessObjectKey = 'Visa' Or BusinessObjectKey = 'WorkPermitItem'",
        CustomMessageTemplate = "Extension application days apply only to Visa and Work permit item.")]
    public class ExpirationAlertRule : BaseObject
    {
        public const int DefaultExpiringSoonDays = 30;
        public const int DefaultExtensionApplicationRequiredDays = 90;

        [Browsable(false)]
        [RuleRequiredField]
        [RuleUniqueValue]
        [ModelDefault("AllowEdit", "False")]
        public virtual string BusinessObjectKey { get; set; }

        [RuleRequiredField]
        public virtual string DisplayName { get; set; }

        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        [XafDisplayName("Duýduryş (gün)")]
        [ToolTip("Calendar days before ExpirationDate when the record enters the expiring-soon state.")]
        public virtual int ExpiringSoonDays { get; set; } = DefaultExpiringSoonDays;

        [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0,
            TargetCriteria = "ExtensionApplicationRequiredDays Is Not Null")]
        [XafDisplayName("Uzaltma arzasy (gün)")]
        [ToolTip("Calendar days before expiration when an extension application should be started (Visa and Work permit item only).")]
        public virtual int? ExtensionApplicationRequiredDays { get; set; }
    }
}
