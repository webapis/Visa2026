using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    [NavigationItem("Lookup/Application/Config")]
    public class BusinessTripPurpose : BaseObject
    {
        [MaxLength(200)]
        public virtual string Name { get; set; }

        [MaxLength(2000)]
        public virtual string Description { get; set; }
    }
}
