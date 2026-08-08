using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Links a <see cref="Person"/> to an <see cref="Application"/> roster (replaces per-person <see cref="ApplicationItem"/> long-term).
/// Officers link/unlink people only; child BO links are auto-resolved into <see cref="ResolvedLinks"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationPerson : BaseObject
{
    public ApplicationPerson()
    {
        ResolvedLinks = new ObservableCollection<ApplicationPersonResolvedLink>();
    }

    [Browsable(false)]
    public virtual Guid ApplicationId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationId))]
    public virtual Application Application { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid PersonId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(PersonId))]
    public virtual Person Person { get; set; } = null!;

    [XafDisplayName("Linked at")]
    public virtual DateTime LinkedAt { get; set; } = DateTime.Now;

    [Aggregated]
    [InverseProperty(nameof(ApplicationPersonResolvedLink.ApplicationPerson))]
    public virtual IList<ApplicationPersonResolvedLink> ResolvedLinks { get; set; }
}

/// <summary>Auto-resolved person-related record linked to an <see cref="ApplicationPerson"/> row.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationPersonResolvedLink : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationPersonId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationPersonId))]
    public virtual ApplicationPerson ApplicationPerson { get; set; } = null!;

    [RuleRequiredField]
    public virtual ApplicationPersonLinkKind? LinkKind { get; set; }

    [RuleRequiredField]
    public virtual Guid? LinkedObjectId { get; set; }
}

public enum ApplicationPersonLinkKind
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
}
