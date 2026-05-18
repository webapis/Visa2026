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
    [Appearance("BusinessTripAddressFieldsVisible", Visibility = ViewItemVisibility.Hide,
        Criteria = "Application.ApplicationType is null or !" + BusinessTripWorkflowCriteria,
        TargetItems = "BusinessTripAddress;BusinessTripAddress.City;BusinessTripAddress.FullAddress",
        Context = "DetailView,ListView")]
    public class ApplicationItem : BaseObject, IObjectSpaceLink, ISoftDelete    //10
    {
        /// <summary>Registration workflow types (hasaba almak, check-in/out, info change, etc.).</summary>
        private const string RegistrationWorkflowCriteria =
            "Application.ApplicationType.ShowRegistrations";

        private const string RegistrationTravelFieldsHiddenCriteria =
            "Application.ApplicationType is null or !" + RegistrationWorkflowCriteria;

        /// <summary>Business-trip application types (per-person line uses <see cref="BusinessTripAddress"/>).</summary>
        private const string BusinessTripWorkflowCriteria =
            "Application.ApplicationType.ShowBusinessTrips";

        public ApplicationItem()
        {
        }

        private Application application;

        [RuleRequiredField]
        [ImmediatePostData] // Ensure changes to Application trigger updates
        public virtual Application Application
        {
            get => application;
            set
            {
                if (application != value)
                {
                    application = value;
                    if (application?.ApplicationType != null)
                        ApplyRegistrationMovementDefaults(application.ApplicationType.Name);
                    UpdateApplicationItemName();
                }
            }
        }

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
                        if (Application?.ApplicationType != null)
                            ApplyRegistrationMovementDefaults(Application.ApplicationType.Name);
                    }
                    UpdateApplicationItemName();
                }
            }
        }
        private Person person;

        private void ApplyRegistrationMovementDefaults(string appTypeName)
        {
            switch (appTypeName)
            {
                case "App_Reg_Check_In":
                    TravelType = BusinessObjects.TravelType.External;
                    MovementType = BusinessObjects.MovementType.Entry;
                    break;
                case "App_Reg_Check_Out":
                    TravelType = BusinessObjects.TravelType.External;
                    MovementType = BusinessObjects.MovementType.Exit;
                    break;
                case "App_Reg_Check_In_Internal":
                    TravelType = BusinessObjects.TravelType.Internal;
                    MovementType = BusinessObjects.MovementType.Entry;
                    break;
                case "App_Reg_Check_Out_Internal":
                    TravelType = BusinessObjects.TravelType.Internal;
                    MovementType = BusinessObjects.MovementType.Exit;
                    break;
                default:
                    return;
            }

            if (!TravelDate.HasValue || TravelDate.Value == default)
                TravelDate = DateTime.Today;

            if (!RegistrationDate.HasValue && Application?.ApplicationDate != null)
                RegistrationDate = Application.ApplicationDate;

            if (ObjectSpace == null)
                return;

            CheckPoint ??= ObjectSpace.GetObjectsQuery<CheckPoint>().FirstOrDefault(x => x.IsDefault);
            PurposeOfTravel ??= ObjectSpace.GetObjectsQuery<PurposeOfTravel>().FirstOrDefault(x => x.IsDefault);
        }

        [Appearance("TravelDateVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria, Context = "DetailView,ListView")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? TravelDate { get; set; }

        [Appearance("TravelTypeVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria, Context = "DetailView,ListView")]
        [ModelDefault("AllowEdit", "False")]
        public virtual TravelType? TravelType { get; set; }

        [Appearance("MovementTypeVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria, Context = "DetailView,ListView")]
        [ModelDefault("AllowEdit", "False")]
        public virtual MovementType? MovementType { get; set; }

        [Appearance("TravelCheckPointVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria + " or TravelType != 'External'", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = RegistrationWorkflowCriteria + " and TravelType = 'External'")]
        public virtual CheckPoint CheckPoint { get; set; }

        [Appearance("PurposeOfTravelVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria, Context = "DetailView,ListView")]
        public virtual PurposeOfTravel PurposeOfTravel { get; set; }

        [Appearance("TravelNotesVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria, Context = "DetailView,ListView")]
        public virtual string TravelNotes { get; set; }

        [Appearance("RegistrationDateVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = "Application.ApplicationType is null or !" + RegistrationWorkflowCriteria,
            Context = "DetailView,ListView")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? RegistrationDate { get; set; }

        [Aggregated]
        [Appearance("BusinessTripAddressVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = "Application.ApplicationType is null or !" + BusinessTripWorkflowCriteria,
            Context = "DetailView,ListView")]
        public virtual BusinessTripAddress BusinessTripAddress { get; set; }

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
                PreviousVisa = null;
                PreviousVisaId = null;
                CurrentAddressOfResidence = null;
                CurrentMedicalRecord = null;
                CurrentEducation = null;
                CurrentInvitationItem = null;
                PreviousInvitationItem = null;
                CurrentPositionHistory = null;
                CurrentEmployeeContract = null;
                CurrentWorkDuty = null;
                CurrentWorkPermitItem = null;
                PreviousWorkPermitItem = null;
                return;
            }

            var p = ObjectSpace.GetObject(person);
            CurrentPassport = p.CurrentPassport;
            CurrentVisa = p.CurrentVisa;
            CurrentAddressOfResidence = p.CurrentAddressOfResidence;
            CurrentMedicalRecord = p.CurrentMedicalRecord;
            CurrentEducation = p.CurrentEducation;
            CurrentInvitationItem = p.CurrentInvitationItem;
            PreviousInvitationItem = null;
            PreviousWorkPermitItem = null;

            if (p.IsEmployee)
            {
                CurrentPositionHistory = p.CurrentPositionHistory;
                CurrentEmployeeContract = p.CurrentEmployeeContract;
                CurrentWorkDuty = p.CurrentWorkDuty;
                CurrentWorkPermitItem = p.CurrentWorkPermitItem;
            }
            else
            {
                CurrentPositionHistory = null;
                CurrentEmployeeContract = null;
                CurrentWorkDuty = null;
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
        public IList<WorkDuty> AvailableWorkDuties
        {
            get
            {
                if (person == null) return new List<WorkDuty>();
                return ObjectSpace?.GetObject(person)?.WorkDuties?.ToList() ?? new List<WorkDuty>();
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

        [XafDisplayName("Middle Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_MiddleName => Person?.MiddleName;

        [XafDisplayName("Gender (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_GenderTm => Person?.Gender?.NameTm;

        [XafDisplayName("Marital Status (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_MaritalStatusTm => Person?.MaritalStatus?.NameTm;

        [XafDisplayName("Birth Place"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_BirthPlace => Person?.BirthPlace;

        [XafDisplayName("Foreign Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddress => Person?.ForeignAddress;

        /// <summary>Country code + foreign address for sanawy columns (e.g. <c>TUR, …</c>).</summary>
        [XafDisplayName("Foreign Address with Country"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddressWithCountry
        {
            get
            {
                var code = Person?.ForeignAddressCountry?.Code?.Trim();
                var addr = Person?.ForeignAddress?.Trim();
                if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(addr)) return string.Empty;
                if (string.IsNullOrEmpty(code)) return addr!;
                if (string.IsNullOrEmpty(addr)) return code;
                return $"{code}, {addr}";
            }
        }

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

        /// <summary>Alias for business-trip sanawy templates (<see cref="Position_PositionTm"/>).</summary>
        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_NameTm => Position_PositionTm;
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

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_NumberAndType => string.Join(" ", new[] { CurrentVisa?.VisaNumber, CurrentVisa?.VisaCategory?.NameTm }.Where(s => !string.IsNullOrEmpty(s)));

        /// <summary>
        /// Multiline block for Excel columns like <c>Möhleti we gezekligi</c>: validity start, end,
        /// parenthesised visa number, then <see cref="VisaCategory"/> (NameTm-first, fallback Name) e.g. köp gezeklik.
        /// </summary>
        [XafDisplayName("Visa duration + frequency (multiline)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_DurationFrequencyBlock
        {
            get
            {
                var v = CurrentVisa;
                if (v == null)
                    return string.Empty;

                var lines = new List<string>(4);
                if (v.StartDate != default)
                    lines.Add($"{v.StartDate:dd.MM.yyyy}");

                if (v.ExpirationDate is DateTime expDate)
                    lines.Add($"{expDate:dd.MM.yyyy}");

                if (!string.IsNullOrWhiteSpace(v.VisaNumber))
                    lines.Add($"({v.VisaNumber.Trim()})");

                var categoryDisplay = PreferLookupTmThenName(v.VisaCategory);
                if (!string.IsNullOrWhiteSpace(categoryDisplay))
                    lines.Add(categoryDisplay);

                return lines.Count == 0 ? string.Empty : string.Join(Environment.NewLine, lines);
            }
        }
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

        #region Travel
        [XafDisplayName("Travel Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Travel_Date => TravelDate;

        [XafDisplayName("Travel Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_DateText => $"{TravelDate:dd.MM.yyyy}";

        [XafDisplayName("Travel Purpose of Travel (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_PurposeOfTravelTm => PurposeOfTravel?.NameTm;

        [XafDisplayName("Travel Checkpoint (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_CheckPointTm => CheckPoint?.NameTm;
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

        #region WorkDuty
        [XafDisplayName("Work Duty (Gelmeginiň Maksady)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkDuty_Description => CurrentWorkDuty?.Description;
        #endregion

        #region Education
        [XafDisplayName("Education Graduation Year"), VisibleInDetailView(false), VisibleInListView(false)]
        public int? Education_GraduationYear => CurrentEducation?.GraduationYear;

        [XafDisplayName("Education Level (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_LevelTm => CurrentEducation?.EducationLevel?.NameTm;

        /// <summary>Institution for reports and PDF-style sanawlar; prefers <see cref="LookupBase.NameTm"/>, falls back to <see cref="LookupBase.Name"/>.</summary>
        [XafDisplayName("Education Institution"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_InstitutionName => PreferLookupTmThenName(CurrentEducation?.EducationInstitution);

        [XafDisplayName("Education Specialty (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_SpecialtyTm => CurrentEducation?.Specialty?.NameTm;

        /// <summary>Combined level + institution for forms (Turkmen level + institution NameTm-first); comma-separated when both exist.</summary>
        [XafDisplayName("Education Level and Institution"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_LevelAndInstitutionTm
        {
            get
            {
                var level = Education_LevelTm;
                var inst = Education_InstitutionName;
                var l = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
                var i = string.IsNullOrWhiteSpace(inst) ? null : inst.Trim();
                if (l == null && i == null) return string.Empty;
                if (l == null) return i!;
                if (i == null) return l;
                return $"{l}, {i}";
            }
        }
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

        [XafDisplayName("Previous Work Permit Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousWorkPermit_Number => PreviousWorkPermitItem?.WorkPermitNumber;

        [XafDisplayName("Previous Work Permit Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousWorkPermit_ExpirationDateText => $"{PreviousWorkPermitItem?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region Invitation
        [XafDisplayName("Invitation Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_Number => CurrentInvitationItem?.Invitation?.InvitationNumber;

        [XafDisplayName("Invitation Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_StartDateText => $"{CurrentInvitationItem?.Invitation?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Invitation Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_ExpirationDateText => $"{CurrentInvitationItem?.Invitation?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Invitation Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousInvitation_Number => PreviousInvitationItem?.Invitation?.InvitationNumber;

        [XafDisplayName("Previous Invitation Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousInvitation_StartDateText => $"{PreviousInvitationItem?.Invitation?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Invitation Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousInvitation_ExpirationDateText => $"{PreviousInvitationItem?.Invitation?.ExpirationDate:dd.MM.yyyy}";
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

        /// <summary>Alias for <c>{{ds.VisaPeriod_NameTm}}</c> on ApplicationItem-root Word templates.</summary>
        [Browsable(false)]
        public string VisaPeriod_NameTm => Application_VisaPeriod_NameTm;

        [XafDisplayName("Visa Category (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_VisaCategory_NameTm => Application?.VisaCategory?.NameTm;

        /// <summary>Alias for <c>{{ds.VisaCategory_NameTm}}</c> on ApplicationItem-root Word templates.</summary>
        [Browsable(false)]
        public string VisaCategory_NameTm => Application_VisaCategory_NameTm;

        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_BorderZoneLocation_NameTm => Application?.BorderZoneLocation?.NameTm;

        [XafDisplayName("Application Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_DateText => $"{Application?.ApplicationDate:dd.MM.yyyy}";

        [XafDisplayName("Migration Service Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_MigrationServiceCode => Application?.MigrationService?.Code;

        [XafDisplayName("Registration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_RegistrationDateText => $"{RegistrationDate:dd.MM.yyyy}";

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

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyHead_FullName => CompanyHead_FullName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyHead_PositionTm => CompanyHead_PositionTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string BusinessTripAddress_FullAddress => BusinessTripAddress?.FullAddress;

        [NotMapped]
        [XafDisplayName("Row Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public int RowNumber { get; set; }

        /// <summary>Passport of the application signatory when they are linked as an expat <see cref="Person"/>; local signatories have no passport on the model.</summary>
        private Passport CompanyHeadPassportForReports()
        {
            var ch = Application?.CompanyHead;
            if (ch == null || ch.IsLocalEmployee)
                return null;
            return ch.Employee?.CurrentPassport;
        }

        [NotMapped]
        [XafDisplayName("Signatory Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportNumber => CompanyHeadPassportForReports()?.PassportNumber;

        [NotMapped]
        [XafDisplayName("Signatory Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportAuthority => CompanyHeadPassportForReports()?.Authority;

        [NotMapped]
        [XafDisplayName("Signatory Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportIssueDateText => $"{CompanyHeadPassportForReports()?.IssueDate:dd.MM.yyyy}";

        /// <summary>One line for Borçnama-style forms: number, authority, issue date with year suffix.</summary>
        [NotMapped]
        [XafDisplayName("Signatory Passport (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportLine
        {
            get
            {
                var p = CompanyHeadPassportForReports();
                if (p == null)
                    return string.Empty;
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(p.PassportNumber))
                    parts.Add(p.PassportNumber.Trim());
                if (!string.IsNullOrWhiteSpace(p.Authority))
                    parts.Add(p.Authority.Trim());
                if (p.IssueDate != default)
                    parts.Add($"{p.IssueDate:dd.MM.yyyy}ý.");
                return string.Join(", ", parts);
            }
        }

        private Passport RepresentativePassportForReports()
        {
            var r = Application?.Representative;
            if (r == null || r.IsLocalEmployee)
                return null;
            return r.Employee?.CurrentPassport;
        }

        [NotMapped]
        [XafDisplayName("Representative Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Representative_FullName => Application?.Representative?.FullName;

        [NotMapped]
        [XafDisplayName("Representative Passport (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Representative_PassportLine
        {
            get
            {
                var p = RepresentativePassportForReports();
                if (p == null)
                    return string.Empty;
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(p.PassportNumber))
                    parts.Add(p.PassportNumber.Trim());
                if (!string.IsNullOrWhiteSpace(p.Authority))
                    parts.Add(p.Authority.Trim());
                if (p.IssueDate != default)
                    parts.Add($"{p.IssueDate:dd.MM.yyyy}ý.");
                return string.Join(", ", parts);
            }
        }

        /// <summary>Placeholder until a contact field exists on <see cref="LocalEmployee"/> / representative.</summary>
        [NotMapped]
        [XafDisplayName("Representative Phone"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Representative_Phone => string.Empty;

        /// <summary>Company tax/registration text, address and phone in one line (data entry controls formatting, e.g. №… date).</summary>
        [NotMapped]
        [XafDisplayName("Company registry, address and phone (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyRegistryAddressLine
        {
            get
            {
                var c = Application?.Company;
                if (c == null)
                    return string.Empty;
                var parts = new List<string>();
                if (!string.IsNullOrWhiteSpace(c.TaxInformation))
                    parts.Add(c.TaxInformation.Trim());
                if (!string.IsNullOrWhiteSpace(c.Address))
                    parts.Add(c.Address.Trim());
                if (!string.IsNullOrWhiteSpace(c.PhoneNumber))
                    parts.Add(c.PhoneNumber.Trim());
                return string.Join(" ", parts);
            }
        }
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

        [Appearance("PreviousVisaIdVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousVisa", Context = "DetailView,ListView")]
        public virtual Guid? PreviousVisaId { get; set; }

        [Appearance("PreviousVisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousVisa", Context = "DetailView,ListView")]
        [ForeignKey(nameof(PreviousVisaId))]
        [DataSourceProperty(nameof(AvailableVisas))]
        public virtual Visa PreviousVisa { get; set; }

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

        [Appearance("PreviousWorkPermitItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousWorkPermitItem", Context = "DetailView,ListView")]
        [XafDisplayName("Previous Work Permit Item")]
        [DataSourceProperty(nameof(AvailableWorkPermitItems))]
        public virtual WorkPermitItem PreviousWorkPermitItem { get; set; }

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

        private InvitationItem previousInvitationItem;
        [ImmediatePostData]
        [Appearance("PreviousInvitationItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousInvitationItem", Context = "DetailView,ListView")]
        [XafDisplayName("Previous Invitation Item")]
        [DataSourceProperty(nameof(AvailableInvitationItems))]
        public virtual InvitationItem PreviousInvitationItem
        {
            get => previousInvitationItem;
            set
            {
                if (previousInvitationItem != value)
                {
                    var oldValue = previousInvitationItem;
                    previousInvitationItem = value;
                    if (ObjectSpace != null)
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(PreviousInvitationItem), oldValue);
                }
            }
        }

        [Appearance("AddressOfResidenceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentAddressOfResidence", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableAddressesOfResidence))]
        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        [Appearance("EmployeeContractVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentEmployeeContract", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableEmployeeContracts))]
        public virtual EmployeeContract CurrentEmployeeContract { get; set; }

        [Appearance("WorkDutyVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentWorkDuty", Context = "DetailView,ListView")]
        [DataSourceProperty(nameof(AvailableWorkDuties))]
        public virtual WorkDuty CurrentWorkDuty { get; set; }

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

        [Browsable(false)]
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
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

        [RuleFromBoolProperty("ApplicationItem_PersonUniqueInApplication", DefaultContexts.Save, "This person already has an Application Item in the same Application.")]
        [Browsable(false)]
        public bool IsPersonUniqueInApplication
        {
            get
            {
                if (Person == null || Application == null) return true;
                return !Application.ApplicationItems.Any(ai => ai.ID != ID && !ai.IsDeleted && ai.Person?.ID == Person.ID);
            }
        }

        [MaxLength(255)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string ApplicationItemName { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            UpdateApplicationItemName();
        }

        private void UpdateApplicationItemName()
        {
            ApplicationItemName = Person == null && Application == null
                ? null
                : $"{Person?.FullName} - {Application?.FullApplicationNumber}";
        }

        private static string? PreferLookupTmThenName(LookupBase? lookup)
        {
            if (lookup == null) return null;
            if (!string.IsNullOrWhiteSpace(lookup.NameTm)) return lookup.NameTm.Trim();
            if (!string.IsNullOrWhiteSpace(lookup.Name)) return lookup.Name.Trim();
            return null;
        }
    }
}
