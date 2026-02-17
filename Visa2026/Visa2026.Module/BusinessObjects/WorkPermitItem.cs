using System;
using System.ComponentModel.DataAnnotations;
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

        public virtual PersonInApplication ProcessNumber { get; set; }

        public virtual bool IsActive { get; set; }

        // Business Logic: ExpirationDate > StartDate
        // This validation is typically handled via RuleCriteria or OnSaving overrides in XAF/EF Core
    }
}