using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    public class MedicalRecordDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual MedicalRecord MedicalRecord { get; set; }
    }
}