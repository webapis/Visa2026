using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Server-shared Address 2 recently-used texts, scoped by Application Profile template + ProjectContract.
/// </summary>
[Table("ApplicationProfileAddress2Recents")]
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationProfileAddress2Recent : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApplicationProfileId))]
    public virtual ApplicationProfile ApplicationProfile { get; set; } = null!;

    /// <summary>Empty Guid when the case has no ProjectContract. No FK — empty is a valid bucket.</summary>
    [Browsable(false)]
    public virtual Guid ProjectContractId { get; set; }

    /// <summary>Normalized fold key for uniqueness (trimmed / folded text, max 450).</summary>
    [Browsable(false)]
    [MaxLength(450)]
    public virtual string TextKey { get; set; } = string.Empty;

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string? Address2Text { get; set; }

    public virtual DateTime UsedAtUtc { get; set; }
}