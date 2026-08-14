using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant singleton: working-day SLA for ministry review legs (elapsed from prior step until <c>{n}_REVIEW_APPROVED</c>).
/// Copied to <see cref="ApplicationProfileInstanceApprovalLegSnapshot"/> when an application selects an
/// <see cref="ApprovalLegProfile"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Configuration")]
[DisplayName("Ministry review SLA")]
[RuleCriteria(
    "MinistryReviewSlaSettingsWarningBeforeMax",
    DefaultContexts.Save,
    "WarningDaysBeforeMax is null OR WarningDaysBeforeMax < MaxDaysInReview",
    CustomMessageTemplate = "Warning days must be less than the maximum review days.")]
public class MinistryReviewSlaSettings : BaseObject
{
    public const int DefaultMaxDaysInReview = 4;
    public const int DefaultWarningDaysBeforeMax = 1;

    /// <summary>Max working days allowed per ministry leg (from prior step until approval).</summary>
    [XafDisplayName("Max working days")]
    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int MaxDaysInReview { get; set; }

    /// <summary>Optional early warning when working days in review exceed this value.</summary>
    [XafDisplayName("Warning (working days)")]
    public virtual int? WarningDaysBeforeMax { get; set; }

    public override void OnCreated()
    {
        base.OnCreated();
        MaxDaysInReview = DefaultMaxDaysInReview;
        WarningDaysBeforeMax = DefaultWarningDaysBeforeMax;
    }

    public static MinistryReviewSlaSettings? TryGetInstance(IObjectSpace objectSpace) =>
        OrganizationSingletonHelper.TryGet(objectSpace, (MinistryReviewSlaSettings _) => "Ministry review SLA");

    public static MinistryReviewSlaSettings GetOrCreateInstance(IObjectSpace objectSpace) =>
        TryGetInstance(objectSpace) ?? objectSpace.CreateObject<MinistryReviewSlaSettings>();
}
