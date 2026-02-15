using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]
    [DefaultProperty(nameof(Name))]
    public class VisaType : BaseObject
    {
        [Required]
        [MaxLength(100)]
        [RuleUniqueValue]
        public virtual string Name { get; set; }

        [Required]
        [MaxLength(10)]
        [RuleUniqueValue]
        public virtual string Code { get; set; }

        public virtual string Description { get; set; }
    }
}