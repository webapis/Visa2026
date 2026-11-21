using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

public enum ApplicationProfileSavedLocationField
{
    BorderZone = 1,
    WorkPermitLocation = 2,
}

public enum ApplicationProfileSavedLocationSource
{
    Lookup = 0,
    User = 1,
}

/// <summary>
/// Tenant saved Requested border zone / work permit location suggestions (shared across applications).
/// </summary>
[Table("ApplicationProfileSavedLocations")]
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationProfileSavedLocation : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileId))]
    public virtual ApplicationProfile ApplicationProfile { get; set; } = null!;

    public virtual ApplicationProfileSavedLocationField Field { get; set; }

    [Browsable(false)]
    [MaxLength(450)]
    public virtual string TextKey { get; set; } = string.Empty;

    [MaxLength(100)]
    public virtual string? Text { get; set; }

    public virtual int UseCount { get; set; } = 1;

    public virtual DateTime UsedAtUtc { get; set; }

    public virtual ApplicationProfileSavedLocationSource Source { get; set; }
}