using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using System.Linq;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;
using Visa2026.Module.Services;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem(false)]
    [DefaultProperty(nameof(ApplicationNumber))]
//    [RuleUniqueValue("UniqueAppNumberPerPrefix", DefaultContexts.Save, "AppNumberPrefix;ApplicationNumber;Year", CustomMessageTemplate = "An application with this prefix, number, and year already exists.")]
    public class Application : BaseObject, ISoftDelete, IBoListRowState
    {
        private const string AppInvApplicationTypeName = "App_Inv";
        private const string AppInvAndWpApplicationTypeName = "App_Inv_And_WP";
        /// <summary>Default visa period for <see cref="AppInvApplicationTypeName"/> (see visa-period.json <c>Month1</c>).</summary>
        private const string AppInvDefaultVisaPeriodLocalizationKey = "Month1";
        /// <summary>Default visa type for <see cref="AppInvApplicationTypeName"/> (see visa-type.json <c>BS1</c>).</summary>
        private const string AppInvDefaultVisaTypeLocalizationKey = "BS1";
        /// <summary>Default visa category for <see cref="AppInvApplicationTypeName"/> (see visa-category.json <c>Double</c> / Iki gezeklik).</summary>
        private const string AppInvDefaultVisaCategoryLocalizationKey = "Double";
        /// <summary>Default visa period for <see cref="AppInvAndWpApplicationTypeName"/> (see visa-period.json <c>Month6</c>).</summary>
        private const string AppInvAndWpDefaultVisaPeriodLocalizationKey = "Month6";
        /// <summary>Default visa category for <see cref="AppInvAndWpApplicationTypeName"/> (see visa-category.json <c>Multiple</c> / köp gezeklik).</summary>
        private const string AppInvAndWpDefaultVisaCategoryLocalizationKey = "Multiple";
        /// <summary>Default visa type for <see cref="AppInvAndWpApplicationTypeName"/> (see visa-type.json <c>WP</c> / WP-Işçi Wiza).</summary>
        private const string AppInvAndWpDefaultVisaTypeLocalizationKey = "WP";

        public Application()
        {
            ApplicationItems = new ObservableCollection<ApplicationItem>();
            Invitations = new ObservableCollection<Invitation>();
            Rejections = new ObservableCollection<Rejection>();
            WorkPermits = new ObservableCollection<WorkPermit>();
            ProgressHistory = new ObservableCollection<ApplicationProgress>();
        }

        [XafDisplayName("Manual Entry")]
        [ToolTip("Enable to manually set the application number for historical records that existed before this system was deployed.")]
        [VisibleInListView(false)]
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
        [Appearance("FullApplicationNumberReadOnly", Context = "DetailView", Criteria = "!IsManualEntry", Enabled = false)]
        public virtual string FullApplicationNumber { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual int Year { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual int Month { get; set; }

        private DateTime applicationDate;
        [RuleRequiredField]
        [Appearance("ApplicationDateReadOnly", Context = "DetailView", Criteria = "!IsManualEntry", Enabled = false)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime ApplicationDate
        {
            get => applicationDate;
            set
            {
                if (applicationDate != value)
                    applicationDate = value;
            }
        }

        /// <summary>Flattened for Word / user-report placeholders (see docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md).</summary>
        [XafDisplayName("Application Date (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ApplicationDateText => ApplicationDate.ToString("dd.MM.yyyy");

        private string applicationTypeQuickCode;

        [XafDisplayName("Application Type Code")]
        [ToolTip("Enter a 3-digit ministry code (e.g. 101 invitation, 104 change invitation, 401 work permit extension). Use … beside this field for the full list.")]
        [EditorAlias(Editors.ApplicationTypeQuickCodeEditorAliases.QuickCode)]
        [NotMapped]
        [ImmediatePostData]
        [VisibleInListView(false)]
        [MaxLength(3)]
        public virtual string ApplicationTypeQuickCode
        {
            get => applicationTypeQuickCode;
            set
            {
                if (string.Equals(applicationTypeQuickCode, value, StringComparison.Ordinal))
                    return;

                applicationTypeQuickCode = value;
                ApplicationTypeQuickCodeChanged?.Invoke(value);
            }
        }

        /// <summary>Wired by <c>ApplicationTypeSelectionController</c> on Blazor postback for <see cref="ApplicationTypeQuickCode"/>.</summary>
        [Browsable(false)]
        [NotMapped]
        public Action<string?>? ApplicationTypeQuickCodeChanged { get; set; }

        /// <summary>
        /// Set when <see cref="Application"/> is created from a route-specific ListView
        /// (<see cref="ApplicationProgressRouteNavigation"/>). Filters the type-code picker until saved.
        /// </summary>
        [Browsable(false)]
        [NotMapped]
        public virtual ApplicationProgressRouteKind? CreationProgressRoute { get; set; }

        /// <summary>Filtered by <see cref="ApplicationProgressRouteHelper"/> for nested <see cref="ApplicationProgress"/> entry.</summary>
        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationState> AvailableProgressStates => LoadAvailableProgressStates();

        /// <summary>Filtered by <see cref="ApplicationProgressRouteHelper"/> for nested <see cref="ApplicationProgress"/> entry.</summary>
        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationLocation> AvailableProgressLocations => LoadAvailableProgressLocations();

        /// <summary>
        /// Latest <see cref="ApplicationProgress"/> state/location code for ListView row color (<see cref="IBoListRowState"/>).
        /// </summary>
        [Browsable(false)]
        [NotMapped]
        public string PrimaryStateCode =>
            ApplicationProgressPrimaryStateCodeResolver.Resolve(this) ?? string.Empty;

        /// <summary>
        /// Latest progress state and location (localized) for ListView — <see cref="ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayName"/>.
        /// </summary>
        [XafDisplayName("Current State")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public string CurrentState =>
            ApplicationProgressPrimaryStateCodeResolver.ResolveDisplayName(this) ?? string.Empty;

        private ApplicationType applicationType;
        [ImmediatePostData, RuleRequiredField]
        [DataSourceCriteria("!IsNullOrEmpty(SelectionCode)")]
        [Appearance("ApplicationTypeReadOnlyOnDetail", Enabled = false, Context = "DetailView")]
        [ToolTip("Selected via Application type code or the … list beside it.")]
        public virtual ApplicationType ApplicationType
        {
            get => applicationType;
            set
            {
                if (applicationType != value)
                {
                    applicationType = value;
                    ApplyDefaultsForApplicationType();
                    if (ApplicationItems != null)
                    {
                        foreach (var item in ApplicationItems)
                            item.RefreshVisibilityGatedReferenceFields();
                    }
                }
            }
        }

        private void ApplyDefaultsForApplicationType()
        {
            if (ObjectSpaceHelper.Get(this) == null || applicationType == null)
                return;

            if (!TryGetDefaultVisaLookupKeys(
                    applicationType.Name,
                    out var visaPeriodKey,
                    out var visaCategoryKey,
                    out var visaTypeKey))
                return;

            if (applicationType.ShowVisaPeriod && visaPeriodKey != null)
            {
                var period = ObjectSpaceHelper.Get(this).GetObjectsQuery<VisaPeriod>()
                    .FirstOrDefault(vp => vp.LocalizationKey == visaPeriodKey);
                if (period != null)
                    VisaPeriod = period;
            }

            if (applicationType.ShowVisaType && visaTypeKey != null)
            {
                var visaType = ObjectSpaceHelper.Get(this).GetObjectsQuery<VisaType>()
                    .FirstOrDefault(vt => vt.LocalizationKey == visaTypeKey);
                if (visaType != null)
                    VisaType = visaType;
            }

            if (applicationType.ShowVisaCategory && visaCategoryKey != null)
            {
                var category = ObjectSpaceHelper.Get(this).GetObjectsQuery<VisaCategory>()
                    .FirstOrDefault(vc => vc.LocalizationKey == visaCategoryKey);
                if (category != null)
                    VisaCategory = category;
            }
        }

        private static bool TryGetDefaultVisaLookupKeys(
            string? applicationTypeName,
            out string? visaPeriodLocalizationKey,
            out string? visaCategoryLocalizationKey,
            out string? visaTypeLocalizationKey)
        {
            visaPeriodLocalizationKey = null;
            visaCategoryLocalizationKey = null;
            visaTypeLocalizationKey = null;

            if (string.Equals(applicationTypeName, AppInvApplicationTypeName, StringComparison.Ordinal))
            {
                visaPeriodLocalizationKey = AppInvDefaultVisaPeriodLocalizationKey;
                visaCategoryLocalizationKey = AppInvDefaultVisaCategoryLocalizationKey;
                visaTypeLocalizationKey = AppInvDefaultVisaTypeLocalizationKey;
                return true;
            }

            if (string.Equals(applicationTypeName, AppInvAndWpApplicationTypeName, StringComparison.Ordinal))
            {
                visaPeriodLocalizationKey = AppInvAndWpDefaultVisaPeriodLocalizationKey;
                visaCategoryLocalizationKey = AppInvAndWpDefaultVisaCategoryLocalizationKey;
                visaTypeLocalizationKey = AppInvAndWpDefaultVisaTypeLocalizationKey;
                return true;
            }

            return false;
        }

        [Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowProjectContract", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual ProjectContract ProjectContract { get; set; }

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
        public string Company_Code => OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this))?.Code ?? string.Empty;

        [XafDisplayName("Company Name (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_Company_Name => OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this))?.Name ?? string.Empty;

        [XafDisplayName("Company Address (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_Company_Address => OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this))?.Address ?? string.Empty;

        [XafDisplayName("Company Phone (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_Company_PhoneNumber => OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this))?.PhoneNumber ?? string.Empty;

        [XafDisplayName("Company Email (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_Company_Email => OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this))?.Email ?? string.Empty;

        /// <summary>Flattened for Word / user-report placeholders (see <c>docs/WORD_REPORT_PLACEHOLDER_REFERENCE.md</c>).</summary>
        [XafDisplayName("Company Head Full Name (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_CompanyHead_FullName => OrganizationReportHelper.GetSignatory(ObjectSpaceHelper.Get(this))?.FullName ?? string.Empty;

        /// <summary>Flattened for Word / user-report placeholders.</summary>
        [XafDisplayName("Company Head Position (Tm, Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_CompanyHead_PositionTm => OrganizationReportHelper.GetSignatory(ObjectSpaceHelper.Get(this))?.PositionTitleTm ?? string.Empty;

        [XafDisplayName("Application Type Name (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ApplicationType_Name => ApplicationType?.Name ?? string.Empty;

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
            PersonCurrentItems.GetCurrentPositionHistory(ApplicationItems?.FirstOrDefault()?.Person?.SponsoringEmployee)?.Position?.NameTm;

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
        public int TotalPersonCount => ApplicationItems?.Count ?? 0;

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

        /// <summary>
        /// Total visas requested for cancellation on <see cref="App_Cancel_Visa"/> applications:
        /// per active line, +1 when <see cref="ApplicationItem.CurrentVisa"/> is set and +1 when <see cref="ApplicationItem.NextVisa"/> is set.
        /// </summary>
        [XafDisplayName("Cancel Visa Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelVisaCount => ApplicationItems?
            .Where(ai => ai != null && !ai.IsDeleted)
            .Sum(ai => (ai.CurrentVisa != null ? 1 : 0) + (ai.NextVisa != null ? 1 : 0)) ?? 0;

        [XafDisplayName("Cancel Visa Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CancelVisaCountText => NumberToTurkmenWords(CancelVisaCount);

        [XafDisplayName("Cancel WP Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelWPCount => (ApplicationItems?.Count ?? 0)
            + (ApplicationItems?.Count(ai => ai.PreviousWorkPermitItem != null) ?? 0);

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

        // [RuleRequiredField]
        // [DataSourceCriteria("ApplicationType.ID = '@This.ApplicationType.ID'")]
        // [Appearance("ApplicationReasonVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowApplicationReason", Context = "DetailView")]
        // public virtual ApplicationReason ApplicationReason { get; set; }

        [Aggregated]
        [InverseProperty(nameof(ApplicationProgress.Application))]
        [VisibleInListView(false)]
        public virtual IList<ApplicationProgress> ProgressHistory { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                ApplicationDate = DateTime.Now;
                Urgency = objectSpace.GetObjectsQuery<Urgency>().FirstOrDefault(u => u.IsDefault);
                VisaType = objectSpace.GetObjectsQuery<VisaType>().FirstOrDefault(v => v.IsDefault);
                VisaCategory = objectSpace.GetObjectsQuery<VisaCategory>().FirstOrDefault(vc => vc.IsDefault);
                VisaPeriod = objectSpace.GetObjectsQuery<VisaPeriod>().FirstOrDefault(vp => vp.IsDefault);
                ProjectContract = objectSpace.GetObjectsQuery<ProjectContract>().FirstOrDefault(pc => pc.IsDefault);
                ApplicationProgressInitializer.EnsureInitialProgress(this, objectSpace);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();
            if (ObjectSpaceHelper.Get(this) != null && ObjectSpaceHelper.Get(this).IsNewObject(this))
            {
                Year = ApplicationDate.Year;
                Month = ApplicationDate.Month;

                var numbering = GetNumberingConfiguration();
                if (string.IsNullOrEmpty(AppNumberPrefix))
                    AppNumberPrefix = numbering.Prefix;

                if (IsManualEntry)
                {
                    if (!string.IsNullOrEmpty(ApplicationNumber))
                        FullApplicationNumber = BuildFullNumber(
                            numbering.Format,
                            AppNumberPrefix,
                            Year, Month,
                            ApplicationNumber);
                    else if (!string.IsNullOrEmpty(FullApplicationNumber))
                        ApplicationNumber = FullApplicationNumber;
                    return;
                }

                if (string.IsNullOrEmpty(ApplicationNumber))
                {
                    string fmt = numbering.Format;
                    bool scopeByYear  = string.IsNullOrEmpty(fmt) || fmt.Contains("{YEAR}")  || fmt.Contains("{YEAR2}");
                    bool scopeByMonth = !string.IsNullOrEmpty(fmt) && (fmt.Contains("{MONTH}") || fmt.Contains("{MONTH2}"));

                    var dbQuery = ObjectSpaceHelper.Get(this).GetObjectsQuery<Application>()
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
                    if (ObjectSpaceHelper.Get(this) is BaseObjectSpace baseObjectSpace)
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

                    ApplicationNumber = (Math.Max(Math.Max(maxDb, maxLocal), numbering.Seed) + 1).ToString($"D{numbering.Padding}");
                }

                FullApplicationNumber = BuildFullNumber(
                    numbering.Format,
                    AppNumberPrefix,
                    Year, Month,
                    ApplicationNumber);
            }
            else if (IsManualEntry)
            {
                Year = ApplicationDate.Year;
                Month = ApplicationDate.Month;
                var numbering = GetNumberingConfiguration();
                if (string.IsNullOrEmpty(AppNumberPrefix))
                    AppNumberPrefix = numbering.Prefix;
                if (!string.IsNullOrEmpty(ApplicationNumber))
                    FullApplicationNumber = BuildFullNumber(
                        numbering.Format,
                        AppNumberPrefix,
                        Year, Month,
                        ApplicationNumber);
                else if (!string.IsNullOrEmpty(FullApplicationNumber))
                    ApplicationNumber = FullApplicationNumber;
            }

            if (IsDeleted)
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace != null)
                    RegistrationTravelHistorySyncService.SoftDeleteAllForApplication(this, objectSpace);
            }
        }

        private (string Prefix, string Format, int Seed, int Padding) GetNumberingConfiguration()
        {
            var profile = OrganizationReportHelper.GetApplicationNumbering(ObjectSpaceHelper.Get(this));
            if (profile != null)
            {
                return (
                    profile.AppNumberPrefix ?? string.Empty,
                    profile.AppNumberFormat,
                    profile.ApplicationNumberSeed,
                    profile.ApplicationNumberPadding > 0
                        ? profile.ApplicationNumberPadding
                        : ApplicationNumberingProfile.DefaultApplicationNumberPadding);
            }

            return (
                string.Empty,
                "{PREFIX}{YEAR}-{NUMBER}",
                ApplicationNumberingProfile.DefaultApplicationNumberSeed,
                ApplicationNumberingProfile.DefaultApplicationNumberPadding);
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

        private IList<ApplicationState> LoadAvailableProgressStates()
        {
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace == null)
                return Array.Empty<ApplicationState>();

            var allowedCodes = ApplicationProgressRouteHelper.GetAllowedStateCodes(this)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationState>()
                .Where(s => s.Code != null && allowedCodes.Contains(s.Code))
                .OrderBy(s => s.Code)
                .ToList();
        }

        private IList<ApplicationLocation> LoadAvailableProgressLocations()
        {
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace == null)
                return Array.Empty<ApplicationLocation>();

            var allowedCodes = ApplicationProgressRouteHelper.GetAllowedLocationCodes(this)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationLocation>()
                .Where(l => l.Code != null && allowedCodes.Contains(l.Code))
                .OrderBy(s => s.Code)
                .ToList();
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            applicationTypeQuickCode = applicationType?.SelectionCode;
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }
    }
}