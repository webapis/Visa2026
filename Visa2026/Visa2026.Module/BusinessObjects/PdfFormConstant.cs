using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.ComponentModel;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("System")]
    [DefaultProperty(nameof(DisplayValue))]
    public class PdfFormConstant : BaseObject
    {
        [RuleRequiredField]
        public virtual string Category { get; set; }

        [RuleRequiredField]
        public virtual string DisplayValue { get; set; }

        [RuleRequiredField]
        public virtual string PdfValue { get; set; }
    }
}