using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// One excluded roster person on a <see cref="ApplicationProfileInstanceExclusion"/> letter.
/// Name and passport are snapshots at save so the letter can be regenerated unchanged.
/// </summary>
[Table("ApplicationProfileInstanceExclusionPeople")]
[DefaultClassOptions]
[NavigationItem(false)]
[XafDisplayName("Excluded person")]
[DefaultProperty(nameof(FullName))]
public class ApplicationProfileInstanceExclusionPerson : BaseObject
{
    [Browsable(false)]
    public virtual Guid ExclusionId { get; set; }

    [Browsable(false)]
    public virtual ApplicationProfileInstanceExclusion Exclusion { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid PersonId { get; set; }

    [Browsable(false)]
    public virtual Person Person { get; set; } = null!;

    [XafDisplayName("#")]
    public virtual int Sequence { get; set; }

    [MaxLength(300)]
    [XafDisplayName("Person")]
    public virtual string FullName { get; set; } = string.Empty;

    [MaxLength(100)]
    [XafDisplayName("Passport")]
    public virtual string? PassportNumber { get; set; }
}
