using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>One ordered ministry leg on a <see cref="ProjectContract"/> approval process.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Sequence))]
public class ProjectContractMinistryLeg : BaseObject
{
    [Browsable(false)]
    public virtual Guid ProjectContractId { get; set; }

    [Browsable(false)]
    [ForeignKey(nameof(ProjectContractId))]
    public virtual ProjectContract ProjectContract { get; set; } = null!;

    [RuleRequiredField]
    public virtual int? Sequence { get; set; }

    [RuleRequiredField]
    public virtual ApprovingMinistry ApprovingMinistry { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ApprovingMinistryId { get; set; }

    /// <summary>Max working days allowed in <c>{n}_REVIEW_STARTED</c> for this leg (required before contract is active).</summary>
    [XafDisplayName("Maks. iş günleri")]
    public virtual int? MaxDaysInReview { get; set; }

    /// <summary>Optional early warning when working days in review exceed this value (must be &lt; <see cref="MaxDaysInReview"/>).</summary>
    [XafDisplayName("Duýduryş (iş günleri)")]
    public virtual int? WarningDaysBeforeMax { get; set; }

    public override void OnSaving()
    {
        SyncForeignKeys();
        base.OnSaving();
    }

    /// <summary>EF persists explicit FK scalars; XAF may set navigations only.</summary>
    internal void SyncForeignKeys()
    {
        if (ProjectContract != null)
            ProjectContractId = ProjectContract.ID;

        if (ApprovingMinistry != null)
            ApprovingMinistryId = ApprovingMinistry.ID;
    }
}
