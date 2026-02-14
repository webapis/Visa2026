using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    public class Gender : BaseObject
    {
        [Required]
        [MaxLength(50)]
        public virtual string Name { get; set; }
    }
}
