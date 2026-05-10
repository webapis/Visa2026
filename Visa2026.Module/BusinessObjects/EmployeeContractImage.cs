using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    [NavigationItem(false)]
    public class EmployeeContractImage : ImageBase
    {
        [RuleRequiredField]
        public virtual EmployeeContract EmployeeContract { get; set; }
    }
}