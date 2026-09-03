using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using Visa2026.Module.Services;
using Visa2026.Module.Services.StateEvaluation;
using Visa2026.Module.Services.StateEvaluation.Evaluators;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using Visa2026.Module.Editors;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("WorkPermit")]
    [DefaultProperty(nameof(WorkPermitItemName))]
    [RuleCriteria("WorkPermitItem_DateRange", DefaultContexts.Save, "ExpirationDate > StartDate", "Expiration Date must be later than Start Date.")]
    [Appearance("WPStateInfo", Priority = 100, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "StateSeverityLevel = 1", Context = "ListView", BackColor = "LightSkyBlue")]
    [Appearance("WPStateWarning", Priority = 200, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "StateSeverityLevel = 2", Context = "ListView", BackColor = "LightSalmon")]
    [Appearance("WPStateCritical", Priority = 300, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "StateSeverityLevel >= 3", Context = "ListView", BackColor = "LightCoral")]
    [Appearance("WorkPermitItem_InputApplicationProfileInstancesHiddenWhenIssued", Priority = 50,
        AppearanceItemType = "ViewItem", TargetItems = "ApplicationProfileInstances",
        Criteria = "ApplicationProfileInstance is not null", Context = "DetailView",
        Visibility = ViewItemVisibility.Hide)]
    [SupportsOptionalDetailFields]
    public class WorkPermitItem : BaseObject, IExpirationLogic, IOptionalDetailFields
    {
        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceCriteria("IsEmployee = true")]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (ObjectSpaceHelper.Get(this) != null)
                    {
                        ApplyCurrentFieldsFromSelectedPerson();
                    }
                }
            }
        }
        private Person person;

        /// <summary>
        /// Copies <see cref="Person"/>'s current document links into this item when <see cref="Person"/> changes.
        /// </summary>
        private void ApplyCurrentFieldsFromSelectedPerson()
        {
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace == null)
                return;

            if (person == null)
            {
                Passport = null;
                CurrentPositionHistory = null;
                return;
            }

            var p = objectSpace.GetObject(person);
            Passport = PersonCurrentItems.GetCurrentPassport(p);
            CurrentPositionHistory = PersonCurrentItems.GetCurrentPositionHistory(p);

            var visa = PersonCurrentItems.GetCurrentVisa(p);
            if (visa != null && visa.ExpirationDate.HasValue && visa.ExpirationDate.Value.Date >= DateTime.Today)
            {
                if (StartDate == default)
                    StartDate = visa.StartDate;
                if (ExpirationDate == default || ExpirationDate == null)
                    ExpirationDate = visa.ExpirationDate.Value;
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<Passport> AvailablePassports
        {
            get
            {
                if (person == null) return new List<Passport>();
                return ObjectSpaceHelper.Get(this)?.GetObject(person)?.Passports?.ToList() ?? new List<Passport>();
            }
        }

        [NotMapped]
        [Browsable(false)]
        public IList<EmployeePositionHistory> AvailablePositionHistories
        {
            get
            {
                if (person == null) return new List<EmployeePositionHistory>();
                return ObjectSpaceHelper.Get(this)?.GetObject(person)?.PositionHistory?.ToList() ?? new List<EmployeePositionHistory>();
            }
        }

        [RuleRequiredField]
        [DataSourceProperty(nameof(AvailablePassports))]
        public virtual Passport Passport { get; set; }

        [RuleRequiredField]
        [DataSourceProperty(nameof(AvailablePositionHistories))]
        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime StartDate { get; set; }

        [RuleRequiredField]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
        public virtual DateTime ExpirationDate { get; set; }

        [RuleRequiredField]
        public virtual string WorkPermitNumber { get; set; }

        

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        [RuleRequiredField]
        [Appearance("WorkPermitItem_ASNumberReadOnlyFromInstance", Enabled = false, Context = "DetailView",
            Criteria = "WorkPermit is not null And WorkPermit.ApplicationProfileInstance is not null And WorkPermit.ApplicationProfileInstance.ProcessNumber is not null And WorkPermit.ApplicationProfileInstance.ProcessNumber != ''")]
        public virtual string ASNumber { get; set; }

        public virtual WorkPermit WorkPermit { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [MaxLength(500)]
        [VisibleInListView(true)]
        [EditorAlias(CommaSeparatedMultiSelectEditorAliases.WorkPermittedLocation)]
        [CommaSeparatedMultiSelect(
            CatalogEntityType = typeof(WorkPermittedLocationName),
            NoneValue = "")]
        public virtual string WorkPermittedLocations { get; set; }

        [RuleFromBoolProperty("WorkPermitItem_EmployeeIsValid", DefaultContexts.Save, "The selected employee is not part of the parent application.")]
        [Browsable(false)]
        public bool IsEmployeeValid
        {
            get
            {
                if (Person == null || WorkPermit?.ApplicationProfileInstance == null) return true;
                return ApplicationRosterHelper.IsPersonOnApplication(WorkPermit.ApplicationProfileInstance, Person);
            }
        }

        [RuleFromBoolProperty("WorkPermitItem_PersonUniqueInWorkPermit", DefaultContexts.Save, "This person already has a Work Permit Item in the same Work Permit.")]
        [Browsable(false)]
        public bool IsPersonUniqueInWorkPermit
        {
            get
            {
                if (Person == null || WorkPermit == null) return true;
                // Ignore soft-deleted rows and rows marked for deletion in the current ObjectSpace
                // (e.g., when the user removes an item and saves the parent in the same transaction).
                var os = ObjectSpaceHelper.Get(this);
                return !WorkPermit.WorkPermitItems.Any(wpi =>
                    wpi.ID != ID
                    && wpi.Person?.ID == Person.ID
                   
                    && (os == null || !os.IsDeletedObject(wpi)));
            }
        }

        DateTime? IExpirationLogic.ExpirationDate => ExpirationDate;

        public int DaysRemaining => ExpirationLogicHelper.CalculateDaysRemaining(ExpirationDate, IsCancelled);

        public string WorkPermitItemName => $"{Person?.FullName} - {WorkPermitNumber}";

        #region Report [NotMapped] properties — WorkPermitListReport fields
        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_FullName => Person?.FullName;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_DateOfBirthText => Person?.DateOfBirth is DateTime dob ? dob.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityCode => Person?.Nationality?.Code;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Education_LevelTm => PersonCurrentItems.GetCurrentEducation(Person)?.EducationLevel?.NameTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Number => Passport?.PassportNumber;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_ExpirationDateText => Passport?.ExpirationDate is DateTime exp ? exp.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_NameTm => CurrentPositionHistory?.Position?.NameTm;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_FullAddress => PersonCurrentItems.GetCurrentAddressOfResidence(Person)?.FullAddress;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string WP_StartDateText => StartDate.ToString("dd.MM.yyyy");

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string WP_ExpirationDateText => ExpirationDate.ToString("dd.MM.yyyy");

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_Number => PersonCurrentItems.GetCurrentVisa(Person)?.VisaNumber;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_StartDateText => PersonCurrentItems.GetCurrentVisa(Person)?.StartDate is DateTime vs ? vs.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_ExpirationDateText => PersonCurrentItems.GetCurrentVisa(Person)?.ExpirationDate is DateTime ve ? ve.ToString("dd.MM.yyyy") : string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string Company_Name =>
            WorkPermit?.ApplicationProfileInstance?.Application_Company_Name ?? string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_PositionTm => WorkPermit?.ApplicationProfileInstance?.Application_CompanyHead_PositionTm ?? string.Empty;

        [NotMapped, VisibleInDetailView(false), VisibleInListView(false)]
        public string CompanyHead_FullName => WorkPermit?.ApplicationProfileInstance?.Application_CompanyHead_FullName ?? string.Empty;
        #endregion

        public override void OnCreated()
        {
            base.OnCreated();
        }

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

        /// <summary>
        /// Derived from linked completed Cancellation instances. Not stored.
        /// Officer auto-link requires this false (work-permit item must still be valid: not cancelled/changed, not expired).
        /// </summary>
        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [ExcludeFromOptionalDetailFields]
        public bool IsCancelled => IssuedDocumentLifecycle.IsCancelled(this);

        /// <summary>Derived from linked completed Change instances. Not stored. Cancelled wins.</summary>
        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInDetailView(false)]
        [ExcludeFromOptionalDetailFields]
        public bool IsChanged => IssuedDocumentLifecycle.IsChanged(this);

        [NotMapped]
        [Browsable(false)]
        public int StateSeverityLevel
        {
            get
            {
                var objectSpace = ObjectSpaceHelper.Get(this);
                return objectSpace != null
                    ? (int)WorkPermitItemStateEvaluator.Evaluate(
                        this,
                        StateEvaluationSettings.FromObjectSpace(objectSpace)
                      ).Severity
                    : 0;
            }
        }

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            Visa2026.Module.Localization.VisaUiMessages.Get("WorkPermitDocumentCopies.List.ColumnLink");

        /// <summary>Skip-navigation M2M with <see cref="ApplicationProfileInstance"/> (input linked items). Distinct from parent <see cref="WorkPermit.ApplicationProfileInstance"/> issued-from FK.</summary>
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(false)]
        public virtual IList<ApplicationProfileInstance> ApplicationProfileInstances { get; set; } = new ObservableCollection<ApplicationProfileInstance>();

        /// <summary>
        /// ApplicationProfileInstance linked on the parent <see cref="WorkPermit"/> (if any). Read-only ListView/Detail convenience.
        /// </summary>
        [NotMapped]
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(true)]
        [VisibleInDetailView(true)]
        [VisibleInLookupListView(false)]
        [XafDisplayName("Application Profile instance")]
        [ToolTip("Issuing profile instance from the parent work permit header (WorkPermit.ApplicationProfileInstance).")]
        public ApplicationProfileInstance ApplicationProfileInstance => WorkPermit?.ApplicationProfileInstance;
    }
}