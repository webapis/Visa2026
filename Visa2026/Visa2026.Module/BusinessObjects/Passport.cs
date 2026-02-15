using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    public class Passport : SingleActiveBaseObject<Person, Passport>, IExpirationLogic
    {
        [MaxLength(20)]
        [RuleRequiredField]
        public virtual string PassportNumber { get; set; }

        public virtual PassportType PassportType { get; set; }

        public virtual DateTime IssueDate { get; set; }

        public virtual DateTime ExpirationDate { get; set; }

        [MaxLength(100)]
        public virtual string Authority { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<Passport> GetSiblings(Person parent)
        {
            return parent?.Passports;
        }

        public override void SetParentActiveItem(Person parent, Passport item)
        {
            parent.ActivePassport = item;
            if (item != null)
            {
                parent.PassportNumber = item.PassportNumber;
            }
        }

        public override bool IsParentActiveItem(Person parent, Passport item)
        {
            return parent.ActivePassport == item;
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
    }
}