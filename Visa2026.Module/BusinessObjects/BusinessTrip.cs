using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(DefaultProperty))]
    public class BusinessTrip : SingleActiveBaseObject<Person, BusinessTrip>,ISoftDelete
    {
        [NotMapped]
        [Browsable(false)]
        public string DefaultProperty => Person?.FullName;

        private Person person;
        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (person != null)
                    {
                        CurrentPassport = person.CurrentPassport;
                        CurrentVisa = person.CurrentVisa;
                        CurrentAddressOfResidence = person.CurrentAddressOfResidence;
                    }
                }
            }
        }

        public virtual Application Application { get; set; }

        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        public virtual Passport CurrentPassport { get; set; }

        public virtual Visa CurrentVisa { get; set; }

        [Aggregated]
        public virtual BusinessTripAddress BusinessTripAddress { get; set; }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<BusinessTrip> GetSiblings(Person parent)
        {
            return parent?.BusinessTrips;
        }

        public override void SetParentActiveItem(Person parent, BusinessTrip item)
        {
            parent.CurrentBusinessTrip = item;
        }

        public override bool IsParentActiveItem(Person parent, BusinessTrip item)
        {
            return parent.CurrentBusinessTrip == item;
        }

              [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}