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

namespace Visa2026.Module.BusinessObjects
{
    // Abstract base class to enforce standard structure as per LookupBusinessObjects.md
    public abstract class LookupBase : BaseObject
    {
        [RuleRequiredField]
        [MaxLength(100)]
        public virtual string Name { get; set; }

        [MaxLength(20)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string Code { get; set; }
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

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application")]
    public class ApplicationTypeFilter : LookupBase
    {
        [InverseProperty(nameof(ApplicationType.ApplicationTypeFilter))]
        public virtual IList<ApplicationType> ApplicationTypes { get; set; } = new ObservableCollection<ApplicationType>();
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application")]

    public class ApplicationType : LookupBase
    {   
        public virtual int PdfForm_Code { get; set; }
        public virtual ApplicationLifecycleStage LifecycleStage { get; set; }
        public virtual ApplicationTypeCategory Category { get; set; }

        public virtual int DurationInDays { get; set; }

        // --- These flags control the visibility of fields in the main Application Detail View ---
        public virtual bool ShowProjectContract { get; set; }
        public virtual bool ShowVisaPeriod { get; set; }
        public virtual bool ShowVisaCategory { get; set; }
        public virtual bool ShowUrgency { get; set; }
        public virtual bool ShowInvitations { get; set; }
        public virtual bool ShowRejections { get; set; }
        public virtual bool ShowWorkPermits { get; set; }
        public virtual bool ShowRegistrations { get; set; }
        public virtual bool ShowVisas { get; set; }
        public virtual bool ShowApplicationItems { get; set; }
        public virtual bool ShowApplicationReason { get; set; }
        public virtual bool ShowMigrationService { get; set; }
        public virtual bool ShowBusinessTripPlan { get; set; }
        public virtual bool ShowBusinessTrips { get; set; }

        // --- These flags control the visibility of fields in the nested ApplicationItem Detail View ---
        public virtual bool ShowPreviousPassport { get; set; }
        public virtual bool ShowCurrentVisa { get; set; }
        public virtual bool ShowCurrentWorkPermitItem { get; set; }
		public virtual bool ShowCurrentInvitationItem { get; set; }
        public virtual bool ShowCurrentAddressOfResidence { get; set; }
        public virtual bool ShowCurrentRegistration { get; set; }
        public virtual bool ShowCurrentEmployeeContract { get; set; }
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
        [RuleRequiredField]
        public virtual OrganizationType OrganizationType { get; set; }
        [MaxLength(100)]
        [Browsable(false)]
        public virtual string OrganizationTypeNames { get; set; }

        public virtual ApplicationTypeFilter ApplicationTypeFilter { get; set; }
        [MaxLength(100)]
        [Browsable(false)]
        public virtual string ApplicationTypeFilterNames { get; set; }
        [InverseProperty(nameof(ApplicationReason.ApplicationType))]
        public virtual IList<ApplicationReason> ApplicationReasons { get; set; } = new ObservableCollection<ApplicationReason>();
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application")]
   
    public class ApplicationState : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application")]
   
    public class ApplicationLocation : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class BorderZone : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Geography")]

    public class CheckPoint : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Geography")]

    public class Country : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]

    public class Department : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Education")]

    public class EducationInstitution : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Education")]

    public class EducationLevel : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]

    public class Gender : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]

    public class MaritalStatus : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]

    public class MigrationService : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]

    public class OrganizationType : LookupBase
    {
        [InverseProperty(nameof(ApplicationType.OrganizationType))]
        public virtual IList<ApplicationType> ApplicationTypes { get; set; } = new ObservableCollection<ApplicationType>();
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Passport")]
 
    public class PassportType : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]
  
    public class Position : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class PurposeOfTravel : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Geography")]

    public class Region : LookupBase
    {
        [InverseProperty(nameof(City.Region))]
        public virtual IList<City> Cities { get; set; } = new ObservableCollection<City>();
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
 
    public class Relationship : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Education")]

    public class Specialty : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Organization")]

    public class Subcontractor : LookupBase
    {
        [MaxLength(255)]
        public virtual string ContactPerson { get; set; }

        [MaxLength(50)]
        public virtual string PhoneNumber { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class Urgency : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class ValidityDuration : LookupBase
    {   [RuleValueComparison(DefaultContexts.Save, ValueComparisonType.GreaterThan, 0)]
        public virtual int NumberOfDays { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class VisaCategory : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class VisaIssuedPlace : LookupBase
    {
        public virtual bool IsDefault { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class VisaPeriod : LookupBase
    {
        public virtual int Months { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class VisaType : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Geography")]

    public class WorkPermitLocation : LookupBase
    {
        public virtual Region Region { get; set; }
    }
}