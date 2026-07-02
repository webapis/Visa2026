using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Immutable ministry-leg snapshot copied from <see cref="ApprovalLegProfile.MinistryLegs"/>
/// (or legacy <see cref="ProjectContract.MinistryLegs"/> until Phase 5) when an
/// <see cref="Application"/> selects an approval profile.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(MinistryShortName))]
public class ApplicationApprovalLegSnapshot : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationId { get; set; }

    [Browsable(false)]
    public virtual Application Application { get; set; } = null!;

    [RuleRequiredField]
    [XafDisplayName("Tertip")]
    public virtual int? Sequence { get; set; }

    [Browsable(false)]
    public virtual Guid? ApprovingMinistryId { get; set; }

    [RuleRequiredField]
    [MaxLength(40)]
    [XafDisplayName("Gysga ady")]
    public virtual string MinistryShortName { get; set; } = string.Empty;

    [MaxLength(200)]
    [Browsable(false)]
    public virtual string MinistryNameTm { get; set; }

    [XafDisplayName("Maks. iş günleri")]
    public virtual int? MaxDaysInReview { get; set; }

    [XafDisplayName("Duýduryş (iş günleri)")]
    public virtual int? WarningDaysBeforeMax { get; set; }
}
