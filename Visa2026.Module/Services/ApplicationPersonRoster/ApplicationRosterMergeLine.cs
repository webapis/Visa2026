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
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Documentation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Detached roster merge/PDF line (from Person + instance ResolvedLinks).
    /// Not an XAF BO — do not use DomainComponent / BaseObject.
    /// </summary>
    public class ApplicationRosterMergeLine
    {
        public Guid ID { get; set; }

        private const string DefaultBorderZoneLocationNameTm = "Ýok";

        /// <summary>Registration workflow types (hasaba almak, check-in/out, info change, etc.).</summary>
        private const string RegistrationWorkflowCriteria =
            "ApplicationProfileInstance.CfgShowRegistrations";

        private const string RegistrationTravelFieldsHiddenCriteria =
            "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowRegistrations";

        /// <summary>Matches <see cref="ApplicationProfileInstance"/> border-zone gate and profile/type configuration.</summary>
        private const string ApplicationItemBorderZoneLocationHiddenCriteria =
            "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowBorderZoneLocation";

        /// <summary>Business-trip application types (per-person line uses <see cref="BusinessTripAddress"/>).</summary>
        private const string BusinessTripWorkflowCriteria =
            "ApplicationProfileInstance.CfgShowBusinessTrips";

        /// <summary>Family-member line (not employee). Used to hide employee document FKs on the item.</summary>
        private const string PersonIsFamilyMemberCriteria =
            "Person Is Not Null And [Person.PersonRole] = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,FamilyMember#";

        private const string RegistrationFamilyMemberContextCriteria =
            "ApplicationProfileInstance is not null And ApplicationProfileInstance.CfgShowRegistrations And "
            + PersonIsFamilyMemberCriteria;

        /// <summary>Any line on a registration workflow application type (education not shown or required).</summary>
        private const string RegistrationApplicationItemContextCriteria =
            ApplicationPresentCriteria + " And " + RegistrationWorkflowCriteria;

        private const string ApplicationPresentCriteria =
            "ApplicationProfileInstance is not null";

        private const string ApplicationTypePresentCriteria =
            "ApplicationProfileInstance is not null And (ApplicationProfileInstance.ApplicationProfile is not null Or ApplicationProfileInstance.ApplicationType is not null)";

        private const string EmployeeApplicationItemLineCriteria =
            "Person Is Not Null And [Person.PersonRole] = ##Enum#Visa2026.Module.BusinessObjects.PersonRecordRole,Employee#";

        private const string ShowPreviousPassportRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowPreviousPassport";

        private const string ShowCurrentVisaRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowCurrentVisa";

        private const string ShowNextVisaRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowNextVisa";

        private const string ShowCurrentWorkPermitItemRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowCurrentWorkPermitItem And "
            + EmployeeApplicationItemLineCriteria;

        private const string ShowPreviousWorkPermitItemRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowPreviousWorkPermitItem";

        private const string ShowCurrentInvitationItemRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowCurrentInvitationItem";

        private const string ShowPreviousInvitationItemRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowPreviousInvitationItem";

        private const string ShowCurrentAddressOfResidenceRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowCurrentAddressOfResidence";

        private const string ShowCurrentWorkDutyRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowCurrentWorkDuty And "
            + EmployeeApplicationItemLineCriteria;

        private const string ShowCurrentSalaryRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowCurrentSalary And "
            + EmployeeApplicationItemLineCriteria;

        private const string ShowCurrentEducationRequiredCriteria =
            ApplicationPresentCriteria + " And ApplicationProfileInstance.CfgShowCurrentEducation And "
            + EmployeeApplicationItemLineCriteria + " And Not ("
            + RegistrationApplicationItemContextCriteria + ")";

        public ApplicationRosterMergeLine()
        {
        }

        /// <summary>
        /// When true, changing <see cref="Person"/> does not run
        /// <see cref="ApplyCurrentFieldsFromSelectedPerson"/> or person-triggered sync rules, and changing
        /// <see cref="CurrentVisa"/>, <see cref="CurrentWorkPermitItem"/>, <see cref="CurrentInvitationItem"/>,
        /// or <see cref="PreviousInvitationItem"/> skips their <see cref="CrossObjectSyncHelper"/> rule dispatch too.
        /// VISA2014 OData import sets this so legacy-mapped FKs (passport, visa, position, …) are kept
        /// without re-running per-property SyncRule evaluation for every imported row.
        /// </summary>
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual bool SuppressPersonCurrentFieldSync { get; set; }

        private ApplicationProfileInstance applicationProfileInstance;

        [RuleRequiredField]
        [ImmediatePostData] // Ensure changes to ApplicationProfileInstance trigger updates
        public virtual ApplicationProfileInstance ApplicationProfileInstance
        {
            get => applicationProfileInstance;
            set
            {
                if (applicationProfileInstance != value)
                {
                    applicationProfileInstance = value;
                    if (applicationProfileInstance?.ApplicationType != null && !SuppressPersonCurrentFieldSync)
                        ApplyRegistrationMovementDefaults(applicationProfileInstance.ApplicationType.Name);
                    ApplyVisibilityGatedReferenceFields();
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
                    if (false && !SuppressPersonCurrentFieldSync)
                    {
                        // Must not rely only on SyncRule + CrossObjectSyncHelper: non-admin users cannot read
                        // SyncRule, so GetObjectsQuery<SyncRule>() is empty and rules never run in production.
                        ApplyCurrentFieldsFromSelectedPerson();
                        // Detached merge line: skip CrossObjectSyncHelper.
                        if (ApplicationProfileInstance?.ApplicationType != null)
                            ApplyRegistrationMovementDefaults(ApplicationProfileInstance.ApplicationType.Name);
                    }
                    UpdateApplicationItemName();
                }
            }
        }
        private Person person;

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        public bool ShowOptionalFields { get; set; }

        /// <summary>Clears reference fields hidden by the parent application type's Show* flags.</summary>
        internal void RefreshVisibilityGatedReferenceFields() =>
            ApplyVisibilityGatedReferenceFields();

        private void ApplyVisibilityGatedReferenceFields()
        {
            var appType = ApplicationProfileInstance?.ApplicationType;
            if (appType == null)
                return;

            if (!appType.ShowCurrentVisa)
            {
                CurrentVisa = null;
                CurrentVisaId = null;
            }

            if (!appType.ShowNextVisa)
            {
                NextVisa = null;
                NextVisaId = null;
            }

            if (!appType.ShowWorkPermittedLocations)
                WorkPermittedLocations = string.Empty;
        }

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

            if (!RegistrationDate.HasValue && ApplicationProfileInstance?.ApplicationDate != null)
                RegistrationDate = ApplicationProfileInstance.ApplicationDate;

            // Detached merge line: no ObjectSpace — skip CheckPoint default lookup.
        }

        [Appearance("TravelDateVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria, Context = "DetailView,ListView")]
        [ExcludeFromOptionalDetailFields]
        [RuleRequiredField(TargetCriteria = RegistrationWorkflowCriteria)]
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
        [ExcludeFromOptionalDetailFields]
        [RuleRequiredField(TargetCriteria = RegistrationWorkflowCriteria + " and TravelType = 'External'")]
        public virtual CheckPoint CheckPoint { get; set; }

        [Appearance("TravelNotesVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationTravelFieldsHiddenCriteria, Context = "DetailView,ListView")]
        public virtual string TravelNotes { get; set; }

        [Appearance("RegistrationDateVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = "ApplicationProfileInstance is null or !" + RegistrationWorkflowCriteria,
            Context = "DetailView,ListView")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? RegistrationDate { get; set; }

        [Aggregated]
        [ExcludeFromOptionalDetailFields]
        [Appearance("BusinessTripAddressVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = "ApplicationProfileInstance is null or !" + BusinessTripWorkflowCriteria,
            Context = "DetailView,ListView")]
        public virtual BusinessTripAddress BusinessTripAddress { get; set; }
        [RuleFromBoolProperty(
            "ApplicationItem_BusinessTripAddressValid",
            DefaultContexts.Save,
            "Business trip city and full address are required.",
            TargetCriteria = BusinessTripWorkflowCriteria)]
        public bool IsBusinessTripAddressValid =>
            BusinessTripAddress?.City != null
            && !string.IsNullOrWhiteSpace(BusinessTripAddress?.FullAddress);

        [Appearance("ApplicationItem_BorderZoneLocationVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = ApplicationItemBorderZoneLocationHiddenCriteria, Context = "DetailView,ListView")]
        [ExcludeFromOptionalDetailFields]
        [VisibleInListView(false)]
        [MaxLength(500)]
        [EditorAlias(Editors.CommaSeparatedMultiSelectEditorAliases.BorderZone)]
        [Editors.CommaSeparatedMultiSelect(
            CatalogEntityType = typeof(BorderZoneName),
            NoneValue = Services.CommaSeparatedSelectionHelper.NoneValue)]
        public virtual string BorderZoneLocation { get; set; }
        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string BorderZoneLocation_NameTm =>
            Services.BorderZoneSelectionHelper.IsNoneValue(BorderZoneLocation)
                ? DefaultBorderZoneLocationNameTm
                : BorderZoneLocation?.Trim() ?? DefaultBorderZoneLocationNameTm;

        [VisibleInListView(false)]
        [MaxLength(500)]
        [Appearance("WorkPermittedLocationsVisible", Visibility = ViewItemVisibility.Hide,
            Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowWorkPermittedLocations",
            Context = "DetailView,ListView")]
        [EditorAlias(Editors.CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation)]
        [Editors.CommaSeparatedMultiSelect(
            CatalogEntityType = typeof(WorkPermittedLocationName),
            NoneValue = "")]
        public virtual string WorkPermittedLocations { get; set; }

        /// <summary>
        /// Copies <see cref="Person"/>'s current document links into this item when <see cref="Person"/> changes.
        /// Mirrors <see cref="Visa2026.Module.DatabaseUpdate.SyncRulesUpdater"/> "Pull * from Person" rules so behavior does not
        /// depend on the current user having read access to <see cref="SyncRule"/>.
        /// </summary>
        private void ApplyCurrentFieldsFromSelectedPerson()
        {
            if (null == null)
                return;

            if (person == null)
            {
                CurrentPassport = null;
                CurrentVisa = null;
                NextVisa = null;
                NextVisaId = null;
                CurrentAddressOfResidence = null;
                CurrentMedicalRecord = null;
                CurrentEducation = null;
                CurrentInvitationItem = null;
                PreviousInvitationItem = null;
                CurrentPositionHistory = null;
                CurrentSalary = null;
                CurrentWorkDuty = null;
                CurrentWorkPermitItem = null;
                PreviousWorkPermitItem = null;
                WorkPermittedLocations = string.Empty;
                return;
            }

            var p = person;
            CurrentPassport = PersonCurrentItems.GetCurrentPassport(p);
            // Visa is date-effective: "current" is effective now; "next" is the nearest future visa (if any).
            var asOf = (ApplicationProfileInstance?.ApplicationDate ?? DateTime.Today).Date;
            var visas = p.Passports?
                .Where(pp => pp != null)
                .SelectMany(pp => pp.Visas ?? Array.Empty<Visa>())
                .Where(v => v != null && !v.IsCancelled && v.StartDate != default)
                .ToList() ?? new List<Visa>();

            var currentVisa = visas
                .Where(v => v.StartDate.Date <= asOf)
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .FirstOrDefault();

            var nextVisa = visas
                .Where(v => v.StartDate.Date > asOf)
                .OrderBy(v => v.StartDate.Date)
                .ThenBy(v => v.IssueDate.Date)
                .FirstOrDefault();

            var appType = ApplicationProfileInstance?.ApplicationType;
            if (appType?.ShowCurrentVisa == true)
                CurrentVisa = currentVisa ?? PersonCurrentItems.GetCurrentVisa(p, asOf);
            else
            {
                CurrentVisa = null;
                CurrentVisaId = null;
            }

            if (appType?.ShowNextVisa == true)
                NextVisa = nextVisa;
            else
            {
                NextVisa = null;
                NextVisaId = null;
            }

            CurrentAddressOfResidence = PersonCurrentItems.GetCurrentAddressOfResidence(p);
            CurrentMedicalRecord = PersonCurrentItems.GetCurrentMedicalRecord(p);
            CurrentInvitationItem = PersonCurrentItems.GetCurrentInvitationItem(p);
            PreviousInvitationItem = null;
            PreviousWorkPermitItem = null;

            if (p.IsEmployee)
            {
                CurrentEducation = PersonCurrentItems.GetCurrentEducation(p);
                CurrentPositionHistory = PersonCurrentItems.GetCurrentPositionHistory(p);
                CurrentSalary = PersonCurrentItems.GetCurrentSalary(p);
                CurrentWorkDuty = PersonCurrentItems.GetCurrentWorkDuty(p);
                CurrentWorkPermitItem = PersonCurrentItems.GetCurrentWorkPermitItem(p);
                if (ApplicationProfileInstance?.ApplicationType?.ShowWorkPermittedLocations == true)
                    WorkPermittedLocations = PersonCurrentItems.GetCurrentWorkPermitItem(p)?.WorkPermittedLocations ?? string.Empty;
            }
            else
            {
                CurrentEducation = null;
                CurrentPositionHistory = null;
                CurrentSalary = null;
                CurrentWorkDuty = null;
                CurrentWorkPermitItem = null;
                WorkPermittedLocations = string.Empty;
            }
        }
        [NotMapped]
        public IList<Person> AvailablePeople
        {
            get
            {
                IObjectSpace objectSpace = null;
                if (objectSpace == null) return new List<Person>();

                if (ApplicationProfileInstance == null) return new List<Person>();

                var query = objectSpace.GetObjectsQuery<Person>();

                var excludedPersonIds = new HashSet<Guid>();
                if (excludedPersonIds.Count > 0)
                    query = query.Where(p => !excludedPersonIds.Contains(p.ID));

                var category = ApplicationProfileInstance.ApplicationType?.Category;
                if (category == ApplicationTypeCategory.Both || category == null)
                {
                    return query
                        .OrderBy(p => p.LastName)
                        .ThenBy(p => p.FirstName)
                        .ToList();
                }

                bool isEmployee = category == ApplicationTypeCategory.Employee;
                return query
                    .Where(p => p.IsEmployee == isEmployee)
                    .OrderBy(p => p.LastName)
                    .ThenBy(p => p.FirstName)
                    .ToList();
            }
        }

        [NotMapped]
        public IList<Passport> AvailablePassports
        {
            get
            {
                if (person == null) return new List<Passport>();
                return new List<Passport>();
            }
        }

        [NotMapped]
        public IList<EmployeePositionHistory> AvailablePositionHistories
        {
            get
            {
                if (person == null) return new List<EmployeePositionHistory>();
                return new List<EmployeePositionHistory>();
            }
        }

        [NotMapped]
        public IList<Visa> AvailableVisas
        {
            get
            {
                if (person == null) return new List<Visa>();
                return new List<Visa>();
            }
        }

        [NotMapped]
        public IList<WorkPermitItem> AvailableWorkPermitItems
        {
            get
            {
                if (person == null) return new List<WorkPermitItem>();
                return new List<WorkPermitItem>();
            }
        }

        [NotMapped]
        public IList<InvitationItem> AvailableInvitationItems
        {
            get
            {
                if (person == null) return new List<InvitationItem>();
                return new List<InvitationItem>();
            }
        }

        [NotMapped]
        public IList<AddressOfResidence> AvailableAddressesOfResidence
        {
            get
            {
                if (person == null) return new List<AddressOfResidence>();
                return new List<AddressOfResidence>();
            }
        }

        [NotMapped]
        public IList<WorkDuty> AvailableWorkDuties
        {
            get
            {
                if (person == null) return new List<WorkDuty>();
                return new List<WorkDuty>();
            }
        }

        [NotMapped]
        public IList<EmployeeSalary> AvailableSalaries
        {
            get
            {
                if (person == null) return new List<EmployeeSalary>();
                return new List<EmployeeSalary>();
            }
        }

        [NotMapped]
        public IList<MedicalRecord> AvailableMedicalRecords
        {
            get
            {
                if (person == null) return new List<MedicalRecord>();
                return new List<MedicalRecord>();
            }
        }

        [NotMapped]
        public IList<Education> AvailableEducations
        {
            get
            {
                if (person == null) return new List<Education>();
                return new List<Education>();
            }
        }

        [Appearance("CurrentPositionHistoryEmployeeOnly", Visibility = ViewItemVisibility.Hide,
            Criteria = PersonIsFamilyMemberCriteria, Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = EmployeeApplicationItemLineCriteria)]
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

        [XafDisplayName("Foreign Address Country Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddressCountryCode => Person?.ForeignAddressCountry?.Code;

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

        /// <summary>
        /// Stacked visa numbers for <c>wiza_yatyrylmak_sanaw.docx</c>: <see cref="CurrentVisa"/> line first, then <see cref="NextVisa"/> when set (same table row).
        /// </summary>
        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CancelVisa_NumberBlock =>
            JoinVisaFieldLines(CurrentVisa?.VisaNumber, NextVisa?.VisaNumber);

        /// <summary>Stacked visa validity start dates (CurrentVisa then NextVisa).</summary>
        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CancelVisa_StartDateBlock =>
            JoinVisaFieldLines(FormatVisaDateText(CurrentVisa?.StartDate), FormatVisaDateText(NextVisa?.StartDate));

        /// <summary>Stacked visa validity end dates (CurrentVisa then NextVisa).</summary>
        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CancelVisa_ExpirationDateBlock =>
            JoinVisaFieldLines(
                FormatVisaDateText(CurrentVisa?.ExpirationDate),
                FormatVisaDateText(NextVisa?.ExpirationDate));

        private static string FormatVisaDateText(DateTime? date) =>
            date is DateTime d && d != default ? $"{d:dd.MM.yyyy}" : string.Empty;

        private static string JoinVisaFieldLines(params string?[] parts) =>
            string.Join(Environment.NewLine, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        #endregion

        #region Address
        [XafDisplayName("Address Full Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_FullAddress => CurrentAddressOfResidence?.FullAddress;

        [XafDisplayName("Address Type"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_Type => CurrentAddressOfResidence?.Type?.ToString();

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

        /// <summary>Alias for reports; registration travel purpose uses <see cref="CurrentPositionHistory"/>.</summary>
        [XafDisplayName("Travel Purpose of Travel (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_PurposeOfTravelTm => Position_PositionTm;

        [XafDisplayName("Travel Checkpoint (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Travel_CheckPointTm => CheckPoint?.NameTm;
        #endregion

        #region Registration report fields (Forma 16, RegistrationList)
        [XafDisplayName("Is Employee"), VisibleInDetailView(false), VisibleInListView(false)]
        public bool Person_IsEmployee => Person?.IsEmployee ?? false;

        [XafDisplayName("Relationship (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_RelationshipTm => Person?.Relationship?.NameTm;

        [XafDisplayName("Sponsoring Employee Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_SponsoringEmployeeFullName => Person?.SponsoringEmployee?.FullName;

        [XafDisplayName("Sponsoring Employee Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_SponsoringEmployeePositionTm =>
            PersonCurrentItems.GetCurrentPositionHistory(Person?.SponsoringEmployee)?.Position?.NameTm;

        /// <summary>
        /// Forma 16 §8 / <see cref="Reports.RegistrationForm16Report"/>: employee → <see cref="Position_PositionTm"/>;
        /// family member → <c>position-fullName-relationship</c> (dash-separated), e.g.
        /// <c>Türkmenistandaky şahamça müdiriniň orunbasary-Ali Enes Yetkin-ayaly</c>.
        /// Detail UI: read-only context for family members on registration applications (replaces empty position lookup).
        /// </summary>
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [ModelDefault("AllowEdit", "False")]
        [Appearance("RegistrationGelmeginMaksadyTmVisible", Visibility = ViewItemVisibility.Show,
            Criteria = RegistrationFamilyMemberContextCriteria, Context = "DetailView")]
        public string Registration_GelmeginMaksadyTm
        {
            get
            {
                if (Person?.IsEmployee == true)
                    return Position_PositionTm ?? string.Empty;

                return JoinRegistrationGelmeginFamilyMemberLine(
                    Person_SponsoringEmployeePositionTm,
                    Person_SponsoringEmployeeFullName,
                    Person_RelationshipTm);
            }
        }

        private static string JoinRegistrationGelmeginFamilyMemberLine(
            string? positionTm,
            string? employeeFullName,
            string? relationshipTm)
        {
            var parts = new[] { positionTm, employeeFullName, relationshipTm }
                .Select(static p => p?.Trim())
                .Where(static p => !string.IsNullOrEmpty(p))
                .ToArray();
            return parts.Length == 0 ? string.Empty : string.Join("-", parts);
        }
        #endregion

        #region Contract
        [XafDisplayName("Contract Salary"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Contract_Salary =>
            CurrentSalary?.Amount ?? PersonCurrentItems.GetCurrentSalary(Person)?.Amount ?? string.Empty;

        [XafDisplayName("Contract Salary (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Contract_SalaryText =>
            CurrentSalary?.Amount ?? PersonCurrentItems.GetCurrentSalary(Person)?.Amount ?? string.Empty;

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
                var months = ApplicationProfileInstance?.VisaPeriod?.PdfForm_Count;
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

                var months = ApplicationProfileInstance?.VisaPeriod?.PdfForm_Count;
                if (months is null || months <= 0)
                {
                    return string.Empty;
                }

                return $"Rugsatnamanyň başlaýan gününden {months} aý möhleti bilen güýje girer.";
            }
        }

        [XafDisplayName("Salary Currency Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Salary_CurrencyCode =>
            (CurrentSalary?.Currency ?? PersonCurrentItems.GetCurrentSalary(Person)?.Currency)?.ToString();

        [XafDisplayName("Company Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyAddress =>
            OrganizationReportHelper.GetCompanyProfile(OrganizationReportHelper.ResolveObjectSpace(null, ApplicationProfileInstance))?.Address ?? string.Empty;
        #endregion

        #region WorkDuty
        [XafDisplayName("Work Duty (Gelmeginiň Maksady)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkDuty_Description => CurrentWorkDuty?.Description;
        #endregion

        #region Education
        [XafDisplayName("Education Graduation Year"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_GraduationYear => CurrentEducation?.GraduationYear;

        [XafDisplayName("Education Level (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_LevelTm => CurrentEducation?.EducationLevel?.NameTm;

        /// <summary>Institution for reports and PDF-style sanawlar; prefers <see cref="LookupBase.NameTm"/>, falls back to <see cref="LookupBase.Name"/>.</summary>
        [XafDisplayName("Education Institution"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_InstitutionName => PreferLookupTmThenName(CurrentEducation?.EducationInstitution);

        [XafDisplayName("Education Specialty (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_SpecialtyTm => CurrentEducation?.Specialty?.NameTm;

        [XafDisplayName("Education Country Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_CountryCode => CurrentEducation?.EducationCountry?.Code;

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

        /// <summary>
        /// Item 21 — Okan (okaýan) ýeri on the TM visa XFA PDF, e.g. <c>TUR, SOMA LINYIT YORITE ORTA HUNARMENLIK MEKDEBI</c>.
        /// </summary>
        [NotMapped]
        [XafDisplayName("PDF Education Place of Study"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_EducationPlaceOfStudy => BuildPdfEducationPlaceOfStudy();

        private string BuildPdfEducationPlaceOfStudy()
        {
            if (CurrentEducation == null)
            {
                return null;
            }

            var code = Education_CountryCode?.Trim();
            var institution = Education_InstitutionName?.Trim();
            if (string.IsNullOrEmpty(code) && string.IsNullOrEmpty(institution))
            {
                return null;
            }

            if (string.IsNullOrEmpty(code))
            {
                return institution;
            }

            if (string.IsNullOrEmpty(institution))
            {
                return code;
            }

            return $"{code}, {institution}";
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
        public string WorkPermit_WorkPermittedLocations =>
            !string.IsNullOrWhiteSpace(WorkPermittedLocations)
                ? WorkPermittedLocations
                : CurrentWorkPermitItem?.WorkPermittedLocations;

        [XafDisplayName("Previous Work Permit Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousWorkPermit_Number => PreviousWorkPermitItem?.WorkPermitNumber;

        [XafDisplayName("Previous Work Permit Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousWorkPermit_ExpirationDateText => $"{PreviousWorkPermitItem?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region Invitation
        [XafDisplayName("Invitation Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_Number => CurrentInvitationItem?.Invitation?.InvitationNumber;

        [XafDisplayName("Invitation Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        /// <summary>Invitation formalization date (legacy Resmileşdirilen sene) — <see cref="Invitation.IssuedDate"/>.</summary>
        public string Invitation_StartDateText => $"{CurrentInvitationItem?.Invitation?.IssuedDate:dd.MM.yyyy}";

        [XafDisplayName("Invitation Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Invitation_ExpirationDateText => $"{CurrentInvitationItem?.Invitation?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Invitation Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousInvitation_Number => PreviousInvitationItem?.Invitation?.InvitationNumber;

        [XafDisplayName("Previous Invitation Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        /// <summary>Previous invitation formalization date — <see cref="Invitation.IssuedDate"/>.</summary>
        public string PreviousInvitation_StartDateText => $"{PreviousInvitationItem?.Invitation?.IssuedDate:dd.MM.yyyy}";

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

        #region ApplicationProfileInstance
        [XafDisplayName("ApplicationProfileInstance Full Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_FullNumber => ApplicationProfileInstance?.FullApplicationNumber;

        [XafDisplayName("Visa Period (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_VisaPeriod_NameTm => ApplicationProfileInstance?.VisaPeriod?.NameTm;

        /// <summary>Alias for <c>{{ds.VisaPeriod_NameTm}}</c> on ApplicationItem-root Word templates.</summary>
        public string VisaPeriod_NameTm => Application_VisaPeriod_NameTm;

        [XafDisplayName("Visa Category (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_VisaCategory_NameTm => ApplicationProfileInstance?.VisaCategory?.NameTm;

        /// <summary>Alias for <c>{{ds.VisaCategory_NameTm}}</c> on ApplicationItem-root Word templates.</summary>
        public string VisaCategory_NameTm => Application_VisaCategory_NameTm;

        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_BorderZoneLocation_NameTm =>
            ApplicationProfileInstance?.BorderZoneLocation_NameTm ?? DefaultBorderZoneLocationNameTm;
        [XafDisplayName("Item Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Item_BorderZoneLocation_NameTm => BorderZoneLocation_NameTm;

        [XafDisplayName("ApplicationProfileInstance Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_DateText => $"{ApplicationProfileInstance?.ApplicationDate:dd.MM.yyyy}";

        [XafDisplayName("Migration Service Code"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_MigrationServiceCode => ApplicationProfileInstance?.MigrationService?.Code;

        [XafDisplayName("Registration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_RegistrationDateText => $"{RegistrationDate:dd.MM.yyyy}";

        [XafDisplayName("Sponsor Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_SponsorName =>
            OrganizationReportHelper.GetCompanyProfile(OrganizationReportHelper.ResolveObjectSpace(null, ApplicationProfileInstance))?.Name ?? string.Empty;

        [XafDisplayName("Sponsor Authorized Signatory"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_SponsorSignatory => ApplicationProfileInstance?.Application_CompanyHead_FullName ?? string.Empty;
        #endregion

        #region CompanyHead (Signatory)
        [NotMapped]
        [XafDisplayName("Signatory Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_FullName => ApplicationProfileInstance?.Application_CompanyHead_FullName ?? string.Empty;

        [NotMapped]
        [XafDisplayName("Signatory Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PositionTm => ApplicationProfileInstance?.Application_CompanyHead_PositionTm ?? string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyHead_FullName => CompanyHead_FullName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyHead_PositionTm => CompanyHead_PositionTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string BusinessTripAddress_FullAddress => BusinessTripAddress?.FullAddress;

        [NotMapped]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public int RowNumber { get; set; }

        private AuthorizedSignatory? SignatoryForReports() =>
            OrganizationReportHelper.GetSignatory(OrganizationReportHelper.ResolveObjectSpace(null, ApplicationProfileInstance));

        private AuthorizedRepresentative? RepresentativeForReports() =>
            OrganizationReportHelper.GetRepresentative(OrganizationReportHelper.ResolveObjectSpace(null, ApplicationProfileInstance));

        [NotMapped]
        [XafDisplayName("Signatory Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportNumber => SignatoryForReports()?.PassportNumber ?? string.Empty;

        [NotMapped]
        [XafDisplayName("Signatory Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportAuthority => SignatoryForReports()?.PassportAuthority ?? string.Empty;

        [NotMapped]
        [XafDisplayName("Signatory Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportIssueDateText
        {
            get
            {
                var d = SignatoryForReports()?.PassportIssueDate;
                return d is { } date && date != default ? $"{date:dd.MM.yyyy}" : string.Empty;
            }
        }

        /// <summary>One line for Borçnama-style forms: number, authority, issue date with year suffix.</summary>
        [NotMapped]
        [XafDisplayName("Signatory Passport (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PassportLine => SignatoryForReports()?.PassportLine ?? string.Empty;

        [NotMapped]
        [XafDisplayName("Representative Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Representative_FullName =>
            RepresentativeForReports()?.FullName ?? string.Empty;

        [NotMapped]
        [XafDisplayName("Representative Passport (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Representative_PassportLine => RepresentativeForReports()?.PassportLine ?? string.Empty;

        [NotMapped]
        [XafDisplayName("Representative Phone"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Representative_Phone => RepresentativeForReports()?.Phone ?? string.Empty;

        /// <summary>Company tax/registration text, address and phone in one line (data entry controls formatting, e.g. №… date).</summary>
        [NotMapped]
        [XafDisplayName("Company registry, address and phone (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_CompanyRegistryAddressLine
        {
            get
            {
                var c = OrganizationReportHelper.GetCompanyProfile(
                    OrganizationReportHelper.ResolveObjectSpace(null, ApplicationProfileInstance));
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

        #region PDF Visa ApplicationProfileInstance (XFA) — family members aggregate
        /// <summary>
        /// Full family list for the TM visa PDF from the sponsoring employee's
        /// <see cref="Person.VisaApplicationFamilyMembersText"/> only (no fallback to linked
        /// <see cref="Person.FamilyMembers"/>). For a family-member item, uses the sponsor's manual text.
        /// </summary>
        [NotMapped]
        [XafDisplayName("PDF Family Members Aggregate"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_FamilyMembersAggregateText => BuildPdfFamilyMembersAggregateText();

        [NotMapped]
        [XafDisplayName("PDF Family Members Marital Line 1"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_FamilyMembersMaritalLine1 => GetVisaPdfMaritalFamilyLines().line1;

        [NotMapped]
        [XafDisplayName("PDF Family Members Marital Line 2"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_FamilyMembersMaritalLine2 => GetVisaPdfMaritalFamilyLines().line2;

        [NotMapped]
        [XafDisplayName("PDF Family Members Marital Line 3"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_FamilyMembersMaritalLine3 => GetVisaPdfMaritalFamilyLines().line3;

        private string BuildPdfFamilyMembersAggregateText() =>
            VisaFamilyMemberLinesHelper.FormatVisaPdfMaritalFamilyBlockFromRows(BuildVisaPdfMaritalFamilyRows());

        private (string line1, string line2, string line3) GetVisaPdfMaritalFamilyLines()
        {
            var rows = BuildVisaPdfMaritalFamilyRows();
            return rows == null || rows.Count == 0
                ? (null, null, null)
                : VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(rows);
        }

        private IReadOnlyList<VisaFamilyMemberLineDto> BuildVisaPdfMaritalFamilyRows()
        {
            var emp = PdfEmployeeForHouseholdOnVisaForm();
            if (emp == null
                || VisaFamilyMemberLinesHelper.IsManualVisaFamilyEmpty(emp.VisaApplicationFamilyMembersText))
            {
                return Array.Empty<VisaFamilyMemberLineDto>();
            }

            return VisaFamilyMemberLinesHelper.Parse(emp.VisaApplicationFamilyMembersText);
        }

        /// <summary>Employee whose household is listed on the visa form (applicant or sponsor).</summary>
        private Person PdfEmployeeForHouseholdOnVisaForm()
        {
            if (Person == null) return null;
            return Person.IsEmployee ? Person : Person.SponsoringEmployee;
        }

        /// <summary>
        /// Maşgala ýagdaýy line for <c>sahsy_kagyz.docx</c> from manual
        /// <see cref="Person.VisaApplicationFamilyMembersText"/> only.
        /// </summary>
        [NotMapped]
        [XafDisplayName("Şahsy Kagyz Family Status"), VisibleInDetailView(false), VisibleInListView(false)]
        public string SahsyKagyz_FamilyStatusText => BuildSahsyKagyzFamilyStatusText();

        private string BuildSahsyKagyzFamilyStatusText()
        {
            var emp = PdfEmployeeForHouseholdOnVisaForm();
            if (emp == null)
            {
                return string.Empty;
            }

            return VisaFamilyMemberLinesHelper.FormatSahsyKagyzFamilyStatus(emp.VisaApplicationFamilyMembersText)?.Trim()
                ?? string.Empty;
        }
        #endregion

        #region PDF Visa ApplicationProfileInstance (XFA) — spouse & accompanying travellers
        /// <summary>
        /// Spouse row on the TM visa PDF from the employee's manual family line whose relationship is spouse.
        /// </summary>
        [NotMapped]
        [XafDisplayName("PDF Spouse Last Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_SpouseLastName
        {
            get
            {
                var spouse = PdfSpouseManualLine();
                if (spouse == null)
                {
                    return null;
                }

                VisaFamilyMemberLinesHelper.SplitFullNameForPdf(spouse.FullName, out _, out var lastName);
                return lastName;
            }
        }

        [NotMapped]
        [XafDisplayName("PDF Spouse First Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_SpouseFirstName
        {
            get
            {
                var spouse = PdfSpouseManualLine();
                if (spouse == null)
                {
                    return null;
                }

                VisaFamilyMemberLinesHelper.SplitFullNameForPdf(spouse.FullName, out var firstName, out _);
                return firstName;
            }
        }

        [NotMapped]
        [XafDisplayName("PDF Spouse Additional"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Pdf_SpouseAdditional
        {
            get
            {
                var spouse = PdfSpouseManualLine();
                if (spouse?.BirthDate == null)
                {
                    return null;
                }

                return spouse.BirthDate.Value.ToString("dd.MM.yyyy");
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

        private VisaFamilyMemberLineDto? PdfSpouseManualLine()
        {
            var emp = Person;
            if (emp is not { IsEmployee: true })
            {
                return null;
            }

            return VisaFamilyMemberLinesHelper.FindSpouseLine(
                emp.VisaApplicationFamilyMembersText,
                null);
        }

        private ApplicationRosterMergeLine FirstAccompanyingApplicationItemForEmployee()
        {
            if (ApplicationProfileInstance?.People == null || Person is not { IsEmployee: true }) return null;
            var sponsorId = Person.ID;
            var companion = ApplicationProfileInstance.People
                .Where(ap => ap != null && !ap.IsEmployee && ap.SponsoringEmployee?.ID == sponsorId)
                .OrderBy(p => p.LastName)
                .ThenBy(p => p.FirstName)
                .FirstOrDefault();
            if (companion == null) return null;
            return new ApplicationRosterMergeLine
            {
                ApplicationProfileInstance = ApplicationProfileInstance,
                Person = companion,
                ApplicationItemName = companion.FullName ?? string.Empty,
            };
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
            return PersonCurrentItems.GetCurrentPassport(PdfAccompanyingPerson());
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
                var pos  = PersonCurrentItems.GetCurrentPositionHistory(emp)?.Position?.NameTm ?? string.Empty;
                var name = emp.FullName ?? string.Empty;
                var rel  = Person?.Relationship?.NameTm ?? string.Empty;
                return $"{pos} {name}-\u0148 {rel}".Trim();
            }
        }
        #endregion

        [RuleRequiredField]
        [DataSourceProperty(nameof(AvailablePassports))]
        public virtual Passport CurrentPassport { get; set; }

        [Appearance("PreviousPassportVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowPreviousPassport", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowPreviousPassportRequiredCriteria)]
        [DataSourceProperty(nameof(AvailablePassports))]
        public virtual Passport PreviousPassport { get; set; }

        [Appearance("NextVisaIdVisible", Visibility = ViewItemVisibility.Hide, Criteria =
            "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowNextVisa",
            Context = "DetailView,ListView")]
        public virtual Guid? NextVisaId { get; set; }

        [Appearance("NextVisaVisible", Visibility = ViewItemVisibility.Hide, Criteria =
            "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowNextVisa",
            Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowNextVisaRequiredCriteria)]
        [ForeignKey(nameof(NextVisaId))]
        [DataSourceProperty(nameof(AvailableVisas))]
        public virtual Visa NextVisa { get; set; }

        private Visa currentVisa;
        [ImmediatePostData]
        // ListView column visibility is model/controller-driven: nested Appearance cannot reliably
        // resolve ApplicationProfileInstance.ApplicationType (often null → column stays hidden even when ShowCurrentVisa).
        [Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentVisa", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = ShowCurrentVisaRequiredCriteria)]
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

                    if (false && !SuppressPersonCurrentFieldSync)
                    {
                        // Detached merge line: skip CrossObjectSyncHelper.
                    }
                }
            }
        }
        // Foreign key property for CurrentVisa
        [Appearance("VisaIdVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentVisa", Context = "DetailView")]
        public virtual Guid? CurrentVisaId { get; set; }

        /// <summary>
        /// Visa issued from this roster line (legacy projection field). Typically 0–1.
        /// Distinct from <see cref="CurrentVisa"/> (predecessor / target of this application line).
        /// Read-only ListView/Detail column; cell tint follows visa <see cref="Visa.StateSeverityLevel"/>.
        /// </summary>
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(true)]
        [VisibleInLookupListView(false)]
        [ToolTip("Visa issued from this application item (Visa.IssuingApplicationProfileInstance). Not the Current Visa predecessor.")]
        [Appearance("ApplicationItem_IssuedVisa_Info", Priority = 100, AppearanceItemType = "ViewItem", TargetItems = "IssuedVisa",
            Criteria = "IssuedVisa is not null and IssuedVisa.StateSeverityLevel = 1", Context = "ListView", BackColor = "LightSkyBlue")]
        [Appearance("ApplicationItem_IssuedVisa_Warning", Priority = 200, AppearanceItemType = "ViewItem", TargetItems = "IssuedVisa",
            Criteria = "IssuedVisa is not null and IssuedVisa.StateSeverityLevel = 2", Context = "ListView", BackColor = "LightSalmon")]
        [Appearance("ApplicationItem_IssuedVisa_Critical", Priority = 300, AppearanceItemType = "ViewItem", TargetItems = "IssuedVisa",
            Criteria = "IssuedVisa is not null and IssuedVisa.StateSeverityLevel >= 3", Context = "ListView", BackColor = "LightCoral")]
        public virtual Visa IssuedVisa { get; set; }

        private WorkPermitItem currentWorkPermitItem;
        [ImmediatePostData]
        [Appearance("CurrentWorkPermitItemEmployeeOnly", Visibility = ViewItemVisibility.Hide,
            Criteria = PersonIsFamilyMemberCriteria, Context = "DetailView,ListView")]
        [Appearance("WorkPermitItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentWorkPermitItem", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowCurrentWorkPermitItemRequiredCriteria)]
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
                    if (value != null && ApplicationProfileInstance?.ApplicationType?.ShowWorkPermittedLocations == true)
                        WorkPermittedLocations = value.WorkPermittedLocations ?? string.Empty;

                    if (false && !SuppressPersonCurrentFieldSync)
                    {
                        // Detached merge line: skip CrossObjectSyncHelper.
                    }
                }
            }
        }

        [Appearance("PreviousWorkPermitItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowPreviousWorkPermitItem", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowPreviousWorkPermitItemRequiredCriteria)]
        [DataSourceProperty(nameof(AvailableWorkPermitItems))]
        public virtual WorkPermitItem PreviousWorkPermitItem { get; set; }

        private InvitationItem currentInvitationItem;
        [ImmediatePostData]
        [Appearance("InvitationItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentInvitationItem", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowCurrentInvitationItemRequiredCriteria)]
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

                    if (false && !SuppressPersonCurrentFieldSync)
                    {
                        // Detached merge line: skip CrossObjectSyncHelper.
                    }
                }
            }
        }

        private InvitationItem previousInvitationItem;
        [ImmediatePostData]
        [Appearance("PreviousInvitationItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowPreviousInvitationItem", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowPreviousInvitationItemRequiredCriteria)]
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
                    if (false && !SuppressPersonCurrentFieldSync)
                    {
                        // Detached merge line: skip CrossObjectSyncHelper.
                        _ = oldValue;
                    }
                }
            }
        }

        [Appearance("AddressOfResidenceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentAddressOfResidence", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowCurrentAddressOfResidenceRequiredCriteria)]
        [DataSourceProperty(nameof(AvailableAddressesOfResidence))]
        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        [Appearance("CurrentWorkDutyEmployeeOnly", Visibility = ViewItemVisibility.Hide,
            Criteria = PersonIsFamilyMemberCriteria, Context = "DetailView,ListView")]
        [Appearance("WorkDutyVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentWorkDuty", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowCurrentWorkDutyRequiredCriteria)]
        [DataSourceProperty(nameof(AvailableWorkDuties))]
        public virtual WorkDuty CurrentWorkDuty { get; set; }

        [Appearance("CurrentSalaryEmployeeOnly", Visibility = ViewItemVisibility.Hide,
            Criteria = PersonIsFamilyMemberCriteria, Context = "DetailView,ListView")]
        [Appearance("SalaryVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentSalary", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowCurrentSalaryRequiredCriteria)]
        [DataSourceProperty(nameof(AvailableSalaries))]
        public virtual EmployeeSalary CurrentSalary { get; set; }

        [Appearance("MedicalRecordVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentMedicalRecord", Context = "DetailView,ListView")]
        [ExcludeFromOptionalDetailFields]
        [DataSourceProperty(nameof(AvailableMedicalRecords))]
        public virtual MedicalRecord CurrentMedicalRecord { get; set; }

        [Appearance("CurrentEducationHiddenOnRegistration", Visibility = ViewItemVisibility.Hide,
            Criteria = RegistrationApplicationItemContextCriteria, Context = "DetailView,ListView")]
        [Appearance("CurrentEducationEmployeeOnly", Visibility = ViewItemVisibility.Hide,
            Criteria = PersonIsFamilyMemberCriteria, Context = "DetailView,ListView")]
        [Appearance("EducationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowCurrentEducation", Context = "DetailView,ListView")]
        [RuleRequiredField(TargetCriteria = ShowCurrentEducationRequiredCriteria)]
        [DataSourceProperty(nameof(AvailableEducations))]
        public virtual Education CurrentEducation { get; set; }

        [ExcludeFromOptionalDetailFields]
        [Appearance("InvitationIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowInvitationItemIsIssued", Context ="DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool InvitationItemIsIssued { get; set; }

        [ExcludeFromOptionalDetailFields]
        [Appearance("WorkPermitIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowWorkPermitItemIsIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool WorkPermitItemIsIssued { get; set; }

        [ExcludeFromOptionalDetailFields]
        [Appearance("RejectionIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowRejectionIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool RejectionIssued { get; set; }

        [ExcludeFromOptionalDetailFields]
        [Appearance("VisaIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowVisaIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIssued { get; set; }

        /// <summary>
        /// Latest parent <see cref="ApplicationProfileInstance"/> progress state/location code for ListView row color (<see cref="IBoListRowState"/>).
        /// Reuses denormalized <see cref="ApplicationProfileInstance.PrimaryStateCode"/> — no per-item progress history walk.
        /// </summary>
        [NotMapped]
        public string PrimaryStateCode => ApplicationProfileInstance?.PrimaryStateCode ?? string.Empty;

        /// <summary>
        /// Localized latest application progress state (from parent denormalized display / computed fallback).
        /// </summary>
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public string LastApplicationState =>
            !string.IsNullOrEmpty(ApplicationProfileInstance?.LatestProgressDisplay)
                ? ApplicationProfileInstance.LatestProgressDisplay
                : ApplicationProfileInstance?.LatestProgressState ?? string.Empty;

        /// <summary>
        /// Row CSS from parent <see cref="ApplicationProfileInstance.ListRowCssClass"/> (includes SLA override). Empty when cancelled line wins.
        /// </summary>
        [NotMapped]
        public string ListRowCssClass =>
            IsLineCancelled ? string.Empty : ApplicationProfileInstance?.ListRowCssClass ?? string.Empty;

        /// <summary>
        /// Line or parent application is cancelled — type-specific flags or latest application <c>PROCESS_CANCELLED</c> progress.
        /// </summary>
        [ToolTip("Work-permit, invitation, or visa line cancel flags, or parent application PROCESS_CANCELLED progress.")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(true)]
        [VisibleInListView(true)]
        [NotMapped]
        public bool IsLineCancelled =>
            InvitationItemIsCancelled
            || VisaIsCancelled
            || IsCancelled
            || string.Equals(
                ApplicationProfileInstance?.PrimaryStateCode,
                ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
                StringComparison.OrdinalIgnoreCase);

		[ExcludeFromOptionalDetailFields]
		[Appearance("InvitationItemIsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowInvitationItemIsCancelled", Context = "DetailView,ListView")]
		public virtual bool InvitationItemIsCancelled { get; set; }

		[ExcludeFromOptionalDetailFields]
		[Appearance("IsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowWorkPermitItemIsCancelled", Context = "DetailView,ListView")]
		public virtual bool IsCancelled { get; set; }

		[ExcludeFromOptionalDetailFields]
		[Appearance("InvitationItemIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowInvitationItemIsChanged", Context = "DetailView,ListView")]
		public virtual bool InvitationItemIsChanged { get; set; }

		[ExcludeFromOptionalDetailFields]
		[Appearance("WorkPermitItemIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowWorkPermitItemIsChanged", Context = "DetailView,ListView")]
		public virtual bool WorkPermitItemIsChanged { get; set; }

		[ExcludeFromOptionalDetailFields]
		[Appearance("VisaIsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowVisaIsCancelled", Context = "DetailView,ListView")]
		[ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIsCancelled { get; set; }

        [ExcludeFromOptionalDetailFields]
        [Appearance("VisaIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "ApplicationProfileInstance is null or !ApplicationProfileInstance.CfgShowVisaIsChanged", Context = "DetailView,ListView")]
		[ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIsChanged { get; set; }
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        public virtual bool ApplicationItemsIsCancelled { get; set; }


        [RuleFromBoolProperty("ApplicationItem_PersonUniqueInApplication", DefaultContexts.Save, "This person already has an ApplicationProfileInstance Item in the same Application.")]
        public bool IsPersonUniqueInApplication
        {
            get
            {
                if (Person == null || ApplicationProfileInstance == null) return true;
                var personId = Person.ID;
                return ApplicationProfileInstance.People?
                    .Count(ap => ap?.ID == personId) <= 1;
            }
        }

        [MaxLength(255)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string ApplicationItemName { get; set; }

        /// <summary>
        /// Lookup / object caption: person plus parent application caption
        /// (application number · process number when present), matching <see cref="ApplicationProfileInstance.DisplayCaption"/>.
        /// </summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public string DisplayCaption
        {
            get
            {
                var person = Person?.FullName?.Trim() ?? string.Empty;
                var appCaption = ApplicationProcessNumberHelper.FormatDisplayCaption(ApplicationProfileInstance);
                if (person.Length == 0)
                    return appCaption;
                if (appCaption.Length == 0)
                    return person;
                return $"{person} - {appCaption}";
            }
        }

        private void UpdateApplicationItemName()
        {
            ApplicationItemName = Person == null && ApplicationProfileInstance == null
                ? null
                : DisplayCaption;
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
