using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Company / head-office job title catalog. Separate from <see cref="Position"/> (visa-report titles).
/// </summary>
[DefaultClassOptions]
[NavigationItem("Lookup/Organization/Config")]
[DisplayName("Actual Position")]
[DefaultProperty(nameof(Name))]
[ImageName("BO_Position")]
public class ActualPosition : BaseObject
{
    [RuleRequiredField(DefaultContexts.Save)]
    [MaxLength(200)]
    [XafDisplayName("Title")]
    public virtual string Name { get; set; }
}
