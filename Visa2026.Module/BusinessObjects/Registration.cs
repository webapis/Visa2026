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
using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using System.Linq;

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

        public virtual DateTime? RegistrationDate { get; set; }

        [NotMapped]
        public string RegistrationName => $"{Person?.FullName} - {Application?.ApplicationType?.Name}";

        #region Person
        [XafDisplayName("Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_FullName => Person?.FullName;

        [XafDisplayName("Birth Place"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_BirthPlace => Person?.BirthPlace;

        [XafDisplayName("Photo"), VisibleInDetailView(false), VisibleInListView(false)]
        [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
        public byte[] Person_Photo => Person?.Photo;

        [XafDisplayName("Date of Birth"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Person_DateOfBirth => Person?.DateOfBirth;

        [XafDisplayName("Date of Birth (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_DateOfBirthText => $"{Person?.DateOfBirth:dd.MM.yyyy}";

        [XafDisplayName("Gender (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_GenderTm => Person?.Gender?.NameTm;

        [XafDisplayName("Marital Status (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_MaritalStatusTm => Person?.MaritalStatus?.NameTm;

        [XafDisplayName("Nationality Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityCode => Person?.Nationality?.Code;

        [XafDisplayName("Nationality (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityTm => Person?.Nationality?.NameTm;

        [XafDisplayName("Country of Birth Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_CountryOfBirthCode => Person?.CountryOfBirth?.Code;

        [XafDisplayName("Country of Birth (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_CountryOfBirthTm => Person?.CountryOfBirth?.NameTm;

        [XafDisplayName("Company Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_CompanyName => Person?.Company?.Name;

        [XafDisplayName("Company Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_CompanyAddress => Person?.Company?.Address;

        [XafDisplayName("Foreign Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddress => Person?.ForeignAddress;

        [XafDisplayName("Foreign Address Country Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddressCountryCode => Person?.ForeignAddressCountry?.Code;

        [XafDisplayName("Foreign Address Country (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddressCountryTm => Person?.ForeignAddressCountry?.NameTm;
        #endregion

        #region Passport
        [XafDisplayName("Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Number => CurrentPassport?.PassportNumber;

        [XafDisplayName("Passport Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Passport_ExpirationDate => CurrentPassport?.ExpirationDate;

        [XafDisplayName("Passport Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_ExpirationDateText => $"{CurrentPassport?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Passport Country Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_CountryCode => CurrentPassport?.IssuedCountry?.Code;

        [XafDisplayName("Passport Country (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_CountryTm => CurrentPassport?.IssuedCountry?.NameTm;
        #endregion

        #region Visa
        [XafDisplayName("Visa Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_Number => CurrentVisa?.VisaNumber;

        [XafDisplayName("Visa Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Visa_IssueDate => CurrentVisa?.IssueDate;

        [XafDisplayName("Visa Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_IssueDateText => $"{CurrentVisa?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Visa Start Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Visa_StartDate => CurrentVisa?.StartDate;

        [XafDisplayName("Visa Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_StartDateText => $"{CurrentVisa?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Visa Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Visa_ExpirationDate => CurrentVisa?.ExpirationDate;

        [XafDisplayName("Visa Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_ExpirationDateText => $"{CurrentVisa?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Visa Issued Place (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_IssuedPlaceTm => CurrentVisa?.VisaIssuedPlace?.NameTm;

        [XafDisplayName("Visa Category (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_CategoryTm => CurrentVisa?.VisaCategory?.NameTm;

        [XafDisplayName("Visa Type (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_TypeTm => CurrentVisa?.VisaType?.NameTm;
        #endregion

        #region Travel
        [XafDisplayName("Travel Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Travel_Date => MovementRecord?.TravelDate;

        [XafDisplayName("Travel Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_DateText => $"{MovementRecord?.TravelDate:dd.MM.yyyy}";

        [XafDisplayName("Travel Purpose of Travel (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_PurposeOfTravelTm => MovementRecord?.PurposeOfTravel?.NameTm;

        [XafDisplayName("Travel Checkpoint (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_CheckPointTm => MovementRecord?.CheckPoint?.NameTm;
        #endregion

        #region Address
        [XafDisplayName("Address Full Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_FullAddress => CurrentAddressOfResidence?.FullAddress;

        [XafDisplayName("Address Region (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_RegionTm => CurrentAddressOfResidence?.Region?.NameTm;

        [XafDisplayName("Address City (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_CityTm => CurrentAddressOfResidence?.City?.NameTm;
        #endregion

        #region Position
        [XafDisplayName("Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_PositionTm => CurrentPositionHistory?.Position?.NameTm;

        [XafDisplayName("Department (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_DepartmentTm => CurrentPositionHistory?.Department?.NameTm;
        #endregion

        #region Application
        [XafDisplayName("Application Full Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_FullNumber => Application?.FullApplicationNumber;

        [XafDisplayName("Application Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_DateText => $"{Application?.ApplicationDate:dd.MM.yyyy}";

        [XafDisplayName("Registration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_RegistrationDateText => $"{RegistrationDate:dd.MM.yyyy}";
        #endregion

        #region CompanyHead
        [NotMapped]
        [XafDisplayName("Signatory Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PositionTm => Person?.Company?.CurrentAuthorizedSignatory?.Position?.NameTm;

        [NotMapped]
        [XafDisplayName("Signatory Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_FullName => Person?.Company?.CurrentAuthorizedSignatory?.FullName;
        #endregion

        [NotMapped]
        [XafDisplayName("Row Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public int RowNumber { get; set; }

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