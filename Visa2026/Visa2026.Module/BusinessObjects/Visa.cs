using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]
    public class Visa : SingleActiveBaseObject<Person, Visa>, IExpirationLogic
    {
        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string VisaNumber { get; set; }

        public virtual DateTime StartDate { get; set; }

        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        public virtual Person Person { get; set; }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<Visa> GetSiblings(Person parent)
        {
            return parent?.Visas;
        }

        public override void SetParentActiveItem(Person parent, Visa item)
        {
            parent.CurrentVisa = item;
        }

        public override bool IsParentActiveItem(Person parent, Visa item)
        {
            return parent.CurrentVisa == item;
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