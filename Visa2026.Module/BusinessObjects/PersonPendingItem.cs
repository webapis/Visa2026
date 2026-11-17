using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// One field or child record the officer marked "Not received" on the person intake wizard.
/// The nine <see cref="Person"/> incomplete-area flags are a rollup of these rows.
/// </summary>
[NavigationItem(false)]
public class PersonPendingItem : BaseObject
{
    [MaxLength(128)]
    [Required]
    public virtual string TargetKey { get; set; }

    /// <summary>One of <see cref="PersonIncompleteDataLabels"/>.</summary>
    [MaxLength(64)]
    [Required]
    public virtual string AreaKey { get; set; }

    /// <summary>Optional waiting-on note for a record. Empty for a field.</summary>
    [MaxLength(2000)]
    public virtual string Note { get; set; }

    public virtual Person Person { get; set; }
}
