using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]
    [DefaultProperty(nameof(Name))]
    public class Ministry : BaseObject
    {
        [Required]
        [MaxLength(100)]
        public virtual string Name { get; set; }
    }
}