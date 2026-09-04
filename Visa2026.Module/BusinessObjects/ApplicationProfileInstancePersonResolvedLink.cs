using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Sticky auto-resolved person-related record for one (instance, person) pair.
/// Keys are <see cref="ApplicationProfileInstanceId"/> + <see cref="PersonId"/> (no roster-line BO).
/// </summary>
[Table("ApplicationProfileInstancePersonResolvedLinks")]
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationProfileInstancePersonResolvedLink : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationProfileInstanceId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileInstanceId))]
    public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid PersonId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(PersonId))]
    public virtual Person Person { get; set; } = null!;

    [RuleRequiredField]
    public virtual ApplicationProfileInstancePersonLinkKind? LinkKind { get; set; }

    [RuleRequiredField]
    public virtual Guid? LinkedObjectId { get; set; }
}

public enum ApplicationProfileInstancePersonLinkKind
{
    Passport = 0,
    Visa = 1,
    Education = 2,
    AddressOfResidence = 3,
    Position = 4,
    Salary = 5,
    MedicalRecord = 6,
    InvitationItem = 7,
    WorkPermitItem = 8,
    BorderZoneItem = 9,
    RejectionItem = 10,
    TravelHistory = 11,
    WorkDuty = 12,
}
