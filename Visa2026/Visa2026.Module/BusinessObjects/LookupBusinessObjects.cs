using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        [RuleUniqueValue]
        [MaxLength(100)]
        public virtual string Name { get; set; }

        [RuleUniqueValue]
        [MaxLength(20)]
        public virtual string Code { get; set; }
    }

    public enum ApplicationTypeCategory
    {
        Employee,
        FamilyMember,
        Both
    }

    [DefaultClassOptions]
    [NavigationItem("Lookup/Application")]
    public class ApplicationType : LookupBase
    {
        public virtual ApplicationTypeCategory Category { get; set; }

        // --- These flags control the visibility of fields in the main Application Detail View ---
        public virtual bool ShowProjectContract { get; set; }
        public virtual bool ShowVisaPeriod { get; set; }
        public virtual bool ShowVisaCategory { get; set; }
        public virtual bool ShowMinistry { get; set; }
        public virtual bool CanRequireWorkPermit { get; set; } // Controls visibility of the 'IsWorkPermitRequired' checkbox

        // --- These flags control the visibility of fields in the nested ApplicationItem Detail View ---
        public virtual bool ShowPreviousPassport { get; set; }
        public virtual bool ShowVisa { get; set; }
        public virtual bool ShowWorkPermit { get; set; }
        public virtual bool ShowPosition { get; set; }
        public virtual bool ShowAddressOfResidence { get; set; }
        public virtual bool ShowCheckPoint { get; set; }
        public virtual bool ShowEntryDate { get; set; }
        public virtual bool ShowVisaIssuedPlace { get; set; }
        public virtual bool ShowPurposeOfTravel { get; set; }
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