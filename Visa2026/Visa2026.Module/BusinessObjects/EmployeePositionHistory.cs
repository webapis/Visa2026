using System;
using System.Collections.Generic;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    public class EmployeePositionHistory : SingleActiveBaseObject<Employee, EmployeePositionHistory>
    {
        public virtual DateTime StartDate { get; set; }

        public virtual DateTime? EndDate { get; set; }

        [RuleRequiredField]
        public virtual Position Position { get; set; }

        [RuleRequiredField]
        public virtual Department Department { get; set; }

        [RuleRequiredField]
        public virtual Employee Employee { get; set; }

        public override Employee GetParent()
        {
            return Employee;
        }

        public override IList<EmployeePositionHistory> GetSiblings(Employee parent)
        {
            return parent?.PositionHistory;
        }

        public override void SetParentActiveItem(Employee parent, EmployeePositionHistory item)
        {
            parent.CurrentPositionHistory = item;
            if (item != null)
            {
                parent.Position = item.Position;
                parent.Department = item.Department;
            }
        }

        public override bool IsParentActiveItem(Employee parent, EmployeePositionHistory item)
        {
            return parent.CurrentPositionHistory == item;
        }
    }
}