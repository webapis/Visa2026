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

    /// <summary>Legacy per-leg SLA — use <see cref="ApplicationProfile.MinistrySlaDays"/> (profile Process &amp; SLA).</summary>
    [Obsolete("Ministry review SLA is configured on ApplicationProfile.MinistrySlaDays. Retained for DB/import compatibility.")]
    [Browsable(false)]
    public virtual int? MaxDaysInReview { get; set; }

    /// <summary>Legacy per-leg SLA — use <see cref="ApplicationProfile.MinistrySlaDays"/> (profile Process &amp; SLA).</summary>
    [Obsolete("Ministry review SLA is configured on ApplicationProfile.MinistrySlaDays. Retained for DB/import compatibility.")]
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

        // Guid.Empty is a real FK value. Blanking it while the parent is still new
        // makes EF insert 00000000-... and Postgres rejects the ministry-leg row.
        // Use the parent client id (XAF assigns it on CreateObject) so parent + legs
        // in the same commit share one key. Nested popup saves still redirect when
        // the parent is not in the batch (WouldOrphan / SaveBeforeMinistryLeg).
        ApprovalLegProfileId = ApprovalLegProfile.ID;
    }
}
