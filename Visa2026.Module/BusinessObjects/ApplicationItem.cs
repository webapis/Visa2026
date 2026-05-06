using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;
using System.Linq;
using System.Collections.Generic;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(ApplicationItemName))]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
    public class ApplicationItem : BaseObject, IObjectSpaceLink, ISoftDelete    //10
    {
        public ApplicationItem()
        {
        }

        [RuleRequiredField]
        [ImmediatePostData] // Ensure changes to Application trigger updates
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceProperty("AvailablePeople")]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (ObjectSpace != null)
                    {
                        // Must not rely only on SyncRule + CrossObjectSyncHelper: non-admin users cannot read
                        // SyncRule, so GetObjectsQuery<SyncRule>() is empty and rules never run in production.
                        ApplyCurrentFieldsFromSelectedPerson();
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(Person));
                    }
                }
            }
        }
        private Person person;

        /// <summary>
        /// Copies <see cref="Person"/>'s current document links into this item when <see cref="Person"/> changes.
        /// Mirrors <see cref="Visa2026.Module.DatabaseUpdate.SyncRulesUpdater"/> "Pull * from Person" rules so behavior does not
        /// depend on the current user having read access to <see cref="SyncRule"/>.
        /// </summary>
        private void ApplyCurrentFieldsFromSelectedPerson()
        {
            if (ObjectSpace == null)
                return;

            if (person == null)
            {
                CurrentPassport = null;
                CurrentVisa = null;
                CurrentAddressOfResidence = null;
                CurrentMedicalRecord = null;
                CurrentEducation = null;
                CurrentInvitationItem = null;
                CurrentPositionHistory = null;
                CurrentEmployeeContract = null;
                CurrentWorkPermitItem = null;
                SecondWorkPermitItem = null;
                return;
            }

            var p = ObjectSpace.GetObject(person);
            CurrentPassport = p.CurrentPassport;
            CurrentVisa = p.CurrentVisa;
            CurrentAddressOfResidence = p.CurrentAddressOfResidence;
            CurrentMedicalRecord = p.CurrentMedicalRecord;
            CurrentEducation = p.CurrentEducation;
            CurrentInvitationItem = p.CurrentInvitationItem;
            SecondWorkPermitItem = null;

            if (p.IsEmployee)
            {
                CurrentPositionHistory = p.CurrentPositionHistory;
                CurrentEmployeeContract = p.CurrentEmployeeContract;
                CurrentWorkPermitItem = p.CurrentWorkPermitItem;
            }
            else
            {
                CurrentPositionHistory = null;
                CurrentEmployeeContract = null;
                CurrentWorkPermitItem = null;
            }
        }

        [Browsable(false)]
        [NotMapped]
        public IList<Person> AvailablePeople
        {
            get
            {
                if (ObjectSpace == null) return new List<Person>();

                if (Application == null) return new List<Person>();

                if (Application.Category == ApplicationTypeCategory.Both)
                {
                    return ObjectSpace.GetObjectsQuery<Person>().ToList();
                }

                bool isEmployee = Application.Category == ApplicationTypeCategory.Employee;
                return ObjectSpace.GetObjectsQuery<Person>().Where(p => p.IsEmployee == isEmployee).ToList();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<Passport> AvailablePassports
        {
            get
            {
                if (person == null) return new List<Passport>();
                return ObjectSpace?.GetObject(person)?.Passports?.ToList() ?? new List<Passport>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<EmployeePositionHistory> AvailablePositionHistories
        {
            get
            {
                if (person == null) return new List<EmployeePositionHistory>();
                return ObjectSpace?.GetObject(person)?.PositionHistory?.ToList() ?? new List<EmployeePositionHistory>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<Visa> AvailableVisas
        {
            get
            {
                if (person == null) return new List<Visa>();
                return ObjectSpace?.GetObject(person)?.Passports?.SelectMany(p => p.Visas ?? new List<Visa>()).ToList() ?? new List<Visa>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<WorkPermitItem> AvailableWorkPermitItems
        {
            get
            {
                if (person == null) return new List<WorkPermitItem>();
                return ObjectSpace?.GetObject(person)?.WorkPermitItems?.ToList() ?? new List<WorkPermitItem>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<InvitationItem> AvailableInvitationItems
        {
            get
            {
                if (person == null) return new List<InvitationItem>();
                return ObjectSpace?.GetObject(person)?.InvitationItems?.ToList() ?? new List<InvitationItem>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<AddressOfResidence> AvailableAddressesOfResidence
        {
            get
            {
                if (person == null) return new List<AddressOfResidence>();
                return ObjectSpace?.GetObject(person)?.AddressesOfResidence?.ToList() ?? new List<AddressOfResidence>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<Registration> AvailableRegistrations
        {
            get
            {
                if (person == null) return new List<Registration>();
                return ObjectSpace?.GetObject(person)?.Registrations?.ToList() ?? new List<Registration>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<EmployeeContract> AvailableEmployeeContracts
        {
            get
            {
                if (person == null) return new List<EmployeeContract>();
                return ObjectSpace?.GetObject(person)?.EmployeeContracts?.ToList() ?? new List<EmployeeContract>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<MedicalRecord> AvailableMedicalRecords
        {
            get
            {
                if (person == null) return new List<MedicalRecord>();
                return ObjectSpace?.GetObject(person)?.MedicalRecords?.ToList() ?? new List<MedicalRecord>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<Education> AvailableEducations
        {
            get
            {
                if (person == null) return new List<Education>();
                return ObjectSpace?.GetObject(person)?.Educations?.ToList() ?? new List<Education>();
            }
        }

        [DataSourceProperty(nameof(AvailablePositionHistories))]
        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        #region Person
        [XafDisplayName("Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_FullName => Person?.FullName;

        [XafDisplayName("Last Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_LastName => Person?.LastName;

        [XafDisplayName("First Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_FirstName => Person?.FirstName;

        [XafDisplayName("Gender (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_GenderTm => Person?.Gender?.NameTm;

        [XafDisplayName("Birth Place"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_BirthPlace => Person?.BirthPlace;

        [XafDisplayName("Foreign Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddress => Person?.ForeignAddress;

        [XafDisplayName("Photo"), VisibleInDetailView(false), VisibleInListView(false)]
        [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
        public byte[] Person_Photo => Person?.Photo;

        [XafDisplayName("Date of Birth"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Person_DateOfBirth => Person?.DateOfBirth;

        [XafDisplayName("Date of Birth (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_DateOfBirthText => $"{Person?.DateOfBirth:dd.MM.yyyy}";

        [XafDisplayName("Nationality Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityCode => Person?.Nationality?.Code;

        [XafDisplayName("Nationality (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityTm => Person?.Nationality?.NameTm;

        [XafDisplayName("Country of Birth Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_CountryOfBirthCode => Person?.CountryOfBirth?.Code;

        [XafDisplayName("Country of Birth (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_CountryOfBirthTm => Person?.CountryOfBirth?.NameTm;
        #endregion

        #region Position
        [XafDisplayName("Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_PositionTm => CurrentPositionHistory?.Position?.NameTm;

        [XafDisplayName("Department (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_DepartmentTm => CurrentPositionHistory?.Department?.NameTm;
        #endregion

        #region Passport
        [XafDisplayName("Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Number => CurrentPassport?.PassportNumber;

        [XafDisplayName("Passport Personal Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_PersonalNumber => Person?.PersonalNumber ?? CurrentPassport?.PersonalNumber;

        [XafDisplayName("Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Authority => CurrentPassport?.Authority;

        [XafDisplayName("Passport Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Passport_IssueDate => CurrentPassport?.IssueDate;

        [XafDisplayName("Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_IssueDateText => $"{CurrentPassport?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Passport Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Passport_ExpirationDate => CurrentPassport?.ExpirationDate;

        [XafDisplayName("Passport Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_ExpirationDateText => $"{CurrentPassport?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Passport Country Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_CountryCode => CurrentPassport?.IssuedCountry?.Code;

        [XafDisplayName("Passport Country (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_CountryTm => CurrentPassport?.IssuedCountry?.NameTm;
        #endregion

        #region PreviousPassport
        [XafDisplayName("Previous Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_Number => PreviousPassport?.PassportNumber;

        [XafDisplayName("Previous Passport Personal Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_PersonalNumber => Person?.PersonalNumber ?? PreviousPassport?.PersonalNumber;

        [XafDisplayName("Previous Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_Authority => PreviousPassport?.Authority;

        [XafDisplayName("Previous Passport Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PreviousPassport_IssueDate => PreviousPassport?.IssueDate;

        [XafDisplayName("Previous Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_IssueDateText => $"{PreviousPassport?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Passport Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PreviousPassport_ExpirationDate => PreviousPassport?.ExpirationDate;

        [XafDisplayName("Previous Passport Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_ExpirationDateText => $"{PreviousPassport?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Passport Country Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_CountryCode => PreviousPassport?.IssuedCountry?.Code;

        [XafDisplayName("Previous Passport Country (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_CountryTm => PreviousPassport?.IssuedCountry?.NameTm;
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

        #region Address
        [XafDisplayName("Address Full Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_FullAddress => CurrentAddressOfResidence?.FullAddress;

        [XafDisplayName("Address Type"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_Type => CurrentAddressOfResidence?.Type?.ToString();

        [XafDisplayName("Address Start Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Address_StartDate => CurrentAddressOfResidence?.StartDate;

        [XafDisplayName("Address Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_StartDateText => $"{CurrentAddressOfResidence?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Address Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Address_ExpirationDate => CurrentAddressOfResidence?.ExpirationDate;

        [XafDisplayName("Address Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_ExpirationDateText => $"{CurrentAddressOfResidence?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Address Region (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_RegionTm => CurrentAddressOfResidence?.Region?.NameTm;

        [XafDisplayName("Address City (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_CityTm => CurrentAddressOfResidence?.City?.NameTm;
        #endregion

        #region Contract
        [XafDisplayName("Contract Salary"), VisibleInDetailView(false), VisibleInListView(false)]
        public decimal? Contract_Salary => Person?.CurrentSalary?.Amount;

        [XafDisplayName("Contract Salary (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Contract_SalaryText => $"{Person?.CurrentSalary?.Amount:#,##0.00}";

        [XafDisplayName("Contract Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Contract_StartDateText
        {
            get
            {
                var baseExpiration = CurrentVisa?.ExpirationDate;
                if (baseExpiration is null)
                {
                    return string.Empty;
                }

                // Contract period should align to the *next* (extended/prolonged) visa period.
                // We treat the next period start as the current visa expiration date.
                var start = baseExpiration.Value.Date;
                return $"{start:dd.MM.yyyy}";
            }
        }

        [XafDisplayName("Contract Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Contract_ExpirationDateText
        {
            get
            {
                var baseExpiration = CurrentVisa?.ExpirationDate;
                var months = Application?.VisaPeriod?.PdfForm_Count;
                if (baseExpiration is null || months is null || months <= 0)
                {
                    return string.Empty;
                }

                var start = baseExpiration.Value.Date;
                var end = start.AddMonths(months.Value);
                return $"{end:dd.MM.yyyy}";
            }
        }

        [XafDisplayName("Contract Period Fallback Text"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Contract_PeriodFallbackText
        {
            get
            {
                if (CurrentVisa != null)
                {
                    return string.Empty;
                }

                var months = Application?.VisaPeriod?.PdfForm_Count;
                if (months is null || months <= 0)
                {
                    return string.Empty;
                }

                return $"Rugsatnamanyň başlaýan gününden {months} aý möhleti bilen güýje girer.";
            }
        }

        [XafDisplayName("Salary Currency Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Salary_CurrencyCode => Person?.CurrentSalary?.Currency.ToString();

        [XafDisplayName("Company Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyAddress => Application?.Company?.Address;
        #endregion

        #region Education
        [XafDisplayName("Education Graduation Year"), VisibleInDetailView(false), VisibleInListView(false)]
        public int? Education_GraduationYear => CurrentEducation?.GraduationYear;

        [XafDisplayName("Education Level (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_LevelTm => CurrentEducation?.EducationLevel?.NameTm;

        [XafDisplayName("Education Institution"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_InstitutionName => CurrentEducation?.EducationInstitution?.Name;

        [XafDisplayName("Education Specialty (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_SpecialtyTm => CurrentEducation?.Specialty?.NameTm;
        #endregion

        #region WorkPermit
        [XafDisplayName("Work Permit Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit_Number => CurrentWorkPermitItem?.WorkPermitNumber;

        [XafDisplayName("Work Permit Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? WorkPermit_ExpirationDate => CurrentWorkPermitItem?.ExpirationDate;

        [XafDisplayName("Work Permit Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit_ExpirationDateText => $"{CurrentWorkPermitItem?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Work Permit Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit_StartDateText => $"{CurrentWorkPermitItem?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Work Permit AS Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit_ASNumber => CurrentWorkPermitItem?.ASNumber;

        [XafDisplayName("Work Permit Permitted Locations"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit_WorkPermittedLocations => CurrentWorkPermitItem?.WorkPermittedLocations;

        [XafDisplayName("Work Permit 2 Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit2_Number => SecondWorkPermitItem?.WorkPermitNumber;

        [XafDisplayName("Work Permit 2 Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit2_ExpirationDateText => $"{SecondWorkPermitItem?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region Invitation
        [XafDisplayName("Invitation Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_Number => CurrentInvitationItem?.Invitation?.InvitationNumber;

        [XafDisplayName("Invitation Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_StartDateText => $"{CurrentInvitationItem?.Invitation?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Invitation Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_ExpirationDateText => $"{CurrentInvitationItem?.Invitation?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region MedicalRecord
        [XafDisplayName("Medical Record Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MedicalRecord_Number => CurrentMedicalRecord?.DocumentNumber;

        [XafDisplayName("Medical Record Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? MedicalRecord_IssueDate => CurrentMedicalRecord?.IssueDate;

        [XafDisplayName("Medical Record Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? MedicalRecord_ExpirationDate => CurrentMedicalRecord?.ExpirationDate;

        [XafDisplayName("Medical Record Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MedicalRecord_ExpirationDateText => $"{CurrentMedicalRecord?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region Application
        [XafDisplayName("Application Full Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_FullNumber => Application?.FullApplicationNumber;

        [XafDisplayName("Visa Period (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_VisaPeriod_NameTm => Application?.VisaPeriod?.NameTm;

        [XafDisplayName("Visa Category (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_VisaCategory_NameTm => Application?.VisaCategory?.NameTm;

        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_BorderZoneLocation_NameTm => Application?.BorderZoneLocation?.NameTm;

        [XafDisplayName("Application Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_DateText => $"{Application?.ApplicationDate:dd.MM.yyyy}";

        [XafDisplayName("Sponsor Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_SponsorName => Application?.Company?.Name;

        [XafDisplayName("Sponsor Authorized Signatory"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_SponsorSignatory => Application?.CompanyHead?.FullName;
        #endregion

        #region CompanyHead (Signatory)
        [NotMapped]
        [XafDisplayName("Signatory Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_FullName => Application?.CompanyHead?.FullName;

        [NotMapped]
        [XafDisplayName("Signatory Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PositionTm => Application?.CompanyHead?.Position?.NameTm;
        #endregion

        #region PDF Visa Application (XFA) — family members aggregate
        /// <summary>
        /// Full family list for the TM visa PDF: from <see cref="Person.FamilyMembers"/> when non-empty,
        /// otherwise from <see cref="Person.VisaApplicationFamilyMembersText"/> when
        /// <see cref="Person.DeclareFamilyMembersOnVisa"/> is true (manual Case 2).
        /// For an <see cref="ApplicationItem"/> whose <see cref="Person"/> is a family member, uses the
        /// <see cref="Person.SponsoringEmployee"/>'s data.
        /// </summary>
        [NotMapped]
        [XafDisplayName("PDF Family Members Aggregate"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_FamilyMembersAggregateText => BuildPdfFamilyMembersAggregateText();

        private string BuildPdfFamilyMembersAggregateText()
        {
            var emp = PdfEmployeeForHouseholdOnVisaForm();
            if (emp == null) return null;
            var fromMaster = FormatFamilyMembersFromMaster(emp);
            if (!string.IsNullOrWhiteSpace(fromMaster))
                return fromMaster.Trim();
            if (emp.DeclareFamilyMembersOnVisa && !string.IsNullOrWhiteSpace(emp.VisaApplicationFamilyMembersText))
                return emp.VisaApplicationFamilyMembersText.Trim();
            return null;
        }

        /// <summary>Employee whose household is listed on the visa form (applicant or sponsor).</summary>
        private Person PdfEmployeeForHouseholdOnVisaForm()
        {
            if (Person == null) return null;
            return Person.IsEmployee ? Person : Person.SponsoringEmployee;
        }

        private string FormatFamilyMembersFromMaster(Person employee)
        {
            if (employee?.FamilyMembers == null) return null;
            var lines = new List<string>();
            foreach (var fm in employee.FamilyMembers
                         .Where(f => f != null && !f.IsDeleted)
                         .OrderBy(f => f.LastName)
                         .ThenBy(f => f.FirstName))
            {
                if (ObjectSpace?.IsObjectToDelete(fm) == true) continue;
                var rel = fm.Relationship?.NameTm ?? fm.Relationship?.Name ?? string.Empty;
                lines.Add($"{fm.FullName}; {fm.DateOfBirth:dd.MM.yyyy}; {rel}".Trim());
            }
            return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
        }
        #endregion

        #region PDF Visa Application (XFA) — spouse & accompanying travellers
        /// <summary>
        /// Spouse row on the TM visa PDF is filled from the employee's <see cref="Person.FamilyMembers"/>
        /// when <see cref="Relationship"/> is marked as spouse (see <see cref="IsSpouseRelationship"/>).
        /// </summary>
        [NotMapped]
        [XafDisplayName("PDF Spouse Last Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_SpouseLastName => PdfSpousePersonFromMasterData()?.LastName;

        [NotMapped]
        [XafDisplayName("PDF Spouse First Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_SpouseFirstName => PdfSpousePersonFromMasterData()?.FirstName;

        [NotMapped]
        [XafDisplayName("PDF Spouse Additional"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_SpouseAdditional
        {
            get
            {
                var s = PdfSpousePersonFromMasterData();
                if (s == null) return null;
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(s.MiddleName))
                    parts.Add(s.MiddleName.Trim());
                if (s.DateOfBirth != default)
                    parts.Add(s.DateOfBirth.ToString("dd.MM.yyyy"));
                return parts.Count == 0 ? null : string.Join(", ", parts);
            }
        }

        /// <summary>
        /// "Accompanying" block: for an employee item, the first other family-member line on the same application
        /// sponsored by this person; for a family-member item, the sponsoring employee.
        /// </summary>
        [NotMapped]
        [XafDisplayName("PDF Accompanying Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_AccompanyingFullName => PdfAccompanyingPerson()?.FullName;

        [NotMapped]
        [XafDisplayName("PDF Accompanying Nationality Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_AccompanyingNationalityCode => PdfAccompanyingPerson()?.Nationality?.Code;

        [NotMapped]
        [XafDisplayName("PDF Accompanying Detail 1"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_AccompanyingDetail1 => PdfAccompanyingRelationshipLabel();

        [NotMapped]
        [XafDisplayName("PDF Accompanying Detail 2"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_AccompanyingDetail2
        {
            get
            {
                var p = PdfAccompanyingPerson();
                return p == null || p.DateOfBirth == default ? null : p.DateOfBirth.ToString("dd.MM.yyyy");
            }
        }

        [NotMapped]
        [XafDisplayName("PDF Accompanying Detail 3"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_AccompanyingDetail3 => PdfAccompanyingPassport()?.PassportNumber;

        [NotMapped]
        [XafDisplayName("PDF Accompanying Detail 4"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_AccompanyingDetail4
        {
            get
            {
                var p = PdfAccompanyingPerson();
                if (p == null) return null;
                return !string.IsNullOrEmpty(p.PersonalNumber) ? p.PersonalNumber : PdfAccompanyingPassport()?.PersonalNumber;
            }
        }

        private Person PdfSpousePersonFromMasterData()
        {
            var emp = Person;
            if (emp is not { IsEmployee: true }) return null;
            if (emp.FamilyMembers == null) return null;
            foreach (var fm in emp.FamilyMembers)
            {
                if (fm == null || ObjectSpace?.IsObjectToDelete(fm) == true) continue;
                if (IsSpouseRelationship(fm.Relationship))
                    return fm;
            }
            return null;
        }

        private static bool IsSpouseRelationship(Relationship r)
        {
            if (r == null) return false;
            if (!string.IsNullOrWhiteSpace(r.Code))
            {
                var c = r.Code.Trim().ToUpperInvariant();
                if (c is "SPOUSE" or "WIFE" or "HUSBAND") return true;
            }
            if (!string.IsNullOrWhiteSpace(r.Name))
            {
                var n = r.Name.ToUpperInvariant();
                if (n.Contains("SPOUSE") || n is "WIFE" or "HUSBAND") return true;
            }
            if (!string.IsNullOrWhiteSpace(r.NameTm))
            {
                var tm = r.NameTm.Trim();
                if (tm.Equals("aýaly", StringComparison.OrdinalIgnoreCase) ||
                    tm.Equals("adamsy", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private ApplicationItem FirstAccompanyingApplicationItemForEmployee()
        {
            if (Application?.ApplicationItems == null || Person is not { IsEmployee: true }) return null;
            return Application.ApplicationItems
                .Where(i => i != null && !i.IsDeleted && i != this && i.Person != null
                    && !i.Person.IsEmployee && i.Person.SponsoringEmployee == Person)
                .OrderBy(i => i.Person.LastName)
                .ThenBy(i => i.Person.FirstName)
                .FirstOrDefault();
        }

        private string PdfAccompanyingRelationshipLabel()
        {
            if (Person is { IsEmployee: true })
                return PdfAccompanyingPerson()?.Relationship?.NameTm;
            return Person?.Relationship?.ReverseNameTm ?? Person?.Relationship?.NameTm;
        }

        private Person PdfAccompanyingPerson()
        {
            if (Person == null) return null;
            if (Person.IsEmployee)
            {
                var item = FirstAccompanyingApplicationItemForEmployee();
                return item?.Person;
            }
            return Person.SponsoringEmployee;
        }

        private Passport PdfAccompanyingPassport()
        {
            if (Person is { IsEmployee: true })
            {
                var item = FirstAccompanyingApplicationItemForEmployee();
                if (item?.CurrentPassport != null) return item.CurrentPassport;
            }
            return PdfAccompanyingPerson()?.CurrentPassport;
        }
        #endregion

        #region FamilyMember display helpers (FM Reports)
        /// <summary>
        /// For FM item reports: "Çaga" if person is under 18, "Orta" if adult family member.
        /// Falls back to Education_LevelTm for employees (IsEmployee = true).
        /// Used in the "Bilimi we okan ýeri" column on FM sanawy reports.
        /// </summary>
        [NotMapped]
        [XafDisplayName("FM Education Level (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string FM_EducationLevelTm
        {
            get
            {
                if (Person?.IsEmployee != false) return Education_LevelTm;
                return (Person.Age < 18) ? "Çaga" : "Orta";
            }
        }

        /// <summary>
        /// For FM item reports: "Çaga" if under 18, "Orta" if adult family member.
        /// Falls back to Education_SpecialtyTm for employees.
        /// Used in the "Bilimine görä hünäri" column on FM sanawy reports.
        /// </summary>
        [NotMapped]
        [XafDisplayName("FM Specialty (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string FM_SpecialtyTm
        {
            get
            {
                if (Person?.IsEmployee != false) return Education_SpecialtyTm;
                return (Person.Age < 18) ? "Çaga" : "Orta";
            }
        }

        /// <summary>
        /// For FM item reports: "[Employee Position] [Employee FullName]-ň [Relationship]".
        /// Example: "Zähmeti goramak we tehniki howpsuzlyk boýunça başlyk Bóra Yolcu-ň gyzy"
        /// Falls back to Position_PositionTm for employees.
        /// Used in the "Wezipesi" column on FM sanawy reports.
        /// </summary>
        [NotMapped]
        [XafDisplayName("FM Wezipesi (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string FM_WezipesiTm
        {
            get
            {
                if (Person?.IsEmployee != false) return Position_PositionTm;
                var emp = Person?.SponsoringEmployee;
                if (emp == null) return Position_PositionTm;
                var pos  = emp.CurrentPositionHistory?.Position?.NameTm ?? string.Empty;
                var name = emp.FullName ?? string.Empty;
                var rel  = Person?.Relationship?.NameTm ?? string.Empty;
                return $"{pos} {name}-\u0148 {rel}".Trim();
            }
        }
        #endregion

        [RuleRequiredField]
        [DataSourceProperty(nameof(AvailablePassports))]
        public virtual Passport CurrentPassport { get; set; }

        [Appearance("PreviousPassportVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousPassport", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailablePassports))]
        public virtual Passport PreviousPassport { get; set; }

        private Visa currentVisa;
        [ImmediatePostData]
        [XafDisplayName("Target Visa")]
        [InverseProperty(nameof(Visa.AssociatedApplicationItems))]
        [Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentVisa", Context = "DetailView,ListView")]
        [ForeignKey(nameof(CurrentVisaId))] // Explicitly define foreign key
        [DataSourceProperty(nameof(AvailableVisas))]
        public virtual Visa CurrentVisa
        {
            get => currentVisa;
            set
            {
                if (currentVisa != value)
                {
                    var oldValue = currentVisa;
                    currentVisa = value;

                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(CurrentVisa), oldValue);
                    }
                }
            }
        }
        // Foreign key property for CurrentVisa
        [Appearance("VisaIdVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentVisa", Context = "DetailView,ListView")]
        public virtual Guid? CurrentVisaId { get; set; }

        private WorkPermitItem currentWorkPermitItem;
        [ImmediatePostData]
        [Appearance("WorkPermitItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentWorkPermitItem", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableWorkPermitItems))]
        public virtual WorkPermitItem CurrentWorkPermitItem
        {
            get => currentWorkPermitItem;
            set
            {
                if (currentWorkPermitItem != value)
                {
                    var oldValue = currentWorkPermitItem;
                    currentWorkPermitItem = value;

                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(CurrentWorkPermitItem), oldValue);
                    }
                }
            }
        }

        [Appearance("SecondWorkPermitItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentWorkPermitItem", Context = "DetailView,ListView")]
        [XafDisplayName("Work Permit Item 2")]
        [DataSourceProperty(nameof(AvailableWorkPermitItems))]
        public virtual WorkPermitItem SecondWorkPermitItem { get; set; }

        private InvitationItem currentInvitationItem;
        [ImmediatePostData]
        [Appearance("InvitationItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentInvitationItem", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableInvitationItems))]
        public virtual InvitationItem CurrentInvitationItem
        {
            get => currentInvitationItem;
            set
            {
                if (currentInvitationItem != value)
                {
                    var oldValue = currentInvitationItem;
                    currentInvitationItem = value;

                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(CurrentInvitationItem), oldValue);
                    }
                }
            }
        }

        [Appearance("AddressOfResidenceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentAddressOfResidence", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableAddressesOfResidence))]
        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        [Appearance("RegistrationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentRegistration", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableRegistrations))]
        public virtual Registration CurrentRegistration { get; set; }

        [Appearance("EmployeeContractVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentEmployeeContract", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableEmployeeContracts))]
        public virtual EmployeeContract CurrentEmployeeContract { get; set; }

        [Appearance("MedicalRecordVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentMedicalRecord", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableMedicalRecords))]
        public virtual MedicalRecord CurrentMedicalRecord { get; set; }

        [Appearance("EducationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentEducation", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableEducations))]
        public virtual Education CurrentEducation { get; set; }

        [Appearance("InvitationIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowInvitationItemIsIssued", Context ="DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool InvitationItemIsIssued { get; set; }

        [Appearance("WorkPermitIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowWorkPermitItemIsIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool WorkPermitItemIsIssued { get; set; }

        [Appearance("RejectionIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowRejectionIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool RejectionIssued { get; set; }

        [Appearance("VisaIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisaIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIssued { get; set; }

		[Appearance("InvitationItemIsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowInvitationItemIsCancelled", Context = "DetailView,ListView")]
		public virtual bool InvitationItemIsCancelled { get; set; }

		[Appearance("IsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowWorkPermitItemIsCancelled", Context = "DetailView,ListView")]
		public virtual bool IsCancelled { get; set; }

		[Appearance("InvitationItemIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowInvitationItemIsChanged", Context = "DetailView,ListView")]
		public virtual bool InvitationItemIsChanged { get; set; }

		[Appearance("WorkPermitItemIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowWorkPermitItemIsChanged", Context = "DetailView,ListView")]
		public virtual bool WorkPermitItemIsChanged { get; set; }

		[Appearance("VisaIsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisaIsCancelled", Context = "DetailView,ListView")]
		[ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIsCancelled { get; set; }

		[Appearance("VisaIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisaIsChanged", Context = "DetailView,ListView")]
		[ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIsChanged { get; set; }

        public virtual bool ApplicationItemsIsCancelled { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion

        [MaxLength(255)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string ApplicationItemName { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            ApplicationItemName = $"{Person?.FullName} - {Application?.FullApplicationNumber}";
        }
    }
}
