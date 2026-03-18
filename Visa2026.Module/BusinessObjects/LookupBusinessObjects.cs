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
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    // Abstract base class to enforce standard structure as per LookupBusinessObjects.md
    public abstract class LookupBase : BaseObject, IObjectSpaceLink
    {
        [RuleRequiredField]
        [MaxLength(100)]
        public virtual string Name { get; set; }

        [RuleRequiredField]
        [MaxLength(100)]
        public virtual string NameTm { get; set; }

        [MaxLength(20)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string Code { get; set; }

        public virtual bool IsDefault { get; set; }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion

        public override void OnSaving()
        {
            base.OnSaving();
            // Generic logic: If this object is default, uncheck others of the SAME type.
            if (ObjectSpace != null && IsDefault)
            {
                var criteria = DevExpress.Data.Filtering.CriteriaOperator.Parse("ID != ? && IsDefault = ?", this.ID, true);
                var otherDefaults = ObjectSpace.GetObjects(this.GetType(), criteria);
                
                foreach (LookupBase other in otherDefaults)
                {
                    other.IsDefault = false;
                }
            }
        }
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
        public ApplicationTypeFilter()
        {
            ApplicationTypes = new ObservableCollection<ApplicationType>();
        }

        public virtual ApplicationTypeCategory Category { get; set; }

        [InverseProperty(nameof(ApplicationType.ApplicationTypeFilter))]
        public virtual IList<ApplicationType> ApplicationTypes { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application")]

    public class ApplicationType : LookupBase
    {
        public ApplicationType()
        {
            ApplicationReasons = new ObservableCollection<ApplicationReason>();
        }

        [ModelDefault("AllowEdit", "False")]
        public virtual int PdfForm_Code { get; set; }
        public virtual ApplicationLifecycleStage LifecycleStage { get; set; }
        public virtual ApplicationTypeCategory Category { get; set; }

        public virtual int DurationInDays { get; set; }

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
        public virtual IList<ApplicationReason> ApplicationReasons { get; set; }
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
         [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]

    public class Gender : LookupBase
    {
        [ModelDefault("AllowEdit", "False")]
        public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]

    public class MaritalStatus : LookupBase
    {
          [ModelDefault("AllowEdit", "False")]
        public virtual int PdfForm_Code { get; set; }
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
        public OrganizationType()
        {
            ApplicationTypes = new ObservableCollection<ApplicationType>();
        }

        [InverseProperty(nameof(ApplicationType.OrganizationType))]
        public virtual IList<ApplicationType> ApplicationTypes { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Passport")]
 
    public class PassportType : LookupBase
    {
           [ModelDefault("AllowEdit", "False")]
        public virtual string PdfForm_Code { get; set; }
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
     [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
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
     [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class VisaIssuedPlace : LookupBase
    {
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Visa")]

    public class VisaPeriod : LookupBase
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
    [NavigationItem("Lookup/Visa")]

    public class VisaType : LookupBase
    {
            [ModelDefault("AllowEdit", "False")]
     public virtual int PdfForm_Code { get; set; }
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Geography")]

    public class WorkPermitLocation : LookupBase
    {
        public virtual Region Region { get; set; }
    }
}