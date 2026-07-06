using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>One ordered ministry leg on an <see cref="ApprovalLegProfile"/>.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Sequence))]
[Appearance(
    "ApprovalLegProfileMinistryLeg_HideLegSlaColumns",
    AppearanceItemType = "ViewItem",
    TargetItems = nameof(MaxDaysInReview) + ";" + nameof(WarningDaysBeforeMax),
    Visibility = ViewItemVisibility.Hide,
    Context = "DetailView,ListView")]
public class ApprovalLegProfileMinistryLeg : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApprovalLegProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApprovalLegProfileId))]
    public virtual ApprovalLegProfile ApprovalLegProfile { get; set; } = null!;

    [RuleRequiredField]
    public virtual int? Sequence { get; set; }

    [RuleRequiredField]
    public virtual ApprovingMinistry ApprovingMinistry { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ApprovingMinistryId { get; set; }

    /// <summary>Legacy per-leg SLA — use <see cref="MinistryReviewSlaSettings"/> (Configuration).</summary>
    [Obsolete("Ministry review SLA is configured globally on MinistryReviewSlaSettings. Retained for DB/import compatibility.")]
    [Browsable(false)]
    public virtual int? MaxDaysInReview { get; set; }

    /// <summary>Legacy per-leg SLA — use <see cref="MinistryReviewSlaSettings"/> (Configuration).</summary>
    [Obsolete("Ministry review SLA is configured globally on MinistryReviewSlaSettings. Retained for DB/import compatibility.")]
    [Browsable(false)]
    public virtual int? WarningDaysBeforeMax { get; set; }

    public override void OnSaving()
    {
        SyncForeignKeys();
        base.OnSaving();
    }

    internal void SyncForeignKeys()
    {
        if (ApprovingMinistry != null)
            ApprovingMinistryId = ApprovingMinistry.ID;

        if (ApprovalLegProfile == null)
            return;

        var objectSpace = ObjectSpaceHelper.Get(ApprovalLegProfile) ?? ObjectSpaceHelper.Get(this);
        if (objectSpace == null || objectSpace.IsNewObject(ApprovalLegProfile))
        {
            ApprovalLegProfileId = Guid.Empty;
            return;
        }

        ApprovalLegProfileId = ApprovalLegProfile.ID;
    }
}
