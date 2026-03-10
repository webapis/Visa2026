using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class ProjectContractDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual ProjectContract ProjectContract { get; set; }
    }
}