using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class EmployeeContractDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual EmployeeContract EmployeeContract { get; set; }
    }
}