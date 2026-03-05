using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class RepresentativeDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Representative Representative { get; set; }
    }
}