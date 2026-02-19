using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
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

        [NotMapped]
        public string Title => $"{Position?.Name} from {StartDate:d}";

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
        }

        public override bool IsParentActiveItem(Employee parent, EmployeePositionHistory item)
        {
            return parent.CurrentPositionHistory == item;
        }

        protected override void UpdateActiveState()
        {
            if (IsActive)
            {
                var parent = GetParent();
                if (parent != null)
                {
                    var siblings = GetSiblings(parent);
                    if (siblings != null)
                    {
                        foreach (var sibling in siblings)
                        {
                            if (sibling != this && sibling.IsActive)
                            {
                                sibling.EndDate = StartDate;
                            }
                        }
                    }
                }
            }
            base.UpdateActiveState();
        }
    }
}