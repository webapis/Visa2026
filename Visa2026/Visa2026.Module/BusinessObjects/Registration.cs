using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(RegistrationName))]
    public class Registration : SingleActiveBaseObject<Person, Registration>
    {
        private Person person;
        [RuleRequiredField]
        [ImmediatePostData]
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
                        CurrentTravelHistory = person.CurrentTravelHistory;
                        PurposeOfTravel = person.CurrentTravelHistory?.PurposeOfTravel;
                        AddressOfResidence = person.CurrentAddressOfResidence;
                        if (person is Employee employee)
                        {
                            CurrentPositionHistory = employee.CurrentPositionHistory;
                        }
                        else
                        {
                            CurrentPositionHistory = null;
                        }
                    }
                    else
                    {
                        CurrentPassport = null;
                        CurrentVisa = null;
                        CurrentTravelHistory = null;
                        PurposeOfTravel = null;
                        AddressOfResidence = null;
                        CurrentPositionHistory = null;
                    }
                }
            }
        }

        [RuleRequiredField]
        public virtual DateTime RegistrationDate { get; set; }

        public virtual DateTime? ExpirationDate { get; set; }

        [MaxLength(50)]
        public virtual string RegistrationNumber { get; set; }

        [RuleRequiredField]
        public virtual AddressOfResidence AddressOfResidence { get; set; }

        public virtual Passport CurrentPassport { get; set; }

        public virtual Visa CurrentVisa { get; set; }

        public virtual CheckPoint CheckPoint { get; set; }

        public virtual PurposeOfTravel PurposeOfTravel { get; set; }

        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        public virtual TravelHistory CurrentTravelHistory { get; set; }

        [RuleRequiredField]
        public virtual RegistrationType RegistrationType { get; set; }

        [NotMapped]
        public string RegistrationName => $"{Person?.FullName} - {RegistrationType?.Name} on {RegistrationDate:d}";

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<Registration> GetSiblings(Person parent)
        {
            return parent?.Registrations;
        }

        public override void SetParentActiveItem(Person parent, Registration item)
        {
            parent.CurrentRegistration = item;
        }

        public override bool IsParentActiveItem(Person parent, Registration item)
        {
            return parent.CurrentRegistration == item;
        }
    }
}