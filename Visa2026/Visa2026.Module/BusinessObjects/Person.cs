using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(FullName))]
    public class Person : BaseObject
    {
        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string FirstName { get; set; }

        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string LastName { get; set; }

        [MaxLength(100)]
        public virtual string MiddleName { get; set; }

        public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrEmpty(s)));

        [RuleRequiredField]
        public virtual DateTime BirthDate { get; set; }

        [RuleRequiredField]
        public virtual Country Nationality { get; set; }

        public virtual Passport CurrentPassport { get; set; }

        public virtual Visa CurrentVisa { get; set; }

        public virtual Education CurrentEducation { get; set; }

        public virtual MedicalRecord CurrentMedicalRecord { get; set; }

        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        [InverseProperty(nameof(Education.Person))]
        public virtual IList<Education> Educations { get; set; } = new ObservableCollection<Education>();

        [InverseProperty(nameof(Visa.Person))]
        public virtual IList<Visa> Visas { get; set; } = new ObservableCollection<Visa>();

        [InverseProperty(nameof(Passport.Person))]
        public virtual IList<Passport> Passports { get; set; } = new ObservableCollection<Passport>();

        [InverseProperty(nameof(MedicalRecord.Person))]
        public virtual IList<MedicalRecord> MedicalRecords { get; set; } = new ObservableCollection<MedicalRecord>();

        [InverseProperty(nameof(AddressOfResidence.Person))]
        public virtual IList<AddressOfResidence> AddressesOfResidence { get; set; } = new ObservableCollection<AddressOfResidence>();

        [InverseProperty(nameof(PersonDocument.Person))]
        public virtual IList<PersonDocument> Documents { get; set; } = new ObservableCollection<PersonDocument>();
    }
}