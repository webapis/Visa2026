using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using System.Linq;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;
using Visa2026.Module.Documentation;

namespace Visa2026.Module.BusinessObjects
{
    [UserDocumentation("applications/overview", Category = "ApplicationProfileInstances")]
    [Table("ApplicationProfileInstances")]
    [DefaultClassOptions]
    [NavigationItem(false)]
    [XafDisplayName("Application Profile instance")]
    [DefaultProperty(nameof(DisplayCaption))]
    [RuleCriteria(
        "Application_ProfileOrTypeRequired",
        DefaultContexts.Save,
        "ApplicationProfile is not null Or ApplicationType is not null",
        CustomMessageTemplate = "Application Profile is required.")]
    [Appearance(
        "ApplicationReadOnlyAfterOfficePreparation",
        AppearanceItemType = "ViewItem",
        TargetItems = ApplicationProfileInstanceProgressProfileResolver.LockedApplicationHeaderTargetItems,
        Criteria = "IsLockedAfterOfficePreparation",
        Enabled = false,
        Context = "DetailView")]
    [Appearance(
        "ApplicationReadOnlyWhenWorkflowTerminal",
        AppearanceItemType = "ViewItem",
        TargetItems = ApplicationProfileInstanceProgressProfileResolver.TerminalLockedApplicationDetailTargetItems,
        Criteria = "IsWorkflowTerminal",
        Enabled = false,
        Context = "DetailView")]
//    [RuleUniqueValue("UniqueAppNumberPerPrefix", DefaultContexts.Save, "AppNumberPrefix;ApplicationNumber;Year", CustomMessageTemplate = "An application with this prefix, number, and year already exists.")]
    public partial class ApplicationProfileInstance : BaseObject, IBoListRowState
    {
        private const string DefaultBorderZoneLocationNameTm = "Ýok";

        private const string BorderZoneLocationVisibleCriteria = "!CfgShowBorderZoneLocation";

        private const string AppInvApplicationTypeName = "App_Inv";
        private const string AppInvAndWpApplicationTypeName = "App_Inv_And_WP";
        private const string AppInvAccordingToWpApplicationTypeName = "App_Inv_According_to_WP";
        private const string AppVisaAndWpExtApplicationTypeName = "App_Visa_and_WP_Ext";
        private const string AppVisaExtAccordingToWpApplicationTypeName = "App_Visa_Ext_According_to_WP";
        private const string AppInvFmApplicationTypeName = "App_Inv_FM";
        private const string AppVisaExtFmApplicationTypeName = "App_Visa_Ext_FM";
        private const string AppVisaForNewBornFmApplicationTypeName = "App_Visa_For_New_Born_FM";
        private const string AppVisaExtApplicationTypeName = "App_Visa_Ext";
        private const string AppExitVisaApplicationTypeName = "App_Exit_Visa";
        private const string AppServicePassportApplicationTypeName = "App_Sevice_Passport";
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
        /// <summary>Default visa type for WP-linked application types (see visa-type.json <c>WP</c> / WP-Işçi Wiza).</summary>
        private const string WpDefaultVisaTypeLocalizationKey = "WP";
        /// <summary>Default visa type for family-member invitation / extension types (see visa-type.json <c>FM</c>).</summary>
        private const string FmDefaultVisaTypeLocalizationKey = "FM";
        /// <summary>Default visa type for exit visa (see visa-type.json <c>EX</c> / EX-Çykyş).</summary>
        private const string ExDefaultVisaTypeLocalizationKey = "EX";
        /// <summary>Default visa type for service-passport invitation (see visa-type.json <c>OF</c>).</summary>
        private const string OfDefaultVisaTypeLocalizationKey = "OF";

        /// <summary>Registration and business-trip application types target a migration-service office.</summary>
        private const string MigrationServiceVisibleCriteria = "!CfgShowMigrationService";

        public ApplicationProfileInstance()
        {
            People = new ObservableCollection<Person>();
            Passports = new ObservableCollection<Passport>();
            Visas = new ObservableCollection<Visa>();
            Educations = new ObservableCollection<Education>();
            AddressesOfResidence = new ObservableCollection<AddressOfResidence>();
            PositionHistories = new ObservableCollection<EmployeePositionHistory>();
            Salaries = new ObservableCollection<EmployeeSalary>();
            MedicalRecords = new ObservableCollection<MedicalRecord>();
            WorkDuties = new ObservableCollection<WorkDuty>();
            InvitationItems = new ObservableCollection<InvitationItem>();
            WorkPermitItems = new ObservableCollection<WorkPermitItem>();
            BorderZoneItems = new ObservableCollection<BorderZoneItem>();
            TravelHistories = new ObservableCollection<TravelHistory>();
            PersonResolvedLinks = new ObservableCollection<ApplicationProfileInstancePersonResolvedLink>();
            Invitations = new ObservableCollection<Invitation>();
            Rejections = new ObservableCollection<Rejection>();
            IssuedVisas = new ObservableCollection<Visa>();
            WorkPermits = new ObservableCollection<WorkPermit>();
            BorderZones = new ObservableCollection<BorderZone>();
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>();
            ApprovalLegSnapshots = new ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>();
        }

