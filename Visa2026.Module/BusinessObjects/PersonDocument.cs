using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Documents")]
    public class PersonDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Person Person { get; set; }
    }
}