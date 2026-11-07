using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>Scanned order-for-assignment file(s) for an <see cref="EmployeePositionHistory"/> row.</summary>
    [DefaultClassOptions]
    [NavigationItem("Documents")]
    public class EmployeePositionHistoryDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual EmployeePositionHistory EmployeePositionHistory { get; set; }
    }
}