        [XafDisplayName("Manual Entry")]
        [ToolTip("Enable to manually set the application number for historical records that existed before this system was deployed.")]
        [VisibleInListView(false)]
        [ImmediatePostData]
        public virtual bool IsManualEntry { get; set; }

        /// <summary>
        /// When true, <see cref="ApplicationProfileInstanceProgressInitializer"/> does not seed the first progress row.
        /// Used by VISA2014 OData import — synthetic <see cref="ApplicationProfileInstanceProgress"/> is imported separately.
        /// </summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual bool SuppressInitialProgress { get; set; }

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

        /// <summary>
        /// Denormalized migration-service process number from <c>PROCESS_STARTED</c>
        /// (see <see cref="ApplicationProcessNumberHelper"/>). Editable after Submitted without revert.
        /// </summary>
        [XafDisplayName("Process number")]
        [MaxLength(100)]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        public virtual string? ProcessNumber { get; set; }

        /// <summary>
        /// Set by Start process (merge/ready). Distinguishes staged office prep from an in-process
        /// case that has not yet received a migration-service process number.
        /// </summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual bool HasLeftStagedQueue { get; set; }

        /// <summary>
        /// Lookup / object caption: application number, plus process number when present
        /// (e.g. <c>12/-7010 · AS538188</c>).
        /// </summary>
        [XafDisplayName("Application Profile instance")]
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public string DisplayCaption => ApplicationProcessNumberHelper.FormatDisplayCaption(this);

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
        [XafDisplayName("ApplicationProfileInstance Date (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ApplicationDateText => ApplicationDate.ToString("dd.MM.yyyy");

        [XafDisplayName("Person count")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public int TotalPersonCount => listViewTotalPersonCount ?? ApplicationRosterHelper.GetRosterPersonCountInMemory(this);

        private string applicationTypeQuickCode;

        [XafDisplayName("ApplicationProfileInstance Type Code")]
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
        /// (<see cref="ApplicationProfileInstanceProgressRouteNavigation"/>). Filters the type-code picker until saved.
        /// </summary>
        [Browsable(false)]
        [NotMapped]
        public virtual ApplicationProfileInstanceProgressRouteKind? CreationProgressRoute { get; set; }

        /// <summary>Filtered by <see cref="ApplicationProfileInstanceProgressRouteHelper"/> for nested <see cref="ApplicationProfileInstanceProgress"/> entry.</summary>
        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationState> AvailableProgressStates => LoadAvailableProgressStates();

        /// <summary>Filtered by <see cref="ApplicationProfileInstanceProgressRouteHelper"/> for nested <see cref="ApplicationProfileInstanceProgress"/> entry.</summary>
        [Browsable(false)]
        [NotMapped]
        public IList<ApplicationLocation> AvailableProgressLocations => LoadAvailableProgressLocations();

        private ApplicationListViewDisplayState? listViewDisplayState;
        private string? listRowCssClass;
        private int? listViewTotalPersonCount;

        /// <summary>Clears cached ListView computed fields (progress display, SLA, row color).</summary>
        public void InvalidateListViewDisplayCache()
        {
            listViewDisplayState = null;
            listRowCssClass = null;
        }

        public void SetListViewTotalPersonCount(int count) => listViewTotalPersonCount = count;

        /// <summary>Precomputes ListView display fields after related collections are preloaded.</summary>
        public void WarmListViewDisplayCache()
        {
            // Always recompute: the grid may have evaluated NotMapped SLA fields before
            // ApplicationListViewPreloadController included ApplicationProfile / LatestProgress.
            var state = ApplicationListViewDisplayState.Resolve(this);
            listViewDisplayState = state;
            listRowCssClass = state.ListRowCssClass;
        }

        private ApplicationListViewDisplayState ListViewDisplay =>
            listViewDisplayState ??= ApplicationListViewDisplayState.Resolve(this);

        /// <summary>
        /// Row background key for ApplicationProfileInstance ListViews (SLA warning/overdue overrides primary progress state).
        /// </summary>
        [Browsable(false)]
        [NotMapped]
        public string ListRowAppearanceStateCode => ListViewDisplay.ListRowAppearanceStateCode;

        /// <summary>Precomputed row CSS classes for ApplicationProfileInstance ListView virtual scroll (see <see cref="ApplicationProfileInstanceProgressRowAppearanceController"/>).</summary>
        [Browsable(false)]
        [NotMapped]
        public string ListRowCssClass => listRowCssClass ?? ListViewDisplay.ListRowCssClass;

        /// <summary>
        /// Latest <see cref="ApplicationProfileInstanceProgress"/> state/location code for ListView row color (<see cref="IBoListRowState"/>).
        /// </summary>
        [Browsable(false)]
        [NotMapped]
        public string PrimaryStateCode =>
            !string.IsNullOrEmpty(LatestPrimaryStateCode)
                ? LatestPrimaryStateCode
                : ListViewDisplay.PrimaryStateCode;

        /// <summary>
        /// Latest progress state and location (localized) for ListView — <see cref="ApplicationProfileInstanceProgressPrimaryStateCodeResolver.ResolveDisplayName"/>.
        /// </summary>
        [XafDisplayName("Current status")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [NotMapped]
        public string CurrentState => ListViewDisplay.CurrentState;

        /// <summary>Localized state from the latest <see cref="ApplicationProfileInstanceProgress"/> row.</summary>
        [XafDisplayName("Latest progress state")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public string LatestProgressState =>
            !string.IsNullOrWhiteSpace(LatestProgressDisplay)
                ? LatestProgressDisplay!
                : ListViewDisplay.CurrentState;

        /// <summary>Date from the latest <see cref="ApplicationProfileInstanceProgress"/> row.</summary>
        [XafDisplayName("Latest progress date")]
        [ModelDefault("AllowEdit", "False")]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public DateTime? LatestProgressDate => ListViewDisplay.LatestProgressDate;

        /// <summary>Latest progress is <c>PROCESS_ISSUED</c>.</summary>
        [Browsable(false)]
        [NotMapped]
        public bool IsIssued =>
            string.Equals(ListViewDisplay.PrimaryStateCode, ApplicationProfileInstanceProgressStateCodes.ProcessIssued, StringComparison.OrdinalIgnoreCase);

        /// <summary>Closed workflow: issued, rejected, or cancelled at migration service.</summary>
        [Browsable(false)]
        [NotMapped]
        public bool IsWorkflowTerminal => ApplicationProfileInstanceProgressProfileResolver.IsWorkflowTerminal(this);

        [XafDisplayName("Working days")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [NotMapped]
        public int? WorkingDaysInCurrentStep => ListViewDisplay.WorkingDaysInCurrentStep;

        [XafDisplayName("Approval deadline")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public string ProgressSlaStatement => ListViewDisplay.ProgressSlaStatement;

        [Browsable(false)]
        [NotMapped]
        public string ProgressSlaAppearanceCode => ListViewDisplay.ProgressSlaAppearanceCode;

        [XafDisplayName("Migration working days")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [NotMapped]
        public int? WorkingDaysInMigrationStep => ListViewDisplay.WorkingDaysInMigrationStep;

        [Appearance("ApprovalLegProfileVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowApprovalLegProfile", Context = "DetailView")]
        [VisibleInListView(false)]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableApprovalLegProfiles))]
        public virtual ApprovalLegProfile ApprovalLegProfile { get; set; }

        [Browsable(false)]
        [NotMapped]
        public IList<ApprovalLegProfile> AvailableApprovalLegProfiles => LoadAvailableApprovalLegProfiles();

        [XafDisplayName("Migration deadline")]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [VisibleInListView(true)]
        [NotMapped]
        public string MigrationSlaStatement => ListViewDisplay.MigrationSlaStatement;

        private ApplicationType applicationType;
        /// <summary>
        /// DEPRECATED — use <see cref="ApplicationProfile"/>. Retained for dual-read / import
        /// (see docs/DEPRECATED.md, docs/APPLICATION_PROFILE_PLAN.md).
        /// </summary>
        [ImmediatePostData]
        [DataSourceCriteria("!IsNullOrEmpty(SelectionCode)")]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [Appearance("ApplicationTypeReadOnlyOnDetail", Enabled = false, Context = "DetailView")]
        [Appearance(
            "HideApplicationTypeWhenProfileSet",
            Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide,
            Criteria = "ApplicationProfile is not null",
            Context = "DetailView")]
        [XafDisplayName("ApplicationProfileInstance Type (Deprecated)")]
        [ToolTip("Deprecated. Prefer Application Profile. Retained for legacy rows and import until cutover.")]
        public virtual ApplicationType ApplicationType
        {
            get => applicationType;
            set
            {
                if (applicationType != value)
                {
                    applicationType = value;
                    ApplyDefaultsForApplicationType();
                }
            }
        }

        private ApplicationProfile? applicationProfile;
        /// <summary>
        /// Live Application Profile (configuration). Set only at ApplicationProfileInstance create; not switched later.
        /// Replaces deprecated <see cref="ApplicationType"/> after cutover.
        /// </summary>
        [ImmediatePostData]
        [DataSourceCriteria("IsActive")]
        [Appearance("ApplicationProfileReadOnlyOnDetail", Enabled = false, Context = "DetailView")]
        [ToolTip("Live configuration for this Application. Chosen at create (or Person/Dossier start); not switched later. Profile config applies live until lock state A.")]
        [XafDisplayName("Application Profile")]
        public virtual ApplicationProfile? ApplicationProfile
        {
            get => applicationProfile;
            set
            {
                if (applicationProfile != value)
                {
                    applicationProfile = value;
                    ApplyDefaultsForApplicationProfile();
                }
            }
        }

        private void ApplyDefaultsForApplicationProfile()
        {
            var os = ObjectSpaceHelper.Get(this);
            if (os == null || applicationProfile == null)
                return;

            if (applicationProfile.DefaultVisaType != null)
                VisaType = applicationProfile.DefaultVisaType;
            if (applicationProfile.DefaultVisaCategory != null)
                VisaCategory = applicationProfile.DefaultVisaCategory;
            if (applicationProfile.DefaultVisaPeriod != null)
                VisaPeriod = applicationProfile.DefaultVisaPeriod;
            if (applicationProfile.DefaultUrgency != null)
                Urgency = applicationProfile.DefaultUrgency;
            if (applicationProfile.DefaultProjectContract != null)
                ProjectContract = applicationProfile.DefaultProjectContract;
            if (applicationProfile.DefaultMigrationService != null)
                MigrationService = applicationProfile.DefaultMigrationService;
            if (applicationProfile.DefaultEntryCheckPoint != null)
                EntryCheckPoint = applicationProfile.DefaultEntryCheckPoint;
            if (applicationProfile.DefaultRegion != null)
                Region = applicationProfile.DefaultRegion;
            if (applicationProfile.DefaultCity != null)
                City = applicationProfile.DefaultCity;
            if (applicationProfile.DefaultBusinessTripAddress != null)
                BusinessTripAddress = applicationProfile.DefaultBusinessTripAddress;
            if (!string.IsNullOrWhiteSpace(applicationProfile.DefaultPurpose))
                Purpose = applicationProfile.DefaultPurpose.Trim();
            if (ApplicationProfileConfigurationResolver.RequireBorderZoneWhenProducingInvitationOrVisa(
                applicationProfile.ProduceInvitation,
                applicationProfile.ProduceVisa,
                applicationProfile.RequireBorderZone))
            {
                BorderZoneLocation = string.IsNullOrWhiteSpace(applicationProfile.DefaultBorderZoneLocation)
                    ? BorderZoneSelectionHelper.NoneValue
                    : applicationProfile.DefaultBorderZoneLocation;
            }
            else if (!string.IsNullOrWhiteSpace(applicationProfile.DefaultBorderZoneLocation))
            {
                BorderZoneLocation = applicationProfile.DefaultBorderZoneLocation;
            }
            if (applicationProfile.DefaultWorkPermitLocation != null)
                MovementPermitLocation = applicationProfile.DefaultWorkPermitLocation;
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

            if (string.Equals(applicationTypeName, AppInvAndWpApplicationTypeName, StringComparison.Ordinal)
                || string.Equals(applicationTypeName, AppInvAccordingToWpApplicationTypeName, StringComparison.Ordinal)
                || string.Equals(applicationTypeName, AppVisaAndWpExtApplicationTypeName, StringComparison.Ordinal)
                || string.Equals(applicationTypeName, AppVisaExtAccordingToWpApplicationTypeName, StringComparison.Ordinal))
            {
                visaPeriodLocalizationKey = AppInvAndWpDefaultVisaPeriodLocalizationKey;
                visaCategoryLocalizationKey = AppInvAndWpDefaultVisaCategoryLocalizationKey;
                visaTypeLocalizationKey = WpDefaultVisaTypeLocalizationKey;
                return true;
            }

            if (string.Equals(applicationTypeName, AppInvFmApplicationTypeName, StringComparison.Ordinal)
                || string.Equals(applicationTypeName, AppVisaExtFmApplicationTypeName, StringComparison.Ordinal)
                || string.Equals(applicationTypeName, AppVisaForNewBornFmApplicationTypeName, StringComparison.Ordinal))
            {
                visaTypeLocalizationKey = FmDefaultVisaTypeLocalizationKey;
                return true;
            }

            if (string.Equals(applicationTypeName, AppVisaExtApplicationTypeName, StringComparison.Ordinal))
            {
                visaTypeLocalizationKey = WpDefaultVisaTypeLocalizationKey;
                return true;
            }

            if (string.Equals(applicationTypeName, AppExitVisaApplicationTypeName, StringComparison.Ordinal))
            {
                visaTypeLocalizationKey = ExDefaultVisaTypeLocalizationKey;
                return true;
            }

            if (string.Equals(applicationTypeName, AppServicePassportApplicationTypeName, StringComparison.Ordinal))
            {
                visaTypeLocalizationKey = OfDefaultVisaTypeLocalizationKey;
                return true;
            }

            return false;
        }

        [Appearance("VisaPeriodVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowVisaPeriod", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual VisaPeriod VisaPeriod { get; set; }

        [Appearance("VisaCategoryVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowVisaCategory", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual VisaCategory VisaCategory { get; set; }

        [Appearance("VisaTypeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowVisaType", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual VisaType VisaType { get; set; }

        [Appearance("EntryCheckPointVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowEntryCheckPoint", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual CheckPoint? EntryCheckPoint { get; set; }

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [NotMapped]
        public bool IsLockedAfterOfficePreparation =>
            ApplicationProfileInstanceProgressProfileResolver.IsApplicationLockedAfterOfficePreparation(
                this, ObjectSpaceHelper.Get(this));

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [NotMapped]
        public bool IsProjectContractLocked =>
            ApplicationProfileConfigurationResolver.ShowProjectContract(this) && IsLockedAfterOfficePreparation;

        [Appearance("ProjectContractVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowProjectContract", Context = "DetailView")]
        [VisibleInListView(false)]
        [ImmediatePostData]
        [DataSourceProperty(nameof(AvailableProjectContracts))]
        public virtual ProjectContract ProjectContract { get; set; }

        [Browsable(false)]
        [NotMapped]
        public IList<ProjectContract> AvailableProjectContracts => LoadAvailableProjectContracts();

        [Browsable(false)]
        [Aggregated]
        [InverseProperty(nameof(ApplicationProfileInstanceApprovalLegSnapshot.ApplicationProfileInstance))]
        public virtual IList<ApplicationProfileInstanceApprovalLegSnapshot> ApprovalLegSnapshots { get; set; }

        /// <summary>Name of the profile approval-leg version copied at create (snapshot; not live).</summary>
        [MaxLength(200)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual string? ApprovalLegVersionName { get; set; }

        /// <summary>Id of the profile version chosen at create (audit only; timeline reads snapshots).</summary>
        [Browsable(false)]
        public virtual Guid? ApprovalLegVersionId { get; set; }

        [Appearance("UrgencyVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowUrgency", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual Urgency Urgency { get; set; }

        [Appearance("MigrationServiceVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = MigrationServiceVisibleCriteria, Context = "DetailView")]
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

        [XafDisplayName("Company Tax Information (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_Company_TaxInformation =>
            OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this))?.TaxInformation ?? string.Empty;

        [XafDisplayName("Company Registration Date (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_Company_RegistrationDateText =>
            OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this))?.RegistrationDateText ?? string.Empty;

        [XafDisplayName("Company registry, address and phone (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Application_CompanyRegistryAddressLine
        {
            get
            {
                var c = OrganizationReportHelper.GetCompanyProfile(ObjectSpaceHelper.Get(this));
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

        [XafDisplayName("Signatory Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CompanyHead_PassportNumber =>
            OrganizationReportHelper.GetSignatory(ObjectSpaceHelper.Get(this))?.PassportNumber ?? string.Empty;

        [XafDisplayName("Signatory Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CompanyHead_PassportAuthority =>
            OrganizationReportHelper.GetSignatory(ObjectSpaceHelper.Get(this))?.PassportAuthority ?? string.Empty;

        [XafDisplayName("Signatory Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CompanyHead_PassportIssueDateText =>
            OrganizationPassportLineHelper.FormatIssueDateText(
                OrganizationReportHelper.GetSignatory(ObjectSpaceHelper.Get(this))?.PassportIssueDate);

        [XafDisplayName("Signatory Passport (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CompanyHead_PassportLine =>
            OrganizationReportHelper.GetSignatory(ObjectSpaceHelper.Get(this))?.PassportLine ?? string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_FullName => Application_CompanyHead_FullName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PositionTm => Application_CompanyHead_PositionTm;

        [XafDisplayName("Representative Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_FullName =>
            OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this))?.FullName ?? string.Empty;

        [XafDisplayName("Representative Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_PositionTm =>
            OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this))?.PositionTitleTm ?? string.Empty;

        [XafDisplayName("Representative Phone"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_Phone =>
            OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this))?.Phone ?? string.Empty;

        [XafDisplayName("Representative Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_PassportNumber =>
            OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this))?.PassportNumber ?? string.Empty;

        [XafDisplayName("Representative Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_PassportAuthority =>
            OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this))?.PassportAuthority ?? string.Empty;

        [XafDisplayName("Representative Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_PassportIssueDateText =>
            OrganizationPassportLineHelper.FormatIssueDateText(
                OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this))?.PassportIssueDate);

        [XafDisplayName("Representative Passport (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_PassportLine =>
            OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this))?.PassportLine ?? string.Empty;

        [XafDisplayName("Representative passport, authority and phone (one line)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string Representative_PassportPhoneLine
        {
            get
            {
                var r = OrganizationReportHelper.GetRepresentative(ObjectSpaceHelper.Get(this));
                return r == null
                    ? string.Empty
                    : OrganizationPassportLineHelper.FormatNumberAuthorityPhone(r.PassportNumber, r.PassportAuthority, r.Phone);
            }
        }

        [XafDisplayName("ApplicationProfileInstance Type Name (Word)"), VisibleInDetailView(false), VisibleInListView(false)]
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
        public string ProjectContract_Description => string.Empty;

        [XafDisplayName("Ministry Recipient Block"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ProjectContract_Ministry_RecipientBlock => string.Empty;

        [XafDisplayName("Ministry Form of Address"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string ProjectContract_Ministry_FormOfAddress => string.Empty;

        [XafDisplayName("FM Relationship (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string FamilyMember_Relationship_NameTm =>
            JoinTurkmenList(
                ApplicationRosterHelper.GetRosterPeople(this)
                    .Select(p => p.Relationship)
                    .Where(r => r != null)
                    .Select(r => string.IsNullOrEmpty(r!.ReverseNameTm) ? r.NameTm : r.ReverseNameTm)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .Distinct()
                    .Select(AddTurkmenGenitive)
                    .ToList());

        [XafDisplayName("Sponsoring Employee Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string SponsoringEmployee_FullName =>
            ApplicationRosterHelper.GetRosterPeople(this).FirstOrDefault()?.SponsoringEmployee?.FullName;

        [XafDisplayName("Sponsoring Employee Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string SponsoringEmployee_PositionTm =>
            PersonCurrentItems.GetCurrentPositionHistory(
                ApplicationRosterHelper.GetRosterPeople(this).FirstOrDefault()?.SponsoringEmployee)?.Position?.NameTm;

        [Appearance("BusinessTripStartDateVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowBusinessTrips", Context = "DetailView")]
        [VisibleInListView(false)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? BusinessTripStartDate { get; set; }

        [Appearance("BusinessTripEndDateVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowBusinessTrips", Context = "DetailView")]
        [VisibleInListView(false)]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime? BusinessTripEndDate { get; set; }

        [Appearance("BusinessTripPurposeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowBusinessTrips", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual BusinessTripPurpose BusinessTripPurpose { get; set; }

        [Appearance("MovementPermitLocationVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowMovementPermitLocation", Context = "DetailView")]
        [VisibleInListView(false)]
        [XafDisplayName("Work permit location")]
        [MaxLength(500)]
        [EditorAlias(CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation)]
        [CommaSeparatedMultiSelect(
            CatalogEntityType = typeof(WorkPermittedLocationName),
            NoneValue = "")]
        public virtual string MovementPermitLocation { get; set; }

        [Browsable(false)]
        [XafDisplayName("Work permit location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MovementPermitLocation_NameTm => MovementPermitLocation?.Trim() ?? string.Empty;

        [Appearance("BorderZoneLocationVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide,
            Criteria = "ApplicationType is null or !ApplicationType.ShowBorderZoneLocation", Context = "DetailView,ListView")]
        [VisibleInListView(false)]
        [MaxLength(500)]
        [RuleRequiredField(TargetCriteria = BorderZoneLocationVisibleCriteria)]
        [EditorAlias(CommaSeparatedMultiSelectEditorAliases.BorderZone)]
        [CommaSeparatedMultiSelect(
            CatalogEntityType = typeof(BorderZoneName),
            NoneValue = CommaSeparatedSelectionHelper.NoneValue)]
        public virtual string BorderZoneLocation { get; set; }

        [Browsable(false)]
        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string BorderZoneLocation_NameTm =>
            BorderZoneSelectionHelper.IsNoneValue(BorderZoneLocation)
                ? DefaultBorderZoneLocationNameTm
                : BorderZoneLocation?.Trim() ?? DefaultBorderZoneLocationNameTm;

        [Appearance("FromCityVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowFromCity", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual City FromCity { get; set; }

        [Appearance("ToCityVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowToCity", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual City ToCity { get; set; }

        [Appearance("RegionVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowRegion", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual Region Region { get; set; }

        [Appearance("CityVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowCity", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual City City { get; set; }

        [Appearance("BusinessTripAddressVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowBusinessTripAddress", Context = "DetailView")]
        [VisibleInListView(false)]
        [XafDisplayName("Business trip address")]
        public virtual BusinessTripAddress BusinessTripAddress { get; set; }

        [Appearance("PurposeVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowPurpose", Context = "DetailView")]
        [VisibleInListView(false)]
        [XafDisplayName("Purpose")]
        [MaxLength(700)]
        public virtual string? Purpose { get; set; }

        #region Person Count

        private IEnumerable<ApplicationRosterMergeLine> RosterLinesForReports() =>
            ApplicationRosterHelper.GetMergeLineItems(this);

        [XafDisplayName("Total Person Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string TotalPersonCountText => NumberToTurkmenWords(TotalPersonCount);

        // Used by App_Cancel_Visa_and_WP and App_Cancel_Inv_WP reports
        [XafDisplayName("Cancel Person Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelPersonCount => RosterLinesForReports().Count();

        [XafDisplayName("Cancel Person Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CancelPersonCountText => NumberToTurkmenWords(CancelPersonCount);

        /// <summary>
        /// Total visas requested for cancellation on <see cref="App_Cancel_Visa"/> applications:
        /// per active line, +1 when <see cref="ApplicationRosterMergeLine.CurrentVisa"/> is set and +1 when <see cref="ApplicationRosterMergeLine.NextVisa"/> is set.
        /// </summary>
        [XafDisplayName("Cancel Visa Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelVisaCount => RosterLinesForReports()
            .Sum(ai => (ai.CurrentVisa != null ? 1 : 0) + (ai.NextVisa != null ? 1 : 0));

        [XafDisplayName("Cancel Visa Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CancelVisaCountText => NumberToTurkmenWords(CancelVisaCount);

        [XafDisplayName("Cancel WP Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelWPCount => RosterLinesForReports().Count()
            + RosterLinesForReports().Count(ai => ai.PreviousWorkPermitItem != null);

        [XafDisplayName("Cancel WP Count (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public string CancelWPCountText => NumberToTurkmenWords(CancelWPCount);

        [XafDisplayName("Cancel Inv Count"), VisibleInDetailView(false), VisibleInListView(false)]
        [NotMapped]
        public int CancelInvCount => RosterLinesForReports().Count(ai => ai.CurrentInvitationItem != null);

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

        [XafDisplayName("People")]
        [InverseProperty(nameof(Person.ApplicationProfileInstances))]
        [VisibleInListView(false)]
        public virtual IList<Person> People { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<Passport> Passports { get; set; }

        /// <summary>Input linked visas (skip-nav M2M). Issued visas use <see cref="IssuedVisas"/> / <see cref="Visa.IssuingApplicationProfileInstance"/>.</summary>
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<Visa> Visas { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<Education> Educations { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<AddressOfResidence> AddressesOfResidence { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<EmployeePositionHistory> PositionHistories { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<EmployeeSalary> Salaries { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<MedicalRecord> MedicalRecords { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<WorkDuty> WorkDuties { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<InvitationItem> InvitationItems { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<BorderZoneItem> BorderZoneItems { get; set; }

        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        public virtual IList<TravelHistory> TravelHistories { get; set; }

        /// <summary>Sticky ResolvedLinks for roster people (cascade with the instance; not aggregated onto Person).</summary>
        [Browsable(false)]
        [Aggregated]
        [VisibleInListView(false)]
        [VisibleInDetailView(false)]
        public virtual IList<ApplicationProfileInstancePersonResolvedLink> PersonResolvedLinks { get; set; }

        /// <summary>Invitations this case may produce (1:N via <see cref="Invitation.ApplicationProfileInstance"/>). Visible when the profile <c>May produce invitation</c> is on.</summary>
        [Aggregated]
        [InverseProperty(nameof(Invitation.ApplicationProfileInstance))]
        [Appearance("InvitationsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowInvitations", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<Invitation> Invitations { get; set; }

        /// <summary>Visas this case issued (new visa or visa extension) via <see cref="Visa.IssuingApplicationProfileInstance"/>. Visible when May produce visa (or invitation, which may later issue a visa).</summary>
        [Aggregated]
        [InverseProperty(nameof(Visa.IssuingApplicationProfileInstance))]
        [Appearance("IssuedVisasVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowIssuedVisas", Context = "DetailView")]
        [VisibleInListView(false)]
        [XafDisplayName("Issued visas")]
        public virtual IList<Visa> IssuedVisas { get; set; }

        /// <summary>Rejections this case may produce (1:N via <see cref="Rejection.ApplicationProfileInstance"/>). Visible when the profile <c>May produce rejection</c> is on.</summary>
        [Aggregated]
        [InverseProperty(nameof(Rejection.ApplicationProfileInstance))]
        [Appearance("RejectionsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowRejections", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<Rejection> Rejections { get; set; }

        /// <summary>Work permits this case may produce (1:N via <see cref="WorkPermit.ApplicationProfileInstance"/>). Visible when the profile <c>May produce work permit</c> is on.</summary>
        [Aggregated]
        [InverseProperty(nameof(WorkPermit.ApplicationProfileInstance))]
        [Appearance("WorkPermitsVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowWorkPermits", Context = "DetailView")]
        [VisibleInListView(false)]
        public virtual IList<WorkPermit> WorkPermits { get; set; }

        /// <summary>Border-zone permits this case may produce (1:N via <see cref="BorderZone.ApplicationProfileInstance"/>). Visible when the profile <c>May produce border zone</c> is on.</summary>
        [Aggregated]
        [InverseProperty(nameof(BorderZone.ApplicationProfileInstance))]
        [Appearance("BorderZonesVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!CfgShowBorderZones", Context = "DetailView")]
        [VisibleInListView(false)]
        [XafDisplayName("Border zone permits")]
        public virtual IList<BorderZone> BorderZones { get; set; }

        // [RuleRequiredField]
        // [DataSourceCriteria("ApplicationType.ID = '@This.ApplicationType.ID'")]
        // [Appearance("ApplicationReasonVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "ApplicationType is null or !ApplicationType.ShowApplicationReason", Context = "DetailView")]
        // public virtual ApplicationReason ApplicationReason { get; set; }

        [Aggregated]
        [InverseProperty(nameof(ApplicationProfileInstanceProgress.ApplicationProfileInstance))]
        [VisibleInListView(false)]
        public virtual IList<ApplicationProfileInstanceProgress> ProgressHistory { get; set; }

        /// <summary>Denormalized pointer to the latest <see cref="ApplicationProfileInstanceProgress"/> row (list/query performance).</summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual Guid? LatestProgressId { get; set; }

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual ApplicationProfileInstanceProgress? LatestProgress { get; set; }

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [MaxLength(64)]
        public virtual string? LatestPrimaryStateCode { get; set; }

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [MaxLength(255)]
        public virtual string? LatestProgressDisplay { get; set; }

        /// <summary>
        /// Officer notes while history is empty (implied office). Copied onto the first
        /// real progress row on advance, then cleared. Not shown on native DetailView.
        /// </summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string? OfficePreparationNotes { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpaceHelper.Get(this) != null
                && string.IsNullOrWhiteSpace(BorderZoneLocation))
            {
                BorderZoneLocation = DefaultBorderZoneLocationNameTm;
            }

            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                ApplicationDate = DateTime.Now;
                Urgency = objectSpace.GetObjectsQuery<Urgency>().FirstOrDefault(u => u.IsDefault);
                VisaType = objectSpace.GetObjectsQuery<VisaType>().FirstOrDefault(v => v.IsDefault);
                VisaCategory = objectSpace.GetObjectsQuery<VisaCategory>().FirstOrDefault(vc => vc.IsDefault);
                VisaPeriod = objectSpace.GetObjectsQuery<VisaPeriod>().FirstOrDefault(vp => vp.IsDefault);
                ProjectContract = objectSpace.GetObjectsQuery<ProjectContract>().FirstOrDefault(pc => pc.IsDefault);
                if (!SuppressInitialProgress && !MigrationImportContext.IsDataImport)
                    ApplicationProfileInstanceProgressInitializer.EnsureInitialProgress(this, objectSpace);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();
            if (ApplicationProfileConfigurationResolver.ShowBorderZoneLocation(this))
                BorderZoneSelectionHelper.ApplyDefaultIfEmpty(this);

            if (ObjectSpaceHelper.Get(this) != null && ObjectSpaceHelper.Get(this).IsNewObject(this))
            {
                Year = ApplicationDate.Year;
                Month = ApplicationDate.Month;

                var numbering = GetNumberingConfiguration();
                if (string.IsNullOrEmpty(AppNumberPrefix))
                    AppNumberPrefix = numbering.Prefix;

                if (IsManualEntry)
                {
                    ApplyManualEntryNumbering(numbering);
                    return;
                }

                if (string.IsNullOrEmpty(ApplicationNumber))
                {
                    string fmt = numbering.Format;
                    bool scopeByYear  = string.IsNullOrEmpty(fmt) || fmt.Contains("{YEAR}")  || fmt.Contains("{YEAR2}");
                    bool scopeByMonth = !string.IsNullOrEmpty(fmt) && (fmt.Contains("{MONTH}") || fmt.Contains("{MONTH2}"));

                    var dbQuery = ObjectSpaceHelper.Get(this).GetObjectsQuery<ApplicationProfileInstance>()
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
                        var localApps = baseObjectSpace.ModifiedObjects.OfType<ApplicationProfileInstance>()
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
                ApplyManualEntryNumbering(GetNumberingConfiguration());
            }

            ApprovalLegProfileMinistryHelper.EnsureSnapshots(ObjectSpaceHelper.Get(this), this);
        }

        /// <summary>
        /// Manual entry / VISA2014 import: preserve <see cref="FullApplicationNumber"/> when already set;
        /// parse sequence into <see cref="ApplicationNumber"/> instead of re-applying <c>AppNumberFormat</c>.
        /// </summary>
        private void ApplyManualEntryNumbering((string Prefix, string Format, int Seed, int Padding) numbering)
        {
            Year = ApplicationDate.Year;
            Month = ApplicationDate.Month;

            if (MigrationImportContext.IsDataImport)
            {
                ApplyImportedManualNumbering();
                return;
            }

            if (string.IsNullOrEmpty(AppNumberPrefix))
                AppNumberPrefix = numbering.Prefix;

            if (!string.IsNullOrWhiteSpace(FullApplicationNumber))
            {
                ApplicationManualNumberParser.Parse(
                    FullApplicationNumber,
                    out var parsedFull,
                    out var parsedPrefix,
                    out var parsedNumber);
                FullApplicationNumber = parsedFull;
                if (!string.IsNullOrEmpty(parsedPrefix))
                    AppNumberPrefix = parsedPrefix;
                if (!string.IsNullOrEmpty(parsedNumber))
                    ApplicationNumber = parsedNumber;
                else if (string.IsNullOrEmpty(ApplicationNumber))
                    ApplicationNumber = FullApplicationNumber;
                return;
            }

            if (!string.IsNullOrEmpty(ApplicationNumber))
            {
                FullApplicationNumber = BuildFullNumber(
                    numbering.Format,
                    AppNumberPrefix,
                    Year, Month,
                    ApplicationNumber);
            }
        }

        /// <summary>
        /// VISA2014 import: keep legacy <see cref="FullApplicationNumber"/> verbatim; never apply <c>AppNumberFormat</c>.
        /// </summary>
        private void ApplyImportedManualNumbering()
        {
            if (!string.IsNullOrWhiteSpace(FullApplicationNumber))
            {
                ApplicationManualNumberParser.Parse(
                    FullApplicationNumber,
                    out var parsedFull,
                    out var parsedPrefix,
                    out var parsedNumber);
                FullApplicationNumber = parsedFull;
                if (!string.IsNullOrEmpty(parsedPrefix))
                    AppNumberPrefix = parsedPrefix;
                if (!string.IsNullOrEmpty(parsedNumber))
                    ApplicationNumber = parsedNumber;
                return;
            }

            if (!string.IsNullOrEmpty(ApplicationNumber))
                FullApplicationNumber = ApplicationNumber;
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

            var allowedCodes = ApplicationProfileInstanceProgressRouteHelper.GetAllowedStateCodes(this)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return objectSpace.GetObjectsQuery<ApplicationState>()
                .Where(s => s.Code != null && allowedCodes.Contains(s.Code))
                .OrderBy(s => s.Code)
                .ToList();
        }

        private IList<ApplicationLocation> LoadAvailableProgressLocations() =>
            Array.Empty<ApplicationLocation>();

        private IList<ApprovalLegProfile> LoadAvailableApprovalLegProfiles()
        {
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace == null)
                return Array.Empty<ApprovalLegProfile>();

            return objectSpace.GetObjectsQuery<ApprovalLegProfile>()
                .Where(profile => profile.IsActive)
                .OrderBy(profile => profile.Code)
                .ToList();
        }

        private IList<ProjectContract> LoadAvailableProjectContracts()
        {
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace == null)
                return Array.Empty<ProjectContract>();

            var query = objectSpace.GetObjectsQuery<ProjectContract>()
                .Where(contract => contract.IsActive);

            if (ApplicationType?.ShowApprovalLegProfile == true && ApprovalLegProfile != null)
            {
                var profileId = ApprovalLegProfile.ID;
                query = query.Where(contract => contract.ApprovalLegProfileId == profileId);
            }

            return query
                .OrderBy(contract => contract.NameTm)
                .ToList();
        }

        public override void OnLoaded()
        {
            base.OnLoaded();
            applicationTypeQuickCode = applicationType?.SelectionCode;
        }

    }
}
