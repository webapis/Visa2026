using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Tenant catalog of non-lodging work sites (plants, highways, project camps, etc.)
/// for <see cref="AddressOfResidence"/> when <see cref="ResidenceType.Other"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(FullAddress))]
public class OtherSite : BaseObject
{
    [MaxLength(255)]
    [RuleRequiredField]
    public virtual string FullAddress { get; set; }

    [RuleRequiredField]
    public virtual City City { get; set; }

    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string Notes { get; set; }
}
