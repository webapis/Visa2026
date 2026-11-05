using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Case-person requested work-permit / border-zone location (comma-separated).
/// One row per (instance, person, kind); Join upserts. Does not mutate
/// <see cref="Visa"/>, <see cref="WorkPermitItem"/>, or <see cref="BorderZoneItem"/>.
/// </summary>
[Table("ApplicationProfileInstancePersonRequestedLocations")]
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationProfileInstancePersonRequestedLocation : BaseObject
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
    public virtual ApplicationProfileInstancePersonRequestedLocationKind? Kind { get; set; }

    /// <summary>Comma-separated location tokens from Linked-records Join.</summary>
    [StringLength(500)]
    public virtual string? Locations { get; set; }
}

public enum ApplicationProfileInstancePersonRequestedLocationKind
{
    WorkPermit = 0,
    BorderZone = 1,
}