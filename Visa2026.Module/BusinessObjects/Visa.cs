using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
    [NavigationItem("Lookup/Visa")]
    [DefaultProperty(nameof(VisaNumber))]
    [RuleCriteria("Visa_ExpirationDate_GreaterThan_StartDate", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
    [Appearance("VisaStateInfo", Priority = 100, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel = 1", Context = "ListView", BackColor = "LightSkyBlue")]
    [Appearance("VisaStateWarning", Priority = 200, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel = 2", Context = "ListView", BackColor = "LightSalmon")]
    [Appearance("VisaStateCritical", Priority = 300, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted = false And StateSeverityLevel >= 3", Context = "ListView", BackColor = "LightCoral")]
    public class Visa : SingleActiveBaseObject<Person, Visa>, IExpirationLogic, ISoftDelete
    {
        private static bool IsSelectable(Visa v) =>
            v != null && !v.IsDeleted && !v.IsCancelled && v.StartDate != default;

        private static bool IsEffectiveOn(Visa v, DateTime asOfDate) =>
            IsSelectable(v) && v.StartDate.Date <= asOfDate.Date;

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

                if (ObjectSpace == null)
                {
                    return true;
                }

                var normalized = VisaNumber.Trim().ToUpperInvariant();
                var currentId = ID;

                return !ObjectSpace.GetObjectsQuery<Visa>()
                    .Where(v => !v.IsDeleted && v.ID != currentId && v.VisaNumber != null)
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

                if (ObjectSpace != null)
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

        [MaxLength(500)]
        [VisibleInListView(false)]
        [EditorAlias(CommaSeparatedMultiSelectEditorAliases.BorderZone)]
        [CommaSeparatedMultiSelect(
            CatalogEntityType = typeof(BorderZoneName),
            NoneValue = "")]
        public virtual string BorderZoneLocation { get; set; }

        [Browsable(false)]
        [XafDisplayName("Border Zone Location (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string BorderZoneLocation_NameTm =>
            string.IsNullOrWhiteSpace(BorderZoneLocation)
                ? string.Empty
                : BorderZoneLocation.Trim();

        [VisibleInListView(false)]
        public virtual bool HasInvitation { get; set; }
        [Appearance("InvitationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!HasInvitation", Context = "DetailView")]
        [RuleRequiredField(TargetCriteria = "HasInvitation")]
        [VisibleInListView(false)]
        public virtual InvitationItem InvitationItem { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        public virtual Passport Passport { get; set; }

        /// <summary>
        /// When true, this visa predates system workflow or has no issuing application on file — <see cref="IssuingApplicationItem"/> is optional.
        /// When false (default), <see cref="IssuingApplicationItem"/> is required and validated as usual.
        /// </summary>
        [ImmediatePostData]
        [ModelDefault("Caption", "Historical import (no issuing application)")]
        [ToolTip("Turn on when the visa was issued before this system or application data is unavailable. Issuing Application Item becomes optional.")]
        [VisibleInListView(false)]
        public virtual bool HistoricalImport { get; set; }

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
                if (HistoricalImport || ObjectSpace == null)
                {
                    return new List<ApplicationItem>();
                }

                var person = Passport?.Person;
                if (person == null)
                {
                    return new List<ApplicationItem>();
                }

                var allowedNames = VisaIssuingApplicationTypes.AllowedApplicationTypeNames.ToArray();
                return ObjectSpace.GetObjectsQuery<ApplicationItem>()
                    .Where(ai => !ai.IsDeleted)
                    .Where(ai => ai.Person != null && ai.Person.ID == person.ID)
                    .Where(ai => ai.Application != null && ai.Application.ApplicationType != null && allowedNames.Contains(ai.Application.ApplicationType.Name))
                    .OrderBy(ai => ai.ApplicationItemName)
                    .ToList();
            }
        }

        /// <summary>
        /// Required unless <see cref="HistoricalImport"/> is true. The application line for the visa holder under which this visa was issued.
        /// Must match <see cref="Passport.Person"/> and allowed application types when set.
        /// </summary>
        [DataSourceProperty(nameof(AvailableIssuingApplicationItems))]
        [RuleRequiredField(TargetCriteria = "!HistoricalImport")]
        [Appearance("IssuingApplicationItemHiddenWhenHistorical", Visibility = ViewItemVisibility.Hide, Criteria = "HistoricalImport", Context = "DetailView")]
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
        public virtual IList<InvitationItem> AvailableInvitationItems
        {
            get => new List<InvitationItem>(); 
        }

        [RuleFromBoolProperty("Visa_PersonIsValid", DefaultContexts.Save, "Issuing Application Item must be the application line for the visa holder (same person as Passport).")]
        [Browsable(false)]
        public bool IsPersonValid
        {
            get
            {
                if (HistoricalImport && IssuingApplicationItem == null) return true;
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
                if (HistoricalImport && IssuingApplicationItem == null) return true;
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
                if (!HasInvitation || InvitationItem == null || Passport?.Person == null) return true;
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

        public override void OnSaving()
        {
            // Visa is date-effective. Future visas must not become "active/current" immediately.
            // SingleActiveBaseObject calls UpdateActiveState() on save; we override it to be date-driven.
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
            StateChangeTrackingHelper.TrackOnSave(this);
        }

        protected override void UpdateActiveState()
        {
            var parent = GetParent();
            if (parent == null)
                return;

            // We intentionally do not use the base "single active" behavior for Visa.
            // Current/next are determined by effective dates.
            var asOf = DateTime.Today;

            var siblings = GetSiblings(parent) ?? new List<Visa>();
            var selectable = siblings.Where(IsSelectable).ToList();

            var currentForPerson = selectable
                .Where(v => IsEffectiveOn(v, asOf))
                .OrderByDescending(v => v.StartDate.Date)
                .ThenByDescending(v => v.IssueDate.Date)
                .FirstOrDefault();

            // Normalize IsActive flags to match the effective current visa.
            foreach (var v in selectable)
                v.IsActive = ReferenceEquals(v, currentForPerson);

            parent.CurrentVisa = currentForPerson;

            // Also keep per-passport CurrentVisa consistent (same date-effective rule).
            if (parent.Passports != null)
            {
                foreach (var passport in parent.Passports.Where(p => p != null && !p.IsDeleted))
                {
                    var pSelectable = passport.Visas?
                        .Where(IsSelectable)
                        .ToList() ?? new List<Visa>();

                    var currentForPassport = pSelectable
                        .Where(v => IsEffectiveOn(v, asOf))
                        .OrderByDescending(v => v.StartDate.Date)
                        .ThenByDescending(v => v.IssueDate.Date)
                        .FirstOrDefault();

                    passport.CurrentVisa = currentForPassport;
                }
            }
        }

        public override Person GetParent()
        {
            return Passport?.Person;
        }
        public override IList<Visa> GetSiblings(Person parent)
        {
            return parent?.Passports?.SelectMany(p => p.Visas).ToList();
        }

        public override void SetParentActiveItem(Person parent, Visa item)
        {
            if (parent != null) parent.CurrentVisa = item;
        }

        public override bool IsParentActiveItem(Person parent, Visa item)
        {
            return parent?.CurrentVisa == item;
        }

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

        public ExpirationState ExpirationState
        {
            get
            {
                return ExpirationLogicHelper.CalculateExpirationState(this, StartDate, ObjectSpace);
            }
        }

        [Browsable(false)]
        [NotMapped]
        protected override DateTime? ChronologicalSortDate => this.IssueDate;

		[VisibleInListView(false)]
		public virtual bool IsCancelled { get; set; }

		[VisibleInListView(false)]
		public virtual bool IsChanged { get; set; }

        [VisibleInListView(false)]
        public virtual bool IsExtended { get; set; }

        [ModelDefault("Caption", "Extension Required")]
        [ToolTip("Uncheck if no extension is needed — e.g. the employee is leaving or the contract is ending.")]
        public virtual bool ExtensionRequired { get; set; } = true;

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        [NotMapped]
        [Browsable(false)]
        public int StateSeverityLevel =>
            ObjectSpace != null
                ? (int)VisaStateEvaluator.Evaluate(
                    this,
                    StateEvaluationSettings.FromSystemSettings(SystemSettings.TryGetInstance(ObjectSpace))
                  ).Severity
                : 0;

        public override void OnCreated()
        {
            base.OnCreated();
            ExtensionRequired = true;
            HistoricalImport = false;
            if (ObjectSpace != null)
            {
                VisaType = ObjectSpace.GetObjectsQuery<VisaType>().FirstOrDefault(v => v.IsDefault);
                VisaCategory = ObjectSpace.GetObjectsQuery<VisaCategory>().FirstOrDefault(vc => vc.IsDefault);
                VisaIssuedPlace = ObjectSpace.GetObjectsQuery<VisaIssuedPlace>().FirstOrDefault(vip => vip.IsDefault);
                // Defaulting IssuingApplicationItem, Passport, InvitationItem is complex and usually handled by business logic or user input.
                // For now, leaving them null as they are RuleRequiredField and will be set explicitly.
                // IssuingApplicationItem = ObjectSpace.GetObjectsQuery<ApplicationItem>().FirstOrDefault(); // Example
                // Passport = ObjectSpace.GetObjectsQuery<Passport>().FirstOrDefault(); // Example
            }
        }
    }
}