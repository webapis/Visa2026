using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

public enum ApplicationProfileInstanceExclusionTemplateKind
{
    /// <summary>The Seretmezlik letter (Word).</summary>
    Letter = 0,

    /// <summary>The separate roster of excluded people — Sanaw (Word or Excel).</summary>
    Roster = 1,
}

/// <summary>
/// Company-wide Seretmezlik template uploaded by an officer (one row per <see cref="Kind"/>).
/// No row means the built-in layout is used. Kept apart from <see cref="UserReportTemplate"/> so it never
/// appears in the Resminamalar catalog.
/// </summary>
[Table("ApplicationProfileInstanceExclusionTemplates")]
[NavigationItem(false)]
[XafDisplayName("Seretmezlik template")]
public class ApplicationProfileInstanceExclusionTemplate : BaseObject
{
    public virtual ApplicationProfileInstanceExclusionTemplateKind Kind { get; set; }

    [Aggregated, ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData? TemplateFile { get; set; }

    [Browsable(false)]
    public virtual DateTime? UpdatedOnUtc { get; set; }

    [MaxLength(255)]
    [Browsable(false)]
    public virtual string? UpdatedByUserName { get; set; }
}
