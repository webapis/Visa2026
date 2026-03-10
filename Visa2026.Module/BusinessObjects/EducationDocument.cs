using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class EducationDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Education Education { get; set; }
    }
}