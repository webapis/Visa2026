using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using System.ComponentModel;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.DC;
namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DefaultProperty(nameof(PassportNumber))]
    [NavigationItem("Lookup/Person")]
    [RuleCriteria("Passport_DateRange", DefaultContexts.Save, "ExpirationDate > IssueDate", "Expiration Date must be later than Issue Date.")]
    public class Passport : SingleActiveBaseObject<Person, Passport>, IExpirationLogic
    {
        [MaxLength(20)]
        [RuleRequiredField]
        [RuleUniqueValue]
        public virtual string PassportNumber { get; set; }

        public virtual PassportType PassportType { get; set; }

        public virtual DateTime IssueDate { get; set; }

        public virtual DateTime ExpirationDate { get; set; }

        [MaxLength(100)]
        public virtual string Authority { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [InverseProperty(nameof(PassportImage.Passport))]
        [Aggregated]
        public virtual IList<PassportImage> Images { get; set; } = new ObservableCollection<PassportImage>();

        [InverseProperty(nameof(Visa.Passport))]
        [Aggregated]
        public virtual IList<Visa> Visas { get; set; } = new ObservableCollection<Visa>();

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
            parent.CurrentPassport = item;
        }

        public override bool IsParentActiveItem(Person parent, Passport item)
        {
            return parent.CurrentPassport == item;
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

                double totalDays = (ExpirationDate.Date - IssueDate.Date).Days;
                if (totalDays > 0)
                {
                    double elapsedDays = (DateTime.Today - IssueDate.Date).Days;
                    if (ObjectSpace != null)
                    {
                        var threshold = (double)SystemSettings.GetInstance(ObjectSpace).ExpirationWarningThreshold;
                        if (elapsedDays / totalDays >= threshold)
                        {
                            return ExpirationState.ExpiringSoon;
                        }
                    }
                }
                else
                {
                    return ExpirationState.ExpiringSoon;
                }
                return ExpirationState.Active;
            }
        }


    }
}