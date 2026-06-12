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
    [NavigationItem(false)]
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

        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
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
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
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

        /// <summary>Optional; detail view (gear). Linked invitation line for this visa holder.</summary>
        [DataSourceProperty(nameof(AvailableInvitationItems))]
        [VisibleInListView(false)]
        public virtual InvitationItem InvitationItem { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual Passport Passport { get; set; }

        /// <summary>
        /// Candidate rows for <see cref="IssuingApplicationItem"/> (same person as <see cref="Passport"/> and allowed issuing application types).
        /// Used by <see cref="DataSourcePropertyAttribute"/> — avoids unreliable criteria reflection on Blazor lookup editors.
        /// </summary>
        [NotMapped]
        [Browsable(false)]
        public IList<ApplicationItem> AvailableIssuingApplicationItems
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                if (objectSpace == null)
                {
                    return new List<ApplicationItem>();
                }

                var person = Passport?.Person;
                if (person == null)
                {
                    return new List<ApplicationItem>();
                }

                var allowedNames = VisaIssuingApplicationTypes.AllowedApplicationTypeNames.ToArray();
                return objectSpace.GetObjectsQuery<ApplicationItem>()
                    .Where(ai => ai.Person != null && ai.Person.ID == person.ID)
                    .Where(ai => ai.Application != null && ai.Application.ApplicationType != null && allowedNames.Contains(ai.Application.ApplicationType.Name))
                    .OrderBy(ai => ai.ApplicationItemName)
                    .ToList();
            }
        }

        /// <summary>
        /// Optional; detail view (gear). Application line for the visa holder under which this visa was issued.
        /// Must match <see cref="Passport.Person"/> and allowed application types when set.
        /// </summary>
        [DataSourceProperty(nameof(AvailableIssuingApplicationItems))]
        [VisibleInListView(false)]
        [XafDisplayName("Issuing Application Item")]
        public virtual ApplicationItem IssuingApplicationItem { get; set; }

        /// <summary>
        /// Application items that reference this visa as <see cref="ApplicationItem.CurrentVisa"/> (target visa), e.g. extensions or cancellations — distinct from <see cref="IssuingApplicationItem"/>.
        /// Hidden from Visa Detail View; linkage remains in the model for reports and state logic.
        /// </summary>
        [InverseProperty("CurrentVisa")]
        [VisibleInDetailView(false)]
        [XafDisplayName("Associated Application Items")]
        [ToolTip("List of applications where this visa is/was used as the current visa.")]
        public virtual IList<ApplicationItem> AssociatedApplicationItems { get; set; } = new ObservableCollection<ApplicationItem>();

        [NotMapped]
        [Browsable(false)]
        public IList<InvitationItem> AvailableInvitationItems
        {
            get
            {
                var person = Passport?.Person;
                if (person == null)
                {
                    return new List<InvitationItem>();
                }

                return ObjectSpaceHelper.Get(this)?.GetObject(person)?.InvitationItems?.ToList()
                    ?? new List<InvitationItem>();
            }
        }

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

        [RuleFromBoolProperty("Visa_IssuingApplicationTypeAllowed", DefaultContexts.Save, "Issuing Application Item must belong to an application type that can issue a new visa (invitation, extension, exit visa, passport change, etc.).")]
        [Browsable(false)]
        public bool IsIssuingApplicationTypeAllowed
        {
            get
            {
                if (IssuingApplicationItem == null) return true;
                var applicationType = IssuingApplicationItem.Application?.ApplicationType;
                if (applicationType == null) return false;
                return VisaIssuingApplicationTypes.IsAllowed(applicationType);
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

        public int DaysRemaining
        {
            get
            {
                if (!ExpirationDate.HasValue)
                {
                    return 0;
                }
                return (ExpirationDate.Value.Date - DateTime.Today).Days;
            }
        }

        /// <summary>
        /// Validity state from flags and expiration (see <see cref="VisaValidityStateHelper"/>).
        /// Expiring window uses per-BO <see cref="ExpirationAlertRule"/> (fallback: <see cref="SystemSettings.DefaultExpiringSoonDays"/>).
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
        }
    }
}