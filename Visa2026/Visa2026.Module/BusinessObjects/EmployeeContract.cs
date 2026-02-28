using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(Title))]
    [RuleCriteria("EmployeeContract_DateRange", DefaultContexts.Save, "ExpirationDate > ContractStartDate", "Expiration Date must be later than Contract Start Date.")]
    public class EmployeeContract : SingleActiveBaseObject<Employee, EmployeeContract>, IExpirationLogic
    {
        [RuleRequiredField]
        public virtual Employee Employee
        {
            get => employee;
            set
            {
                if (employee != value)
                {
                    employee = value;
                    if (employee != null && PositionHistory == null)
                    {
                        PositionHistory = employee.CurrentPositionHistory;
                    }
                }
            }
        }
        private Employee employee;

        public virtual DateTime ContractStartDate { get; set; }

        public virtual DateTime? ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual EmployeePositionHistory PositionHistory { get; set; }

        public virtual decimal Salary { get; set; }

        public virtual ContractTemplate ContractTemplate { get; set; }

        [NotMapped]
        public string Title => $"{PositionHistory?.Position?.Name} since {ContractStartDate:d}";

        #region IExpirationLogic
        [NotMapped]
        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue)
                {
                    return int.MaxValue;
                }
                return (ExpirationDate.Value.Date - DateTime.Now.Date).Days;
            }
        }

        [NotMapped]
        public ExpirationState ExpirationState
        {
            get
            {
                if (!IsActive)
                {
                    return ExpirationState.Archived;
                }
                if (DaysRemaining < 0)
                {
                    return ExpirationState.Expired;
                }
                if (DaysRemaining <= 30)
                {
                    return ExpirationState.ExpiringSoon;
                }
                return ExpirationState.Active;
            }
        }
        #endregion

        #region SingleActiveBaseObject
        public override Employee GetParent()
        {
            return Employee;
        }

        public override IList<EmployeeContract> GetSiblings(Employee parent)
        {
            return parent?.EmployeeContracts;
        }

        public override void SetParentActiveItem(Employee parent, EmployeeContract item)
        {
            parent.CurrentEmployeeContract = item;
        }

        public override bool IsParentActiveItem(Employee parent, EmployeeContract item)
        {
            return parent.CurrentEmployeeContract == item;
        }
        #endregion
    }
}