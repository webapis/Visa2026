using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Geography")]
    [DefaultProperty(nameof(Name))]
    public class Country : BaseObject
    {
        [Required]
        [MaxLength(100)]
        public virtual string Name { get; set; }

        [Required]
        [MaxLength(3)]
        public virtual string Code { get; set; }

        [MaxLength(10)]
        public virtual string DialingCode { get; set; }
    }
}
