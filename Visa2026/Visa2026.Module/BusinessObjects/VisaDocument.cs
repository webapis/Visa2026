using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class VisaDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Visa Visa { get; set; }
    }
}