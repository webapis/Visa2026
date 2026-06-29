using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant catalog of commercial hotels (myhmanhana) for <see cref="AddressOfResidence"/> when <see cref="ResidenceType.Hotel"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Name))]
public class Hotel : BaseObject
{
    [MaxLength(255)]
    [RuleRequiredField]
    [XafDisplayName("Name")]
    public virtual string Name { get; set; }

    [RuleRequiredField]
    public virtual City City { get; set; }

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string Notes { get; set; }
}
