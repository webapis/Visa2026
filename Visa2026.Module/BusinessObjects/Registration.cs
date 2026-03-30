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
    public class Registration : SingleActiveBaseObject<Person, Registration>,ISoftDelete
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
                        CurrentAddressOfResidence = person.CurrentAddressOfResidence;
                        CurrentPositionHistory = person.CurrentPositionHistory;
                    }
                    if (person != null && Application?.ApplicationType != null)
                    {
                        UpdateMovementRecord(Application.ApplicationType.Name);
                    }
                }
            }
        }

        private Application application;
        [ImmediatePostData]
        public virtual Application Application 
        { 
            get => application;
            set
            {
                if (application != value)
                {
                    application = value;
                    if (application?.ApplicationType != null)
                    {
                        UpdateMovementRecord(application.ApplicationType.Name);
                    }
                }
            }
        }

        [RuleRequiredField]
        public virtual Passport CurrentPassport { get; set; }

        [Appearance("RegistrationVisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentVisa", Context = "DetailView")]
        public virtual Visa CurrentVisa { get; set; }

        [Appearance("RegistrationAddressVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentAddressOfResidence", Context = "DetailView")]
        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        [Appearance("RegistrationPositionVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowProjectContract", Context = "DetailView")]
        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }


        private void UpdateMovementRecord(string appTypeName)
        {
            if (ObjectSpace == null || Person == null) return;

            // Determine the required type based on ApplicationType Name
            Type targetType = appTypeName switch
            {
                "App_Reg_Check_In" => typeof(ExternalArrival),
                "App_Reg_Check_Out" => typeof(ExternalDeparture),
                "App_Reg_Check_In_Internal" => typeof(InternalArrival),
                "App_Reg_Check_Out_Internal" => typeof(InternalDeparture),
                _ => null
            };

            if (targetType == null)
            {
                MovementRecord = null;
                return;
            }

            // Create new object if current is null OR if the type has changed
            // This allows switching from External to Internal types dynamically
            if (MovementRecord == null || MovementRecord.GetType() != targetType)
            {
                MovementRecord = (TravelHistory)ObjectSpace.CreateObject(targetType);
            }

            // Keep person and date in sync
            if (MovementRecord != null)
            {
                MovementRecord.Person = this.Person;
            }
        }

        [ExpandObjectMembers(ExpandObjectMembers.InDetailView)]
        [Appearance("ShowTravelForSpecificApps", Visibility = ViewItemVisibility.Hide, Criteria = "Not Application.ApplicationType.Name In ('Visa Entry', 'Border Registration', 'App_Reg_Check_In', 'App_Reg_Check_Out', 'App_Reg_Check_Out_Internal', 'App_Reg_Check_In_Internal')", Context = "DetailView")]
        public virtual TravelHistory MovementRecord { get; set; }

        [NotMapped]
        public string RegistrationName => $"{Person?.FullName} - {Application?.ApplicationType?.Name}";

        [XafDisplayName("Person Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonFullName => Person?.FullName;

        [XafDisplayName("Person Nationality"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonNationality => Person?.Nationality?.Name;

        [XafDisplayName("Person Nationality Full"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonNationalityFull => Person?.Nationality != null ? $"{Person.Nationality.Code}, {Person.Nationality.Name}" : null;

        [XafDisplayName("Person Country of Birth"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCountryOfBirth => Person?.CountryOfBirth?.Name;

        [XafDisplayName("Person Country of Birth Full"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCountryOfBirthFull => Person?.CountryOfBirth != null ? $"{Person.CountryOfBirth.Code}, {Person.CountryOfBirth.Name}" : null;

        [XafDisplayName("Person Date of Birth"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonDateOfBirth => Person?.DateOfBirth;

        [XafDisplayName("Person Date of Birth (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonDateOfBirthText => $"{Person?.DateOfBirth:dd.MM.yyyy}";

        [XafDisplayName("Person Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportNumber => CurrentPassport?.PassportNumber;

        [XafDisplayName("Person Passport Country"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportCountry => CurrentPassport?.IssuedCountry?.Name;

        [XafDisplayName("Person Passport Country Full"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportCountryFull => CurrentPassport?.IssuedCountry != null ? $"{CurrentPassport.IssuedCountry.Code}, {CurrentPassport.IssuedCountry.Name}" : null;

        [XafDisplayName("Person Passport Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonPassportExpirationDate => CurrentPassport?.ExpirationDate;

        [XafDisplayName("Person Passport Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportExpirationDateText => $"{CurrentPassport?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Person Visa Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaNumber => CurrentVisa?.VisaNumber;

        [XafDisplayName("Person Visa Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonVisaExpirationDate => CurrentVisa?.ExpirationDate;

        [XafDisplayName("Person Visa Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaExpirationDateText => $"{CurrentVisa?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Person Visa Issued Place"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaIssuedPlace => CurrentVisa?.VisaIssuedPlace?.Name;

        [XafDisplayName("Person Visa Category"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaCategory => CurrentVisa?.VisaCategory?.Name;

        [XafDisplayName("Person Visa Type"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaType => CurrentVisa?.VisaType?.Name;

        [XafDisplayName("Person Visa Start Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonVisaStartDate => CurrentVisa?.StartDate;

        [XafDisplayName("Person Visa Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaStartDateText => $"{CurrentVisa?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Person Visa Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonVisaIssueDate => CurrentVisa?.IssueDate;

        [XafDisplayName("Person Visa Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaIssueDateText => $"{CurrentVisa?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Movement Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? MovementDate => MovementRecord?.TravelDate;

        [XafDisplayName("Movement Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MovementDateText => $"{MovementRecord?.TravelDate:dd.MM.yyyy}";

        [XafDisplayName("Movement Purpose of Travel"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MovementPurposeOfTravel => MovementRecord?.PurposeOfTravel?.Name;

        [XafDisplayName("Movement Checkpoint"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MovementCheckPoint => MovementRecord?.CheckPoint?.Name;

        [XafDisplayName("Person Current Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddress => CurrentAddressOfResidence?.FullAddress;

        [XafDisplayName("Person Current Address Region"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddressRegion => CurrentAddressOfResidence?.Region?.Name;

        [XafDisplayName("Person Current Address City"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddressCity => CurrentAddressOfResidence?.City?.Name;

        [XafDisplayName("Person Position"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPosition => CurrentPositionHistory?.Position?.Name;

        [XafDisplayName("Person Department"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonDepartment => CurrentPositionHistory?.Department?.Name;

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => MovementRecord?.TravelDate;

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

              [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}