using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class Urgency : BaseObject
    {
        [Required]
        [MaxLength(100)]
        [RuleUniqueValue]
        public virtual string Name { get; set; }

        [MaxLength(10)]
        [RuleUniqueValue]
        public virtual string Code { get; set; }

        [RuleRequiredField]
        [RuleUniqueValue]
        public virtual int Priority { get; set; }
    }
}