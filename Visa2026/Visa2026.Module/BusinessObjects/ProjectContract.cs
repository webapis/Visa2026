using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(ContractNumber))]
    [NavigationItem("Lookup/Organization")]
    public class ProjectContract : BaseObject
    {
        [Required]
        [MaxLength(100)]
        public virtual string ContractNumber { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }

        [RuleRequiredField]
        public virtual Ministry Ministry { get; set; }
    }
}