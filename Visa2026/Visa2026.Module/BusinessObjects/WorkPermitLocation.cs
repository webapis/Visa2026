using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class WorkPermitLocation : BaseObject
    [NavigationItem("Lookup/Geography")]
    {
        [Required]
        [MaxLength(100)]
        public virtual string Name { get; set; }

        public virtual Region Region { get; set; }
    }
}
