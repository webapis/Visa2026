using System.ComponentModel;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant singleton for Work Permit kota oranımız: local headcount and target ORAN.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Configuration")]
[DisplayName("Work permit quota")]
[ModelDefault("Caption", "Work permit quota")]
public class WorkPermitQuotaSettings : BaseObject
{
    public const int DefaultForeignRatio = 10;
    public const int DefaultLocalRatio = 90;
    public const int DefaultLocalPersonnelCount = 0;

    [XafDisplayName("Mevcut Lokal personel")]
    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThanOrEqual, 0)]
    public virtual int LocalPersonnelCount { get; set; }

    [XafDisplayName("ORAN YABANCI")]
    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int ForeignRatio { get; set; }

    [XafDisplayName("ORAN YEREL")]
    [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
    public virtual int LocalRatio { get; set; }

    public override void OnCreated()
    {
        base.OnCreated();
        LocalPersonnelCount = DefaultLocalPersonnelCount;
        ForeignRatio = DefaultForeignRatio;
        LocalRatio = DefaultLocalRatio;
    }

    public static WorkPermitQuotaSettings? TryGetInstance(IObjectSpace objectSpace) =>
        OrganizationSingletonHelper.TryGet(objectSpace, (WorkPermitQuotaSettings _) => "Work permit quota");

    public static WorkPermitQuotaSettings GetOrCreateInstance(IObjectSpace objectSpace) =>
        TryGetInstance(objectSpace) ?? objectSpace.CreateObject<WorkPermitQuotaSettings>();
}