using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Application/Config")]
    [DefaultProperty(nameof(Name))]
    public class ApplicationReason : BaseObject
    {
        [RuleRequiredField]
        [MaxLength(100)]
        public virtual string Name { get; set; }

        [RuleRequiredField]
        [InverseProperty(nameof(ApplicationType.ApplicationReasons))]
        public virtual ApplicationType ApplicationType { get; set; }
        [MaxLength(100)]
        [Browsable(false)]
        public virtual string ApplicationTypeName { get; set; }
    }
}