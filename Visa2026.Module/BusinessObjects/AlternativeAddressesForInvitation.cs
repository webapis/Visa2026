using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Reusable text for the places after the first invitation stay.
/// Region names, city names, and the street are already written in <see cref="AddressLine"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem("Lookup/General/Geography")]
[DefaultProperty(nameof(AddressLine))]
[XafDisplayName("Alternative addresses for invitation")]
public class AlternativeAddressesForInvitation : BaseObject
{
    [MaxLength(2000)]
    [RuleRequiredField]
    [FieldSize(2000)]
    [XafDisplayName("Address line")]
    public virtual string AddressLine { get; set; }
}
