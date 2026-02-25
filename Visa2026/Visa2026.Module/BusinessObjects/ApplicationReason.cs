using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Application")]
    [DefaultProperty(nameof(Name))]
    public class ApplicationReason : BaseObject
    {
        [RuleRequiredField]
        [MaxLength(100)]
        public virtual string Name { get; set; }

        [RuleRequiredField]
        public virtual ApplicationType ApplicationType { get; set; }
    }
}