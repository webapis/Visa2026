using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Organization")]
    public class WorkPermitItem : BaseObject
    {
        [RuleRequiredField]
        [DataSourceProperty("WorkPermit.AvailableEmployees")]
        public virtual Employee Employee { get; set; }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        [RuleRequiredField]
        public virtual Position Position { get; set; }

        [RuleRequiredField]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        public virtual string ASNumber { get; set; }

        public virtual WorkPermit WorkPermit { get; set; }

        public virtual WorkPermitLocation Location { get; set; }

        public virtual ApplicationItem ProcessNumber { get; set; }

        public virtual bool IsActive { get; set; }

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

        // Business Logic: ExpirationDate > StartDate
        // This validation is typically handled via RuleCriteria or OnSaving overrides in XAF/EF Core
    }
}