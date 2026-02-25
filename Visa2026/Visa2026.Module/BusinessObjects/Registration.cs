using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(RegistrationName))]
    public class Registration : SingleActiveBaseObject<Person, Registration>
    {
        private Person person;
        [RuleRequiredField]
        [ModelDefault("AllowEdit", "False")]
        public virtual Person Person
        {
            get => person;
            protected set
            {
                if (person != value)
                {
                    person = value;
                    if (person != null)
                    {
                        CurrentPassport = person.CurrentPassport;
                        CurrentVisa = person.CurrentVisa;
                        CurrentTravelHistory = person.CurrentTravelHistory;
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
                        AddressOfResidence = null;
                        CurrentPositionHistory = null;
                    }
                }
            }
        }

        private Employee employee;
        [ImmediatePostData]
        [Appearance("EmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.IsForFamily", Context = "DetailView")]
        public virtual Employee Employee
        {
            get => employee;
            set
            {
                if (employee != value)
                {
                    employee = value;
                    if (employee != null && (Application == null || !Application.IsForFamily))
                    {
                        Person = employee;
                    }
                }
            }
        }

        private FamilyMember familyMember;
        [ImmediatePostData]
        [Appearance("FamilyMemberVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!Application.IsForFamily", Context = "DetailView")]
        public virtual FamilyMember FamilyMember
        {
            get => familyMember;
            set
            {
                if (familyMember != value)
                {
                    familyMember = value;
                    if (familyMember != null)
                    {
                        Employee = familyMember.Employee;
                        Person = familyMember;
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

        [Appearance("CurrentPositionHistoryVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.IsForFamily", Context = "DetailView")]
        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        public virtual TravelHistory CurrentTravelHistory { get; set; }

        public virtual Application Application { get; set; }

        [NotMapped]
        public string RegistrationName => $"{Person?.FullName} - {Application?.ApplicationType?.Name} on {RegistrationDate:d}";

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