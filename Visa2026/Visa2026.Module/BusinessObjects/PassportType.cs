using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    [NavigationItem("Lookup/Passport")]
    public class PassportType : BaseObject
    {
        [RuleRequiredField]
        [RuleUniqueValue]
        [MaxLength(100)]
        public virtual string Name { get; set; }
    }
}