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
/// Case↔person optional Address 2 free-text notes (People &amp; links).
/// One row per (instance, person). Not a catalog AddressOfResidence FK.
/// </summary>
[Table("ApplicationProfileInstancePersonAddress2Notes")]
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationProfileInstancePersonAddress2Note : BaseObject
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

    /// <summary>Free multiline Address 2 notes for this person on this case.</summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string? Address2Text { get; set; }
}