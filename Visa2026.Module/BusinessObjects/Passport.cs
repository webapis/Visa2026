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
    [NavigationItem("Lookup/Passport")]
    [RuleCriteria("Passport_DateRange", DefaultContexts.Save, "ExpirationDate > IssueDate", "Expiration Date must be later than Issue Date.")]
    public class Passport : SingleActiveBaseObject<Person, Passport>, IExpirationLogic, ISoftDelete
    {
        public Passport()
        {
            Images = new ObservableCollection<PassportImage>();
            Documents = new ObservableCollection<PassportDocument>();
            Visas = new ObservableCollection<Visa>();
        }

        [MaxLength(20)]
        [RuleRequiredField]
        [RuleUniqueValue]
        public virtual string PassportNumber { get; set; }

        [MaxLength(50)]
        [RuleRequiredField]
        public virtual string PersonalNumber { get; set; }
        [RuleRequiredField]
        public virtual PassportType PassportType { get; set; }
        [RuleRequiredField]
        [ImmediatePostData]
        public virtual DateTime? IssueDate { get; set; }
        [RuleRequiredField]
        [ImmediatePostData]
        public virtual DateTime? ExpirationDate { get; set; }

        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string Authority { get; set; }
        [RuleRequiredField]
        public virtual Country IssuedCountry { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual Visa CurrentVisa { get; set; }



        [InverseProperty(nameof(PassportImage.Passport))]
        [Aggregated]
        public virtual IList<PassportImage> Images { get; set; }

        [InverseProperty(nameof(PassportDocument.Passport))]
        [Aggregated]
        public virtual IList<PassportDocument> Documents { get; set; }

        [InverseProperty(nameof(Visa.Passport))]
        [Aggregated]
        public virtual IList<Visa> Visas { get; set; }

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

        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue)
                {
                    return 0;
                }
                return (ExpirationDate.Value.Date - DateTime.Today).Days;
            }
        }

        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, IssueDate, ObjectSpace);
            }
        }

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.IssueDate;

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                PassportType = ObjectSpace.GetObjectsQuery<PassportType>().FirstOrDefault(pt => pt.IsDefault);
                IssuedCountry = ObjectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
            }
        }
    }
}