using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Geography")]
    public class City : LookupBase
    {
        [RuleRequiredField]
        public virtual Region Region { get; set; }
    }
}