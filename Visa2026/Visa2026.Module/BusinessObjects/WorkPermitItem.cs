using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Organization")]
    [RuleValidApplicationPerson]
    public class WorkPermitItem : BaseObject, IApplicationItemChild
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

        [Browsable(false)]
        Person IApplicationItemChild.Person => Employee;

        [Browsable(false)]
        Application IApplicationItemChild.Application => WorkPermit?.Application;

        // Business Logic: ExpirationDate > StartDate
        // This validation is typically handled via RuleCriteria or OnSaving overrides in XAF/EF Core
    }
}