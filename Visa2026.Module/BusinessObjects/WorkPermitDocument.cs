using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class WorkPermitDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual WorkPermit WorkPermit { get; set; }
    }
}