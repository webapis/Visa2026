using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Specialized;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(ApplicationNumber))]
//    [RuleUniqueValue("UniqueAppNumberPerPrefix", DefaultContexts.Save, "AppNumberPrefix;ApplicationNumber;Year", CustomMessageTemplate = "An application with this prefix, number, and year already exists.")]
    public class Application : BaseObject, IExpirationLogic, IObjectSpaceLink, ISoftDelete
    {
        public Application()
        {
            ApplicationItems = new ObservableCollection<ApplicationItem>();
            Invitations = new ObservableCollection<Invitation>();
            Rejections = new ObservableCollection<Rejection>();
            WorkPermits = new ObservableCollection<WorkPermit>();
            Registrations = new ObservableCollection<Registration>();
            BusinessTrips = new ObservableCollection<BusinessTrip>();
  

            var progressHistoryCollection = new ObservableCollection<ApplicationProgress>();
            progressHistoryCollection.CollectionChanged += ProgressHistory_CollectionChanged;
            progressHistory = progressHistoryCollection;
        }

        [XafDisplayName("Manual Entry")]
        [ToolTip("Enable to manually set the application number for historical records that existed before this system was deployed.")]
        [ImmediatePostData]
        public virtual bool IsManualEntry { get; set; }

        [MaxLength(50)]
        [VisibleInListView(false)]
        [Appearance("ApplicationNumberReadOnly", Context = "DetailView", Criteria = "!IsManualEntry", Enabled = false)]
        public virtual string ApplicationNumber { get; set; }

        [VisibleInListView(false)]
        [Appearance("AppNumberPrefixReadOnly", Context = "DetailView", Criteria = "!IsManualEntry", Enabled = false)]
        public virtual string AppNumberPrefix { get; set; }

        [MaxLength(100)]
        public virtual string FullApplicationNumber { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual int Year { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual int Month { get; set; }

        private DateTime applicationDate;
        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime ApplicationDate
        {
            get => applicationDate;
            set
            {
                if (applicationDate != value)
                {
                    applicationDate = value;
                    if (ApplicationType != null && ApplicationType.DurationInDays > 0)
                    {
                        ExpirationDate = applicationDate.AddDays(ApplicationType.DurationInDays);
                    }
                }
            }
        }

        private ApplicationTypeCategory category;
        [ImmediatePostData]

        public virtual ApplicationTypeCategory Category
        {
            get => category;
            set
            {
                if (category != value)
                {
                    category = value;
                    ApplicationTypeFilter = null;
                    ApplicationType = null;
                }
            }
        }

        // private OrganizationType organizationType;
        // [ImmediatePostData]
        // public virtual OrganizationType OrganizationType
        // {
        //     get => organizationType;
        //     set
        //     {
        //         if (organizationType != value)
        //         {
        //             organizationType = value;
        //             ApplicationType = null;
        //         }
        //     }
        // }

        private ApplicationTypeFilter applicationTypeFilter;
        [ImmediatePostData]
        [VisibleInListView(false)]
        [RuleRequiredField]
        [DataSourceCriteria("Category = '@This.Category'")]
        public virtual ApplicationTypeFilter ApplicationTypeFilter
        {
            get => applicationTypeFilter;
            set
            {
                if (applicationTypeFilter != value)
                {
                    applicationTypeFilter = value;
                    ApplicationType = null;
                }
            }
        }

        private ApplicationType applicationType;
        [ImmediatePostData, RuleRequiredField]
        [DataSourceCriteria("ApplicationTypeFilter = '@This.ApplicationTypeFilter'")]
        public virtual ApplicationType ApplicationType
        {
            get => applicationType;
            set
            {
                if (applicationType != value)
                {
                    applicationType = value;
                    if (applicationType != null && applicationType.DurationInDays > 0)
                    {
                        ExpirationDate = ApplicationDate.AddDays(applicationType.DurationInDays);
                    }
                }
            }
        }

        [ModelDefault("AllowEdit", "False")]
        public virtual ApplicationProgress CurrentState { get; set; }


        [Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowProjectContract", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual ProjectContract ProjectContract { get; set; }

        private Company company;
        [ImmediatePostData]
        [VisibleInListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public virtual Company Company
        {
            get => company;
            set
            {
                if (company != value)
                {
                    company = value;
                    CompanyHead = company?.CurrentAuthorizedSignatory;
                    Representative = company?.CurrentRepresentative;
                }
            }
        }

        [DataSourceCriteria("Company = '@This.Company'")]
        [VisibleInListView(false)]
        [ModelDefault("AllowEdit", "False")]
        [RuleRequiredField]
        public virtual CompanyHead CompanyHead { get; set; }


        [DataSourceCriteria("Company = '@This.Company'")]
        [VisibleInListView(false)]
        [ModelDefault("AllowEdit", "False")]
        [RuleRequiredField]
        public virtual Representative Representative { get; set; }

        [Appearance("UrgencyVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowUrgency", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual Urgency Urgency { get; set; }

        [Appearance("VisaPeriodVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaPeriod", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual VisaPeriod VisaPeriod { get; set; }

        [Appearance("VisaCategoryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaCategory", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual VisaCategory VisaCategory { get; set; }

        [Appearance("VisaTypeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowVisaType", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual VisaType VisaType { get; set; }

        [Appearance("MigrationServiceVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowMigrationService", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual MigrationService MigrationService { get; set; }

        [XafDisplayName("Migration Service Name (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string MigrationService_NameTm => MigrationService?.NameTm;

        [XafDisplayName("Company Code"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Company_Code => Company?.Code;

        [XafDisplayName("Urgency (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Urgency_NameTm => Urgency?.NameTm;

        [XafDisplayName("Visa Period (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string VisaPeriod_NameTm => VisaPeriod?.NameTm;

        [XafDisplayName("Visa Category (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string VisaCategory_NameTm => VisaCategory?.NameTm;

        [XafDisplayName("Project Contract Description"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ProjectContract_Description => ProjectContract?.Description;

        [XafDisplayName("Ministry Recipient Block"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ProjectContract_Ministry_RecipientBlock => ProjectContract?.Ministry?.RecipientBlock;

        [XafDisplayName("Ministry Form of Address"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ProjectContract_Ministry_FormOfAddress => ProjectContract?.Ministry?.FormOfAddress;

        [XafDisplayName("FM Relationship (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string FamilyMember_Relationship_NameTm =>
            JoinTurkmenList(
                ApplicationItems?
                    .Select(i => i.Person?.Relationship)
                    .Where(r => r != null)
                    .Select(r => string.IsNullOrEmpty(r.ReverseNameTm) ? r.NameTm : r.ReverseNameTm)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Distinct()
                    .Select(AddTurkmenGenitive)
                    .ToList());

        [XafDisplayName("Sponsoring Employee Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string SponsoringEmployee_FullName =>
            ApplicationItems?.FirstOrDefault()?.Person?.SponsoringEmployee?.FullName;

        [XafDisplayName("Sponsoring Employee Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string SponsoringEmployee_PositionTm =>
            ApplicationItems?.FirstOrDefault()?.Person?.SponsoringEmployee?.CurrentPositionHistory?.Position?.NameTm;

        [Appearance("BusinessTripStartDateVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowBusinessTrips", Context = "DetailView")]
        [VisibleInListView(false)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? BusinessTripStartDate { get; set; }

        [Appearance("BusinessTripEndDateVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowBusinessTrips", Context = "DetailView")]
        [VisibleInListView(false)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? BusinessTripEndDate { get; set; }

        [Appearance("BusinessTripPurposeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowBusinessTrips", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual BusinessTripPurpose BusinessTripPurpose { get; set; }

        [Appearance("MovementPermitLocationVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowMovementPermitLocation", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual MovementPermitLocation MovementPermitLocation { get; set; }

        [XafDisplayName("Movement Permit Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string MovementPermitLocation_NameTm => MovementPermitLocation?.NameTm;

        [Appearance("BorderZoneLocationVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowBorderZoneLocation", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual BorderZoneLocation BorderZoneLocation { get; set; }

        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string BorderZoneLocation_NameTm => BorderZoneLocation?.NameTm;

        [Appearance("FromCityVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowFromCity", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual City FromCity { get; set; }

        [Appearance("ToCityVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowToCity", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual City ToCity { get; set; }

        #region Person Count
        [XafDisplayName("Total Person Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int TotalPersonCount => (ApplicationItems?.Count ?? 0) + (Registrations?.Count ?? 0);

        [XafDisplayName("Total Person Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string TotalPersonCountText => NumberToTurkmenWords(TotalPersonCount);

        // Used by App_Cancel_Visa_and_WP and App_Cancel_Inv_WP reports
        [XafDisplayName("Cancel Person Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelPersonCount => ApplicationItems?.Count ?? 0;

        [XafDisplayName("Cancel Person Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CancelPersonCountText => NumberToTurkmenWords(CancelPersonCount);

        [XafDisplayName("Cancel WP Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelWPCount => (ApplicationItems?.Count ?? 0)
            + (ApplicationItems?.Count(ai => ai.SecondWorkPermitItem != null) ?? 0);

        [XafDisplayName("Cancel WP Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CancelWPCountText => NumberToTurkmenWords(CancelWPCount);

        [XafDisplayName("Cancel Inv Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelInvCount => ApplicationItems?.Count(ai => ai.CurrentInvitationItem != null) ?? 0;

        [XafDisplayName("Cancel Inv Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CancelInvCountText => NumberToTurkmenWords(CancelInvCount);

        #endregion

        private static string NumberToTurkmenWords(int number)
        {
            string[] ones = { "", "bir", "iki", "üç", "dört", "bäş", "alty", "ýedi", "sekiz", "dokuz",
                               "on", "on bir", "on iki", "on üç", "on dört", "on bäş", "on alty", "on ýedi", "on sekiz", "on dokuz" };
            string[] tens = { "", "", "ýigrimi", "otuz", "kyrk", "elli", "altmyş", "ýetmiş", "segsen", "togsan" };

            if (number == 0) return "nol";
            if (number < 20) return ones[number];
            if (number < 100) return tens[number / 10] + (number % 10 != 0 ? " " + ones[number % 10] : "");
            if (number < 1000) return ones[number / 100] + " ýüz" + (number % 100 != 0 ? " " + NumberToTurkmenWords(number % 100) : "");
            return number.ToString();
        }

        /// <summary>
        /// Joins a list of items with commas and "we" for the last pair.
        /// 1 item  → "aýalynyň"
        /// 2 items → "aýalynyň we çagasynyň"
        /// 3 items → "aýalynyň, çagasynyň we oglunyň"
        /// </summary>
        private static string JoinTurkmenList(IList<string> items)
        {
            if (items == null || items.Count == 0) return string.Empty;
            if (items.Count == 1) return items[0];
            return string.Join(", ", items.Take(items.Count - 1)) + " we " + items[items.Count - 1];
        }

        /// <summary>
        /// Appends a Turkmen case suffix with vowel harmony.
        /// Scans from the end of the word to find the last vowel, then picks back or front suffix.
        /// Back vowels: a, o, u, y  |  Front vowels: e, ä, ö, ü, i
        /// Examples:
        ///   Genitive  ("nyň"/"niň")  : "aýaly"          → "aýalynyň"
        ///   Ablative  ("ndan"/"nden"): "Aşgabat şäheri" → "Aşgabat şäherinden"
        ///   Dative    ("na"/"ne")    : "Akbugdaý etraby"→ "Akbugdaý etrabyna"
        /// </summary>
        private static string AddTurkmenCase(string word, string backSuffix, string frontSuffix)
        {
            if (string.IsNullOrEmpty(word)) return word;
            const string backVowels  = "aouяAOUYyаоуя";
            const string frontVowels = "eäöüiEÄÖÜİI";
            for (int i = word.Length - 1; i >= 0; i--)
            {
                if (backVowels.IndexOf(word[i]) >= 0)  return word + backSuffix;
                if (frontVowels.IndexOf(word[i]) >= 0) return word + frontSuffix;
            }
            return word + backSuffix; // fallback
        }

        private static string AddTurkmenGenitive(string word) =>
            AddTurkmenCase(word, "nyň", "niň");

        [XafDisplayName("From City Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string FromCityName => FromCity?.Name;

        [XafDisplayName("From Region Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string FromRegionName => FromCity?.Region?.Name;

        [XafDisplayName("To City Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string ToCityName => ToCity?.Name;

        [XafDisplayName("To Region Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string ToRegionName => ToCity?.Region?.Name;

        /// <summary>Genitive of FromCity region — e.g. "Mary welaýaty" → "Mary welaýatynyň"</summary>
        [XafDisplayName("From Region (Genitive)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string FromRegionName_Genitive => AddTurkmenCase(FromCity?.Region?.Name, "nyň", "niň");

        /// <summary>Ablative of FromCity — e.g. "Aşgabat şäheri" → "Aşgabat şäherinden"</summary>
        [XafDisplayName("From City (Ablative)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string FromCityName_Ablative => AddTurkmenCase(FromCity?.Name, "ndan", "nden");

        /// <summary>Genitive of ToCity region — e.g. "Ahal welaýaty" → "Ahal welaýatynyň"</summary>
        [XafDisplayName("To Region (Genitive)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ToRegionName_Genitive => AddTurkmenCase(ToCity?.Region?.Name, "nyň", "niň");

        /// <summary>Dative of ToCity — e.g. "Akbugdaý etraby" → "Akbugdaý etrabyna"</summary>
        [XafDisplayName("To City (Dative)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ToCityName_Dative => AddTurkmenCase(ToCity?.Name, "na", "ne");

        [XafDisplayName("Business Trip Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string BusinessTripStartDateText => $"{BusinessTripStartDate:dd.MM.yyyy}";

        [XafDisplayName("Business Trip End Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string BusinessTripEndDateText => $"{BusinessTripEndDate:dd.MM.yyyy}";

        [XafDisplayName("Business Trip Duration (Days)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int? BusinessTripDurationDays =>
            BusinessTripStartDate.HasValue && BusinessTripEndDate.HasValue
                ? (int?)((BusinessTripEndDate.Value - BusinessTripStartDate.Value).TotalDays + 1)
                : null;

        [XafDisplayName("Business Trip Purpose (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string BusinessTripPurpose_NameTm => BusinessTripPurpose?.Name;

        [Aggregated]
        [InverseProperty(nameof(ApplicationItem.Application))]
        [Appearance("ApplicationItemsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowApplicationItems", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<ApplicationItem> ApplicationItems { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Invitation.Application))]
        [Appearance("InvitationsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowInvitations", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<Invitation> Invitations { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Rejection.Application))]
        [Appearance("RejectionsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowRejections", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<Rejection> Rejections { get; set; }

        [Aggregated]
        [InverseProperty(nameof(WorkPermit.Application))]
        [Appearance("WorkPermitsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowWorkPermits", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<WorkPermit> WorkPermits { get; set; }

        [Aggregated]
        [InverseProperty(nameof(Registration.Application))]
        [Appearance("RegistrationsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowRegistrations", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<Registration> Registrations { get; set; }

        [Aggregated]
        [InverseProperty(nameof(BusinessTrip.Application))]
        [Appearance("BusinessTripsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowBusinessTrips", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<BusinessTrip> BusinessTrips { get; set; }


        // [RuleRequiredField]
        // [DataSourceCriteria("ApplicationType.ID = '@This.ApplicationType.ID'")]
        // [Appearance("ApplicationReasonVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowApplicationReason", Context = "DetailView")]
        // public virtual ApplicationReason ApplicationReason { get; set; }

        private void ProgressHistory_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateCurrentState();
        }

        private IList<ApplicationProgress> progressHistory;
        [Aggregated]
        [InverseProperty(nameof(ApplicationProgress.Application))]
        [VisibleInListView(false)]
        public virtual IList<ApplicationProgress> ProgressHistory
        {
            get => progressHistory;
            set
            {
                if (progressHistory is INotifyCollectionChanged oldCollection)
                {
                    oldCollection.CollectionChanged -= ProgressHistory_CollectionChanged;
                }
                progressHistory = value;
                if (progressHistory is INotifyCollectionChanged newCollection)
                {
                    newCollection.CollectionChanged += ProgressHistory_CollectionChanged;
                }
            }
        }

        public void UpdateCurrentState(ApplicationProgress changedProgress = null)
        {
            IEnumerable<ApplicationProgress> query = ProgressHistory;
            if (query == null)
            {
                if (changedProgress != null) query = new[] { changedProgress };
                else return;
            }
            else if (changedProgress != null && !query.Contains(changedProgress))
            {
                query = query.Concat(new[] { changedProgress });
            }

            if (ObjectSpace != null)
            {
                query = query.Where(p => !ObjectSpace.IsObjectToDelete(p));
            }

            var latestProgress = query
                .OrderByDescending(p => p.Date)
                .FirstOrDefault();

            if (CurrentState != latestProgress)
            {
                CurrentState = latestProgress;
            }
        }

        public virtual bool IsActive { get; set; } = true;
        [ModelDefault("AllowEdit", "False")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? ExpirationDate { get; set; }

        [NotMapped]
        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue) return int.MaxValue;
                return (ExpirationDate.Value.Date - DateTime.Today).Days;
            }
        }

        [NotMapped]
        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, ApplicationDate, ObjectSpace);
            }
        }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                ApplicationDate = DateTime.Now;
                Company = ObjectSpace.GetObjectsQuery<Company>().FirstOrDefault(c => c.IsDefault);
                Urgency = ObjectSpace.GetObjectsQuery<Urgency>().FirstOrDefault(u => u.IsDefault);
                VisaType = ObjectSpace.GetObjectsQuery<VisaType>().FirstOrDefault(v => v.IsDefault);
                VisaCategory = ObjectSpace.GetObjectsQuery<VisaCategory>().FirstOrDefault(vc => vc.IsDefault);
                VisaPeriod = ObjectSpace.GetObjectsQuery<VisaPeriod>().FirstOrDefault(vp => vp.IsDefault);
                ProjectContract = ObjectSpace.GetObjectsQuery<ProjectContract>().FirstOrDefault(pc => pc.IsDefault);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();
            if (ObjectSpace != null && ObjectSpace.IsNewObject(this))
            {
                Year = ApplicationDate.Year;
                Month = ApplicationDate.Month;

                if (Company != null && string.IsNullOrEmpty(AppNumberPrefix))
                    AppNumberPrefix = Company.AppNumberPrefix;

                if (IsManualEntry)
                {
                    if (!string.IsNullOrEmpty(ApplicationNumber))
                        FullApplicationNumber = BuildFullNumber(
                            Company?.AppNumberFormat,
                            AppNumberPrefix,
                            Year, Month,
                            ApplicationNumber);
                    else if (!string.IsNullOrEmpty(FullApplicationNumber))
                        ApplicationNumber = FullApplicationNumber;
                    return;
                }

                if (string.IsNullOrEmpty(ApplicationNumber))
                {
                    string fmt = Company?.AppNumberFormat;
                    bool scopeByYear  = string.IsNullOrEmpty(fmt) || fmt.Contains("{YEAR}")  || fmt.Contains("{YEAR2}");
                    bool scopeByMonth = !string.IsNullOrEmpty(fmt) && (fmt.Contains("{MONTH}") || fmt.Contains("{MONTH2}"));

                    var dbQuery = ObjectSpace.GetObjectsQuery<Application>()
                        .Where(a => a.AppNumberPrefix == this.AppNumberPrefix);
                    if (scopeByYear || scopeByMonth) dbQuery = dbQuery.Where(a => a.Year  == this.Year);
                    if (scopeByMonth)                dbQuery = dbQuery.Where(a => a.Month == this.Month);

                    var maxDb = dbQuery
                        .Select(a => a.ApplicationNumber)
                        .ToList()
                        .Select(n => int.TryParse(n, out int num) ? num : 0)
                        .DefaultIfEmpty(0)
                        .Max();

                    var maxLocal = 0;
                    if (ObjectSpace is BaseObjectSpace baseObjectSpace)
                    {
                        var localApps = baseObjectSpace.ModifiedObjects.OfType<Application>()
                            .Where(a => !baseObjectSpace.IsObjectToDelete(a) && a != this &&
                                        a.AppNumberPrefix == this.AppNumberPrefix &&
                                        (!(scopeByYear || scopeByMonth) || a.Year  == this.Year) &&
                                        (!scopeByMonth                  || a.Month == this.Month) &&
                                        !string.IsNullOrEmpty(a.ApplicationNumber));
                        if (localApps.Any())
                            maxLocal = localApps.Select(a => int.TryParse(a.ApplicationNumber, out int n) ? n : 0).Max();
                    }

                    int seed = Company?.ApplicationNumberSeed ?? 0;
                    int padding = Company?.ApplicationNumberPadding > 0 ? Company.ApplicationNumberPadding : 4;
                    ApplicationNumber = (Math.Max(Math.Max(maxDb, maxLocal), seed) + 1).ToString($"D{padding}");
                }

                FullApplicationNumber = BuildFullNumber(
                    Company?.AppNumberFormat,
                    AppNumberPrefix,
                    Year, Month,
                    ApplicationNumber);
            }
            else if (IsManualEntry)
            {
                Year = ApplicationDate.Year;
                Month = ApplicationDate.Month;
                if (Company != null && string.IsNullOrEmpty(AppNumberPrefix))
                    AppNumberPrefix = Company.AppNumberPrefix;
                if (!string.IsNullOrEmpty(ApplicationNumber))
                    FullApplicationNumber = BuildFullNumber(
                        Company?.AppNumberFormat,
                        AppNumberPrefix,
                        Year, Month,
                        ApplicationNumber);
                else if (!string.IsNullOrEmpty(FullApplicationNumber))
                    ApplicationNumber = FullApplicationNumber;
            }
        }

        private static string BuildFullNumber(string format, string prefix, int year, int month, string number)
        {
            if (string.IsNullOrEmpty(format))
                return $"{prefix}{number}";

            return format
                .Replace("{PREFIX}",  prefix ?? "")
                .Replace("{YEAR}",    year.ToString())
                .Replace("{YEAR2}",   (year % 100).ToString("D2"))
                .Replace("{MONTH2}",  month.ToString("D2"))
                .Replace("{MONTH}",   month.ToString())
                .Replace("{NUMBER}",  number ?? "");
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            if (progressHistory is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= ProgressHistory_CollectionChanged;
                collection.CollectionChanged += ProgressHistory_CollectionChanged;
            }
            UpdateCurrentState();
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}