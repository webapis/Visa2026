using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Employee")]
    [DefaultProperty(nameof(WorkPermitItemName))]
    [RuleCriteria("WorkPermitItem_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    public class WorkPermitItem : SingleActiveBaseObject<Employee, WorkPermitItem>, IExpirationLogic
    {
        [RuleRequiredField]
      
        public virtual Employee Employee { get; set; }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        [RuleRequiredField]
        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        [RuleRequiredField]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        

        public virtual string ASNumber { get; set; }

        public virtual WorkPermit WorkPermit { get; set; }

        public virtual WorkPermitLocation Location { get; set; }

        [RuleFromBoolProperty("WorkPermitItem_EmployeeIsValid", DefaultContexts.Save, "The selected employee is not part of the parent application.")]
        [Browsable(false)]
        public bool IsEmployeeValid
        {
            get
            {
                if (Employee == null || WorkPermit?.Application == null) return true;
                return WorkPermit.Application.ApplicationItems.Any(ai => ai.Person?.ID == Employee.ID);
            }
        }

        public override Employee GetParent()
        {
            return Employee;
        }

        public override IList<WorkPermitItem> GetSiblings(Employee parent)
        {
            return parent?.WorkPermitItems;
        }

        public override void SetParentActiveItem(Employee parent, WorkPermitItem item)
        {
            parent.CurrentWorkPermitItem = item;
        }

        public override bool IsParentActiveItem(Employee parent, WorkPermitItem item)
        {
            return parent.CurrentWorkPermitItem == item;
        }

        DateTime? IExpirationLogic.ExpirationDate => ExpirationDate;

        public int DaysRemaining
        {
            get
            {
                return (ExpirationDate.Date - DateTime.Today).Days;
            }
        }

        public ExpirationState ExpirationState
        {
            get
            {
                if (!IsActive) return ExpirationState.Archived;
                if (DaysRemaining < 0) return ExpirationState.Expired;
                if (DaysRemaining <= 30) return ExpirationState.ExpiringSoon;
                return ExpirationState.Active;
            }
        }

        public string WorkPermitItemName => $"{Employee?.FullName} - {WorkPermitNumber}";

        public override void OnSaving()
        {
            base.OnSaving();
            if (WorkPermit?.Application != null && Employee != null)
            {
                var appItem = WorkPermit.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == Employee.ID);
                if (appItem != null)
                {
                    appItem.WorkPermitItemIsIssued = true;
                }
            }
        }

		public virtual bool IsCancelled { get; set; }

		public virtual bool IsChanged { get; set; }
    }
}