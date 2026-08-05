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
        /// <see cref="LegacyPersonInApplicationOid"/> (legacy PIA FK also named ProcessNumber in VISA2014).
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
        /// Legacy <c>Visa.ProcessNumber</c> PersonInApplication Oid (import lineage). Domain line link is <see cref="IssuingApplicationItem"/>.
        /// </summary>
        [XafDisplayName("Legacy PIA Oid")]
        [ModelDefault("AllowEdit", "False")]
        [ExcludeFromOptionalDetailFields]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        public virtual Guid? LegacyPersonInApplicationOid { get; set; }

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
        /// Inverse: <see cref="InvitationItem.IssuedVisa"/>.
        /// </summary>
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [DataSourceProperty(nameof(AvailableInvitationItems))]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [XafDisplayName("Invitation Item")]
        public virtual InvitationItem InvitationItem { get; set; }

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
                VisaIssuingLinkPathAMatcher.TryApplyOnce(this);
            }
        }

        private Passport passport;

        /// <summary>
        /// Optional; always visible; read-only (Path A / Path B set on create or import).
        /// Inverse: <see cref="ApplicationItem.IssuedVisa"/>. Distinct from application lines that
        /// reference this visa as <see cref="ApplicationItem.CurrentVisa"/>.
        /// </summary>
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [DataSourceProperty(nameof(AvailableIssuingApplicationItems))]
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        [XafDisplayName("Issuing Application Item")]
        public virtual ApplicationItem IssuingApplicationItem { get; set; }

        /// <summary>Prevents Path A matcher from running more than once on a new Visa instance.</summary>
        [NotMapped]
        [Browsable(false)]
        public bool PathAIssuingLinksApplied { get; set; }

        [NotMapped]
        [Browsable(false)]
        public IList<ApplicationItem> AvailableIssuingApplicationItems
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                var person = Passport?.Person;
                if (objectSpace == null || person == null)
                    return new List<ApplicationItem>();

                return objectSpace.GetObjectsQuery<ApplicationItem>()
                    .Where(ai => ai.Person != null && ai.Person.ID == person.ID)
                    .Where(ai => ai.Application != null
                        && ai.Application.ApplicationType != null
                        && (ai.Application.ApplicationType.CanIssueVisa
                            || ai.Application.ApplicationType.CanIssueInvitation))
                    .OrderByDescending(ai => ai.Application!.ApplicationDate)
                    .ThenBy(ai => ai.ApplicationItemName)
                    .ToList();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<InvitationItem> AvailableInvitationItems
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

        /// <summary>
        /// Application items that reference this visa as <see cref="ApplicationItem.CurrentVisa"/> (target visa), e.g. extensions or cancellations — distinct from <see cref="IssuingApplicationItem"/>.
        /// Hidden from Visa Detail View; linkage remains in the model for reports and state logic.
        /// </summary>
        [InverseProperty("CurrentVisa")]
        [VisibleInDetailView(false)]
        [XafDisplayName("Associated Application Items")]
        [ToolTip("List of applications where this visa is/was used as the current visa.")]
        public virtual IList<ApplicationItem> AssociatedApplicationItems { get; set; } = new ObservableCollection<ApplicationItem>();

        [RuleFromBoolProperty("Visa_PersonIsValid", DefaultContexts.Save, "Issuing Application Item must be the application line for the visa holder (same person as Passport).")]
        [Browsable(false)]
        public bool IsPersonValid
        {
            get
            {
                if (IssuingApplicationItem == null || Passport?.Person == null) return true;
                return IssuingApplicationItem.Person != null && IssuingApplicationItem.Person.ID == Passport.Person.ID;
            }
        }

        [RuleFromBoolProperty("Visa_IssuingApplicationTypeAllowed", DefaultContexts.Save, "Issuing Application Item must belong to an application type that can issue a visa or invitation.")]
        [Browsable(false)]
        public bool IsIssuingApplicationTypeAllowed
        {
            get
            {
                if (IssuingApplicationItem == null) return true;
                var applicationType = IssuingApplicationItem.Application?.ApplicationType;
                return ApplicationTypeCapabilities.CanBeIssuingApplicationForVisa(applicationType);
            }
        }

        [RuleFromBoolProperty("Visa_InvitationPersonIsValid", DefaultContexts.Save, "The owner of the Visa is not included in the selected Invitation.")]
        [Browsable(false)]
        public bool IsInvitationPersonValid
        {
            get
            {
                if (InvitationItem == null || Passport?.Person == null) return true;
                return InvitationItem.Person != null && InvitationItem.Person.ID == Passport.Person.ID;
            }
        }

        [RuleFromBoolProperty("Visa_IssuingApplicationItemSingleUse", DefaultContexts.Save, "This Issuing Application Item is already linked to another visa.")]
        [Browsable(false)]
        public bool IsIssuingApplicationItemSingleUse
        {
            get
            {
                if (IssuingApplicationItem == null) return true;
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null) return true;
                var itemId = IssuingApplicationItem.ID;
                var currentId = ID;
                return !objectSpace.GetObjectsQuery<Visa>()
                    .Any(v => v.ID != currentId
                        && v.IssuingApplicationItem != null
                        && v.IssuingApplicationItem.ID == itemId);
            }
        }

        [RuleFromBoolProperty("Visa_InvitationItemSingleUse", DefaultContexts.Save, "This Invitation Item is already linked to another visa.")]
        [Browsable(false)]
        public bool IsInvitationItemSingleUse
        {
            get
            {
                if (InvitationItem == null) return true;
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null) return true;
                var itemId = InvitationItem.ID;
                var currentId = ID;
                return !objectSpace.GetObjectsQuery<Visa>()
                    .Any(v => v.ID != currentId
                        && v.InvitationItem != null
                        && v.InvitationItem.ID == itemId);
            }
        }

        [RuleFromBoolProperty("Visa_IssuingChronologyValid", DefaultContexts.Save, "Visa Issue Date must be later than the issuing Application Date (and Invitation Issued Date when an invitation is linked).")]
        [Browsable(false)]
        public bool IsIssuingChronologyValid
        {
            get
            {
                if (IssueDate == default) return true;
                if (IssuingApplicationItem?.Application == null && InvitationItem == null) return true;

                if (InvitationItem?.Invitation != null)
                {
                    if (!(IssueDate.Date > InvitationItem.Invitation.IssuedDate.Date))
                        return false;
                    if (IssuingApplicationItem?.Application != null
                        && !(InvitationItem.Invitation.IssuedDate.Date > IssuingApplicationItem.Application.ApplicationDate.Date))
                        return false;
                    return true;
                }

                if (IssuingApplicationItem?.Application != null)
                    return IssueDate.Date > IssuingApplicationItem.Application.ApplicationDate.Date;

                return true;
            }
        }

        [RuleFromBoolProperty("Visa_InvitationOnlyWhenCanIssueInvitation", DefaultContexts.Save, "Invitation Item can only be set when the issuing application type can issue an invitation.")]
        [Browsable(false)]
        public bool IsInvitationLinkConsistent
        {
            get
            {
                if (InvitationItem == null) return true;
                if (IssuingApplicationItem == null) return true;
                return ApplicationTypeCapabilities.CanIssueInvitation(
                    IssuingApplicationItem.Application?.ApplicationType);
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

        /// <summary>Optional; editable on detail view (gear or when true).</summary>
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        public virtual bool IsCancelled { get; set; }

        /// <summary>Optional; editable on detail view (gear or when true).</summary>
        [VisibleInListView(false)]
        [VisibleInDetailView(true)]
        public virtual bool IsChanged { get; set; }

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

            VisaIssuingLinkPathAMatcher.TryApplyOnce(this);
        }
    }
}