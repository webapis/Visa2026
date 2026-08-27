using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using System.Linq;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
  //  [NavigationItem(false)]
      [NavigationItem("Lookup/Visa/Config")]
    [DefaultProperty(nameof(VisaNumber))]
    [RuleCriteria("Visa_ExpirationDate_GreaterThan_StartDate", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    [Appearance("VisaStateInfo", Priority = 100, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "StateSeverityLevel = 1", Context = "ListView", BackColor = "LightSkyBlue")]
    [Appearance("VisaStateWarning", Priority = 200, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "StateSeverityLevel = 2", Context = "ListView", BackColor = "LightSalmon")]
    [Appearance("VisaStateCritical", Priority = 300, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "StateSeverityLevel >= 3", Context = "ListView", BackColor = "LightCoral")]
    [Appearance("Visa_InputApplicationProfileInstancesHiddenWhenIssued", Priority = 50,
        AppearanceItemType = "ViewItem", TargetItems = "ApplicationProfileInstances",
        Criteria = "IssuingApplicationProfileInstance is not null", Context = "DetailView",
        Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide)]
    [SupportsOptionalDetailFields]
    public class Visa : BaseObject, IExpirationLogic, IOptionalDetailFields
    {
        [MaxLength(50)]
        [RuleRequiredField]
        [ModelDefault("CustomCSSClassName", "e2e-visa-visa-number")]
        public virtual string VisaNumber { get; set; }

        /// <summary>
        /// Enforces uniqueness of <see cref="VisaNumber"/> among non-deleted visas (trimmed, case-insensitive).
        /// </summary>
        [RuleFromBoolProperty("Visa_VisaNumberUniqueAmongActive", DefaultContexts.Save, "Another active visa already uses this visa number.")]
        [Browsable(false)]
        public bool IsVisaNumberUniqueAmongActive
        {
            get
            {
                if (string.IsNullOrWhiteSpace(VisaNumber))
                {
                    return true;
                }

                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    return true;
                }

                var normalized = VisaNumber.Trim().ToUpperInvariant();
                var currentId = ID;

                return !objectSpace.GetObjectsQuery<Visa>()
                    .Where(v => v.ID != currentId && v.VisaNumber != null)
                    .Any(v => v.VisaNumber.Trim().ToUpper() == normalized);
            }
        }

        [RuleRequiredField]
        public virtual VisaType VisaType { get; set; }

        [RuleRequiredField]
        public virtual VisaCategory VisaCategory { get; set; }

        [RuleRequiredField]
        public virtual VisaIssuedPlace VisaIssuedPlace { get; set; }

        /// <summary>
        /// Officer-entered process document number from the visa stamp (legacy <c>ASNumber</c> / Işlenen belgisi),
        /// e.g. <c>C00138718</c>. Distinct from <see cref="Application.ProcessNumber"/> and from
        /// <see cref="LegacyPersonInApplicationProfileInstanceOid"/> (legacy PIA FK also named ProcessNumber in VISA2014).
        /// </summary>
        [Index(0)]
        [RuleRequiredField]
        [XafDisplayName("Process number")]
        [ToolTip("Işlenen belgisi — typed from the visa image stamp (legacy ASNumber).")]
        [MaxLength(100)]
        [ExcludeFromOptionalDetailFields]
        [VisibleInListView(true)]
        [VisibleInLookupListView(false)]
        [ModelDefault("CustomCSSClassName", "e2e-visa-process-number")]
        public virtual string ProcessNumber { get; set; }

        /// <summary>
        /// Legacy <c>Visa.ProcessNumber</c> PersonInApplication Oid (import lineage). Domain link is <see cref="IssuingApplicationProfileInstance"/>.
        /// </summary>
        [XafDisplayName("Legacy PIA Oid")]
        [ModelDefault("AllowEdit", "False")]
        [ExcludeFromOptionalDetailFields]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual Guid? LegacyPersonInApplicationProfileInstanceOid { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [ModelDefault("CustomCSSClassName", "e2e-visa-issue-date")]
        public virtual DateTime IssueDate
        {
            get => issueDate;
            set
            {
                if (issueDate == value)
                {
                    return;
                }

                var previousIssueDate = issueDate;
                issueDate = value;

                // Suggest Start Date = Issue Date when validity start still matched the old issue date or is unset.
                var start = StartDate;
                if (start == default || start.Date == previousIssueDate.Date)
                {
                    StartDate = value.Date;
                }

                if (ObjectSpaceHelper.Get(this) != null)
                {
                    CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(IssueDate));
                }
            }
        }

        private DateTime issueDate;

        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [ModelDefault("CustomCSSClassName", "e2e-visa-start-date")]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        [ModelDefault("CustomCSSClassName", "e2e-visa-expiration-date")]
        public virtual DateTime? ExpirationDate { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        [MaxLength(500)]
        [RuleRequiredField]
        [VisibleInListView(false)]
        [EditorAlias(CommaSeparatedMultiSelectEditorAliases.BorderZone)]
        [CommaSeparatedMultiSelect(
            CatalogEntityType = typeof(BorderZoneName),
            NoneValue = CommaSeparatedSelectionHelper.NoneValue)]
        public virtual string BorderZoneLocation { get; set; }

        [Browsable(false)]
        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string BorderZoneLocation_NameTm =>
            BorderZoneSelectionHelper.IsNoneValue(BorderZoneLocation)
                ? BorderZoneSelectionHelper.NoneValue
                : BorderZoneLocation?.Trim() ?? BorderZoneSelectionHelper.NoneValue;

        /// <summary>
        /// Optional; always visible; read-only (Path A / Path B set on create or import).
        /// Issued invitation line consumed for this visa (not input M2M linked items).
        /// Inverse: <see cref="InvitationItem.IssuedVisa"/>.
        /// </summary>
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [DataSourceProperty(nameof(AvailableIssuingInvitationItems))]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [XafDisplayName("Issuing invitation item")]
        public virtual InvitationItem IssuingInvitationItem { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual Passport Passport
        {
            get => passport;
            set
            {
                if (passport == value)
                    return;
                passport = value;
            }
        }

        private Passport passport;

        /// <summary>
        /// Parent application that issued this visa.
        /// Set at create from Application Profile Instance Issued records or import Path B; read-only on detail.
        /// </summary>
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [DataSourceProperty(nameof(AvailableIssuingApplicationProfileInstances))]
        [InverseProperty(nameof(ApplicationProfileInstance.IssuedVisas))]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [XafDisplayName("Issuing profile instance")]
        [ToolTip("Application Profile Instance that produced this visa. Set when created from Issued records on that case.")]
        public virtual ApplicationProfileInstance IssuingApplicationProfileInstance { get; set; }

        /// <summary>Prevents Path A matcher from running more than once on a new Visa instance.</summary>
        [NotMapped]
        [Browsable(false)]
        public bool PathAIssuingLinksApplied { get; set; }

        [NotMapped]
        [Browsable(false)]
        public IList<ApplicationProfileInstance> AvailableIssuingApplicationProfileInstances
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                var person = Passport?.Person;
                if (objectSpace == null || person == null)
                    return new List<ApplicationProfileInstance>();

                var personId = person.ID;
                return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
                    .Where(a => a.People.Any(p => p.ID == personId))
                    .AsEnumerable()
                    .Where(VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance)
                    .GroupBy(app => app.ID)
                    .Select(g => g.First())
                    .OrderByDescending(app => app.ApplicationDate)
                    .ThenBy(app => app.FullApplicationNumber)
                    .ToList();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<InvitationItem> AvailableIssuingInvitationItems
        {
            get
            {
                var person = Passport?.Person;
                if (person == null)
                    return new List<InvitationItem>();

                return ObjectSpaceHelper.Get(this)?.GetObject(person)?.InvitationItems?.ToList()
                    ?? new List<InvitationItem>();
            }
        }

        [RuleFromBoolProperty("Visa_PersonIsValid", DefaultContexts.Save, "Issuing application must include the visa holder (same person as Passport).")]
        [Browsable(false)]
        public bool IsPersonValid
        {
            get
            {
                var application = VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(this);
                if (application == null || Passport?.Person == null)
                    return true;

                return ApplicationRosterHelper.IsPersonOnApplication(application, Passport.Person);
            }
        }

        [RuleFromBoolProperty("Visa_IssuingApplicationTypeAllowed", DefaultContexts.Save, "Issuing application must be a type/profile that can issue a visa or invitation.")]
        [Browsable(false)]
        public bool IsIssuingApplicationTypeAllowed
        {
            get
            {
                var application = VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(this);
                if (application == null)
                    return true;

                return VisaIssuingApplicationProfileInstanceHelper.IsEligibleIssuingApplicationProfileInstance(application);
            }
        }

        [RuleFromBoolProperty("Visa_InvitationPersonIsValid", DefaultContexts.Save, "The owner of the Visa is not included in the selected Invitation.")]
        [Browsable(false)]
        public bool IsInvitationPersonValid
        {
            get
            {
                if (IssuingInvitationItem == null || Passport?.Person == null) return true;
                return IssuingInvitationItem.Person != null && IssuingInvitationItem.Person.ID == Passport.Person.ID;
            }
        }

        [RuleFromBoolProperty("Visa_IssuingApplicationProfileInstanceRequired", DefaultContexts.Save,
            "Create the visa from Application Profile Instance → Issued records (New issued visa), not from Passport.")]
        [Browsable(false)]
        public bool IsIssuingApplicationProfileInstanceRequired =>
            VisaIssuingOriginPolicy.HasRequiredIssuingApplicationProfileInstance(this);

        [RuleFromBoolProperty("Visa_IssuingApplicationProfileInstanceSingleUse", DefaultContexts.Save, "This person already has a visa issued by this application.")]
        [Browsable(false)]
        public bool IsIssuingApplicationProfileInstanceSingleUse
        {
            get
            {
                if (IssuingApplicationProfileInstance == null)
                    return true;

                // Invitation-based cases: one visa per IssuingInvitationItem, not one visa per person.
                if (IssuingInvitationItem != null
                    || VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(
                        IssuingApplicationProfileInstance))
                {
                    return true;
                }

                var personId = Passport?.Person?.ID ?? Guid.Empty;
                if (personId == Guid.Empty)
                    return true;

                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                    return true;

                var appId = IssuingApplicationProfileInstance.ID;
                var currentId = ID;
                return !objectSpace.GetObjectsQuery<Visa>()
                    .Any(v => v.ID != currentId
                        && v.IssuingApplicationProfileInstance != null
                        && v.IssuingApplicationProfileInstance.ID == appId
                        && v.Passport != null
                        && v.Passport.Person != null
                        && v.Passport.Person.ID == personId);
            }
        }

        [RuleFromBoolProperty("Visa_IssuingInvitationItemSingleUse", DefaultContexts.Save, "This issuing invitation item is already linked to another visa.")]
        [Browsable(false)]
        public bool IsIssuingInvitationItemSingleUse
        {
            get
            {
                if (IssuingInvitationItem == null) return true;
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null) return true;
                var itemId = IssuingInvitationItem.ID;
                var currentId = ID;
                return !objectSpace.GetObjectsQuery<Visa>()
                    .Any(v => v.ID != currentId
                        && v.IssuingInvitationItem != null
                        && v.IssuingInvitationItem.ID == itemId);
            }
        }

        [RuleFromBoolProperty("Visa_IssuingChronologyValid", DefaultContexts.Save, "Visa Issue Date must be later than the issuing ApplicationProfileInstance Date (and Invitation Issued Date when an invitation is linked).")]
        [Browsable(false)]
        public bool IsIssuingChronologyValid
        {
            get
            {
                if (IssueDate == default)
                    return true;

                var issuingApplication = VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(this);
                if (issuingApplication == null && IssuingInvitationItem == null)
                    return true;

                if (IssuingInvitationItem?.Invitation != null)
                {
                    if (!(IssueDate.Date > IssuingInvitationItem.Invitation.IssuedDate.Date))
                        return false;
                    if (issuingApplication != null
                        && !(IssuingInvitationItem.Invitation.IssuedDate.Date > issuingApplication.ApplicationDate.Date))
                        return false;
                    return true;
                }

                if (issuingApplication != null)
                    return IssueDate.Date > issuingApplication.ApplicationDate.Date;

                return true;
            }
        }

        [RuleFromBoolProperty("Visa_InvitationOnlyWhenCanIssueInvitation", DefaultContexts.Save, "Issuing invitation item can only be set when the issuing application type can issue an invitation.")]
        [Browsable(false)]
        public bool IsInvitationLinkConsistent
        {
            get
            {
                if (IssuingInvitationItem == null)
                    return true;

                var issuingApplication = VisaIssuingApplicationProfileInstanceHelper.GetEffectiveIssuingApplicationProfileInstance(this);
                if (issuingApplication == null)
                    return true;

                return VisaIssuingApplicationProfileInstanceHelper.CanIssueInvitationForApplication(issuingApplication);
            }
        }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        public virtual string Notes { get; set; }

        [Aggregated]
        [InverseProperty(nameof(VisaImage.Visa))]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public virtual IList<VisaImage> Images { get; set; } = new ObservableCollection<VisaImage>();

        [Aggregated]
        [InverseProperty(nameof(VisaDocument.Visa))]
        public virtual IList<VisaDocument> Documents { get; set; } = new ObservableCollection<VisaDocument>();

        /// <summary>Skip-navigation M2M with <see cref="ApplicationProfileInstance"/> (input linked visas). Not aggregated. Issued-from is <see cref="IssuingApplicationProfileInstance"/> / <see cref="ApplicationProfileInstance.IssuedVisas"/>.</summary>
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(false)]
        public virtual IList<ApplicationProfileInstance> ApplicationProfileInstances { get; set; } = new ObservableCollection<ApplicationProfileInstance>();

        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        [ModelDefault("AllowEdit", "False")]
        [ModelDefault("Caption", "Registration State")]
        [VisibleInListView(false)]
        public virtual string RegistrationState { get; set; }

        public int DaysRemaining => ExpirationLogicHelper.CalculateDaysRemaining(ExpirationDate, IsCancelled);

        /// <summary>
        /// Validity state from flags and expiration (see <see cref="VisaValidityStateHelper"/>).
        /// Expiring window uses per-BO <see cref="ExpirationAlertRule"/> (fallback: <see cref="ExpirationAlertRule.DefaultExpiringSoonDays"/>).
        /// </summary>
        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(true)]
        [VisibleInListView(true)]
        public VisaValidityState State =>
            VisaValidityStateHelper.Resolve(this, ObjectSpaceHelper.Get(this));

        /// <summary>
        /// Derived from linked completed Cancellation instances. Not stored.
        /// Officer auto-link requires this false (visa must still be valid: started, not cancelled/changed, not expired).
        /// </summary>
        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [ExcludeFromOptionalDetailFields]
        public bool IsCancelled => IssuedDocumentLifecycle.IsCancelled(this);

        /// <summary>Derived from linked completed Change instances. Not stored.</summary>
        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [ExcludeFromOptionalDetailFields]
        public bool IsChanged => IssuedDocumentLifecycle.IsChanged(this);

        /// <summary>Optional; editable on detail view (gear or when true).</summary>
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        public virtual bool IsExtended { get; set; }

        [ModelDefault("Caption", "Extension Required")]
        [ToolTip("Uncheck if no extension is needed — e.g. the employee is leaving or the contract is ending.")]
        public virtual bool ExtensionRequired { get; set; } = true;


        [NotMapped]
        [Browsable(false)]
        public int StateSeverityLevel
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                return objectSpace != null
                    ? (int)VisaStateEvaluator.Evaluate(
                        this,
                        StateEvaluationSettings.FromObjectSpace(objectSpace)
                      ).Severity
                    : 0;
            }
        }

        public override void OnSaving()
        {
            BorderZoneSelectionHelper.ApplyDefaultIfEmpty(this);
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
            StateChangeTrackingHelper.TrackOnSave(this);
        }

        public override void OnCreated()
        {
            base.OnCreated();
            ExtensionRequired = true;
            BorderZoneSelectionHelper.ApplyDefaultIfEmpty(this);
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null)
            {
                VisaType = objectSpace.GetObjectsQuery<VisaType>().FirstOrDefault(v => v.IsDefault);
                VisaCategory = objectSpace.GetObjectsQuery<VisaCategory>().FirstOrDefault(vc => vc.IsDefault);
                VisaIssuedPlace = objectSpace.GetObjectsQuery<VisaIssuedPlace>().FirstOrDefault(vip => vip.IsDefault);
            }

            if (IssuingApplicationProfileInstance != null)
                VisaIssuingLinkPathAMatcher.TryApplyOnce(this);
        }
    }
}