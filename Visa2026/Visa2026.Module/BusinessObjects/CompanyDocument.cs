using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class CompanyDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Company Company { get; set; }
    }
}