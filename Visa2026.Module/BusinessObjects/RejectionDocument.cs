using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Documents")]
    public class RejectionDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Rejection Rejection { get; set; }
    }
}