using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects
{
    // Abstract base class to enforce standard structure as per LookupBusinessObjects.md
    public abstract class LookupBase : BaseObject
    {
        /// <summary>Legacy English/seed key. Do not edit in UI — use <see cref="NameTm"/> for Turkmen titles.</summary>
        [Obsolete("Use NameTm for Turkmen lookup titles. Retained for global catalog fallbacks, ApplicationType seed keys, and ProjectContract short codes.")]
        [Browsable(false)]
        [VisibleInLookupListView(false)]
        [MaxLength(200)]
        public virtual string Name { get; set; }

        [RuleRequiredField]
        [MaxLength(200)]
        [XafDisplayName("Ady")]
        [VisibleInLookupListView(false)]
        public virtual string NameTm { get; set; }

        /// <summary>
        /// Stable key for UI localization (<see cref="LookupLocalization"/> / <c>Localization/LookupStrings.json</c>).
        /// Seeded from JSON <c>LocalizationKey</c> or <see cref="Code"/>; not the display text shown to users.
        /// </summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [MaxLength(64)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string LocalizationKey { get; set; }

        /// <summary>Culture-aware display name for UI (Layer B). Sole column in lookup popups for global catalogs.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        public string LocalizedDisplayName => LookupLocalization.GetDisplayName(this);

        [MaxLength(20)]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInLookupListView(false)]
        public virtual string Code { get; set; }

        [VisibleInLookupListView(false)]
        public virtual bool IsDefault { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            // Generic logic: If this object is default, uncheck others of the SAME type.
            var objectSpace = ObjectSpaceHelper.Get(this);
            if (objectSpace != null && IsDefault)
            {
                var criteria = DevExpress.Data.Filtering.CriteriaOperator.Parse("ID != ? && IsDefault = ?", this.ID, true);
                var otherDefaults = objectSpace.GetObjects(this.GetType(), criteria);
                
                foreach (LookupBase other in otherDefaults)
                {
                    other.IsDefault = false;
                }
            }
        }
    }

    /// <summary>
    /// Shared base for embedded global lookup catalogs. UI display uses <see cref="LookupBase.LocalizedDisplayName"/>.
    /// Tenant- or app-specific lookups (e.g. <see cref="Position"/>, <see cref="ProjectContract"/>) stay on <see cref="LookupBase"/>.
    /// </summary>
    [DefaultProperty(nameof(LocalizedDisplayName))]
    public abstract class GlobalLookupCatalogBase : LookupBase
    {
        public override string ToString() => LookupLocalization.GetDisplayName(this);
    }

    public enum ApplicationLifecycleStage
    {
        Entry,
        Stay,
        Exit
    }

    public enum ApplicationTypeCategory
    {
        Employee,
        FamilyMember,
        Both
    }

    /// <summary>Whether an <see cref="Application"/> uses ministry review before migration processing.</summary>
    public enum ApplicationProgressRouteKind
    {
        /// <summary>Office preparation, then ministry review/approval, then migration service.</summary>
        ViaMinistries = 0,

        /// <summary>Office preparation, then migration service (no ministry progress states).</summary>
        DirectToMigrationService = 1
    }

    /// <summary>How many ministry review legs apply when <see cref="ApplicationProgressRouteKind.ViaMinistries"/>.</summary>
    public enum MinistryReviewDepth
    {
        None = 0,
        FirstMinistryOnly = 1,
        FirstAndSecondMinistry = 2
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class ApplicationTypeFilter : LookupBase
    {
        public ApplicationTypeFilter()
        {
            ApplicationTypes = new ObservableCollection<ApplicationType>();
        }

        public virtual ApplicationTypeCategory Category { get; set; }

        [InverseProperty(nameof(ApplicationType.ApplicationTypeFilter))]
        public virtual IList<ApplicationType> ApplicationTypes { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application/Config")]
    [DefaultProperty(nameof(LocalizedDisplayName))]
    public class ApplicationType : LookupBase
    {
        public ApplicationType()
        {
            ApplicationReasons = new ObservableCollection<ApplicationReason>();
        }

        public override string ToString() => LookupLocalization.GetDisplayName(this);

        [ModelDefault("AllowEdit", "False")]
        public virtual int PdfForm_Code { get; set; }

        /// <summary>Ministry 3-digit quick-pick code for Application detail view (see docs/APPLICATION_BO_TYPE_SELECTION_REFACTOR.md).</summary>
        [MaxLength(3)]
        [RuleRegularExpression("ApplicationTypeSelectionCodeFormat", DefaultContexts.Save, @"^\d{3}$", CustomMessageTemplate = "Selection code must be exactly three digits (e.g. 701).")]
        public virtual string SelectionCode { get; set; }

        public virtual ApplicationLifecycleStage LifecycleStage { get; set; }
        public virtual ApplicationTypeCategory Category { get; set; }

        public virtual int DurationInDays { get; set; }

        /// <summary>Workflow route for <see cref="ApplicationProgress"/> (ministry vs direct to migration).</summary>
        public virtual ApplicationProgressRouteKind ApplicationProgressRoute { get; set; }

        /// <summary>Ministry legs when <see cref="ApplicationProgressRoute"/> is <see cref="ApplicationProgressRouteKind.ViaMinistries"/>.</summary>
        public virtual MinistryReviewDepth MinistryReviewDepth { get; set; }

        // --- These flags control the visibility of fields in the main Application Detail View ---
        public virtual bool ShowProjectContract { get; set; }
        public virtual bool ShowVisaPeriod { get; set; }
        public virtual bool ShowVisaCategory { get; set; }
        public virtual bool ShowVisaType { get; set; }
        public virtual bool ShowUrgency { get; set; }
        public virtual bool ShowInvitations { get; set; }
        public virtual bool ShowRejections { get; set; }
        public virtual bool ShowWorkPermits { get; set; }
        public virtual bool ShowRegistrations { get; set; }
        public virtual bool ShowVisas { get; set; }
        public virtual bool ShowApplicationItems { get; set; }
        public virtual bool ShowApplicationReason { get; set; }
        public virtual bool ShowMigrationService { get; set; }
        public virtual bool ShowBusinessTrips { get; set; }
        public virtual bool ShowFromCity { get; set; }
        public virtual bool ShowToCity { get; set; }
        public virtual bool ShowMovementPermitLocation { get; set; }
        public virtual bool ShowBorderZoneLocation { get; set; }

        // --- These flags control the visibility of fields in the nested ApplicationItem Detail View ---
        public virtual bool ShowPreviousPassport { get; set; }
        public virtual bool ShowCurrentVisa { get; set; }
        public virtual bool ShowNextVisa { get; set; }
        public virtual bool ShowCurrentWorkPermitItem { get; set; }
        public virtual bool ShowPreviousWorkPermitItem { get; set; }
		public virtual bool ShowCurrentInvitationItem { get; set; }
        public virtual bool ShowPreviousInvitationItem { get; set; }
        public virtual bool ShowCurrentAddressOfResidence { get; set; }
        public virtual bool ShowCurrentWorkDuty { get; set; }
        public virtual bool ShowCurrentSalary { get; set; }
        public virtual bool ShowWorkPermittedLocations { get; set; }
        public virtual bool ShowCurrentMedicalRecord { get; set; }
        public virtual bool ShowCurrentEducation { get; set; }

        // --- These flags control the visibility of status columns in the ApplicationItem List View ---
        public virtual bool ShowInvitationItemIsIssued { get; set; }
        public virtual bool ShowWorkPermitItemIsIssued { get; set; }
        public virtual bool ShowRejectionIssued { get; set; }
        public virtual bool ShowVisaIssued { get; set; }
        public virtual bool ShowVisaIsCancelled { get; set; }
        public virtual bool ShowVisaIsChanged { get; set; }

		public virtual bool ShowInvitationItemIsCancelled { get; set; }

		public virtual bool ShowWorkPermitItemIsCancelled { get; set; }

		public virtual bool ShowInvitationItemIsChanged { get; set; }

		public virtual bool ShowWorkPermitItemIsChanged { get; set; }

        [Browsable(false)]
        public virtual ApplicationTypeFilter ApplicationTypeFilter { get; set; }
        [MaxLength(100)]
        [Browsable(false)]
        public virtual string ApplicationTypeFilterNames { get; set; }
        [Browsable(false)]
        [InverseProperty(nameof(ApplicationReason.ApplicationType))]
        public virtual IList<ApplicationReason> ApplicationReasons { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application/Config")]
   
    [GlobalLookupCatalog(GlobalLookupCatalogKind.ApplicationState)]
    public class ApplicationState : GlobalLookupCatalogBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application/Config")]
    [GlobalLookupCatalog(GlobalLookupCatalogKind.ApplicationLocation)]
    public class ApplicationLocation : GlobalLookupCatalogBase
    {

    }



    [DefaultClassOptions]
    [NavigationItem("Lookup/General/Geography")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.CheckPoint)]
    public class CheckPoint : GlobalLookupCatalogBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/General/Geography")]
    [GlobalLookupCatalog(GlobalLookupCatalogKind.Country)]
    public class Country : GlobalLookupCatalogBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class Department : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Education/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class EducationInstitution : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Education/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.EducationLevel)]
    public class EducationLevel : GlobalLookupCatalogBase
    {
        public virtual string TestProperty { get; set; }
         [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.Gender)]
    public class Gender : GlobalLookupCatalogBase
    {
        [ModelDefault("AllowEdit", "False")]
        public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.MaritalStatus)]
    public class MaritalStatus : GlobalLookupCatalogBase
    {
          [ModelDefault("AllowEdit", "False")]
        public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.MigrationService)]
    public class MigrationService : GlobalLookupCatalogBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class OrganizationType : LookupBase
    {
        public OrganizationType()
        {
            ApplicationTypes = new ObservableCollection<ApplicationType>();
        }

        public virtual IList<ApplicationType> ApplicationTypes { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Passport/Config")]
 
    [GlobalLookupCatalog(GlobalLookupCatalogKind.PassportType)]
    public class PassportType : GlobalLookupCatalogBase
    {
           [ModelDefault("AllowEdit", "False")]
        public virtual string PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class Position : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.PurposeOfTravel)]
    public class PurposeOfTravel : GlobalLookupCatalogBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/General/Geography")]
    [GlobalLookupCatalog(GlobalLookupCatalogKind.Region)]
    public class Region : GlobalLookupCatalogBase
    {
        public Region()
        {
            Cities = new ObservableCollection<City>();
        }

        [ModelDefault("AllowEdit", "False")]
        public virtual string PdfForm_Code { get; set; }

        [InverseProperty(nameof(City.Region))]
        public virtual IList<City> Cities { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person/Config")]
    [GlobalLookupCatalog(GlobalLookupCatalogKind.Relationship)]
    public class Relationship : GlobalLookupCatalogBase
    {
        /// <summary>
        /// Relationship label from the family member's perspective (possessive form).
        /// Used in letter reports where the FM is the subject.
        /// Example: NameTm = "aýaly" (employee's wife) → ReverseNameTm = "adamsy" (FM's husband)
        ///          NameTm = "gyzy"  (employee's daughter) → ReverseNameTm = "kakasy"
        /// AddTurkmenGenitive is applied at report render time: "adamsy" → "adamsynyň"
        /// </summary>
        [MaxLength(200)]
        public virtual string ReverseNameTm { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Education/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class Specialty : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class Subcontractor : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.Urgency)]
    public class Urgency : GlobalLookupCatalogBase
    {
     [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.ValidityDuration)]
    public class ValidityDuration : GlobalLookupCatalogBase
    {   [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        public virtual int NumberOfDays { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.VisaCategory)]
    public class VisaCategory : GlobalLookupCatalogBase
    {
     [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.VisaIssuedPlace)]
    public class VisaIssuedPlace : GlobalLookupCatalogBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa/Config")]
    [GlobalLookupCatalog(GlobalLookupCatalogKind.VisaPeriod)]
    public class VisaPeriod : GlobalLookupCatalogBase
    {
        [ModelDefault("AllowEdit", "False")]
      
      //public virtual int PdfForm_Code { get; set; }
        public virtual string PdfForm__Code { get; set; }
        
        [ModelDefault("AllowEdit", "False")]
        //day,month,year
        public virtual int PdfForm_Count { get; set; }
       // public virtual int Months { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa/Config")]

    [GlobalLookupCatalog(GlobalLookupCatalogKind.VisaType)]
    public class VisaType : GlobalLookupCatalogBase
    {
            [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/General/Geography")]
    [DefaultProperty(nameof(NameTm))]
    public class WorkPermitLocation : LookupBase
    {
        public virtual Region Region { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/WorkPermit/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class MovementPermitLocation : LookupBase
    {
        [MaxLength(500)]
        public override string NameTm { get; set; }
    }

    /// <summary>
    /// Global catalog (Layer B UI via <see cref="GlobalLookupCatalogBase"/>); rows are maintained in the app, not from <c>LookupCatalogs/*.json</c>.
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("Lookup/WorkPermit/Config")]
    [GlobalLookupCatalog(GlobalLookupCatalogKind.BorderZoneLocation)]
    public class BorderZoneLocation : GlobalLookupCatalogBase
    {
        [MaxLength(500)]
        public override string NameTm { get; set; }
    }

    /// <summary>
    /// Short Turkmen labels for border zones selectable on <see cref="ApplicationItem"/> (comma-joined on the item).
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("Lookup/WorkPermit/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class BorderZoneName : LookupBase
    {
    }

    /// <summary>
    /// Short Turkmen labels for work-permitted locations on <see cref="WorkPermitItem"/> (comma-joined on the item).
    /// </summary>
    [DefaultClassOptions]
    [NavigationItem("Lookup/WorkPermit/Config")]
    [DefaultProperty(nameof(NameTm))]
    public class WorkPermittedLocationName : LookupBase
    {
    }
}
