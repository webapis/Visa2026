using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]
    [DefaultProperty(nameof(Name))]
    public class VisaCategory : BaseObject
    {
        [Required]
        [MaxLength(100)]
        public virtual string Name { get; set; }
    }
}