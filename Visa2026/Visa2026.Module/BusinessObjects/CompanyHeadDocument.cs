using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class CompanyHeadDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual CompanyHead CompanyHead { get; set; }
    }
}