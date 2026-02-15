using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class MaritalStatus : BaseObject
    [NavigationItem("Lookup/Person")]
    {
        [Required]
        [MaxLength(50)]
        public virtual string Name { get; set; }
    }
}
