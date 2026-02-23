using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.ExpressApp.Model;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.DC;
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

        public virtual Gender Gender { get; set; }

        [RuleRequiredField]
        public virtual Country Nationality { get; set; }

        public virtual ProjectContract ProjectContract { get; set; }

        public virtual bool IsArchived { get; set; }
		[ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
		public virtual byte[] Photo { get; set; }
        public virtual Passport CurrentPassport { get; set; }
[ModelDefault("AllowEdit", "False")]
        [ImmediatePostData]
        public virtual Visa CurrentVisa { get; set; }
[ModelDefault("AllowEdit", "False")]
        public virtual Education CurrentEducation { get; set; }
[ModelDefault("AllowEdit", "False")]
        public virtual MedicalRecord CurrentMedicalRecord { get; set; }
[ModelDefault("AllowEdit", "False")]
        [ImmediatePostData]
        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }
[ModelDefault("AllowEdit", "False")]
        public virtual InvitationItem CurrentInvitationItem { get; set; }
[ModelDefault("AllowEdit", "False")]
        public virtual RejectionItem CurrentRejectionItem { get; set; }

        [InverseProperty(nameof(Education.Person))]
        [Aggregated]
        public virtual IList<Education> Educations { get; set; } = new ObservableCollection<Education>();

        [InverseProperty(nameof(Passport.Person))]
        [Aggregated]
        public virtual IList<Passport> Passports { get; set; } = new ObservableCollection<Passport>();

        [InverseProperty(nameof(MedicalRecord.Person))]
        [Aggregated]
        public virtual IList<MedicalRecord> MedicalRecords { get; set; } = new ObservableCollection<MedicalRecord>();

        [InverseProperty(nameof(AddressOfResidence.Person))]
        [Aggregated]
        public virtual IList<AddressOfResidence> AddressesOfResidence { get; set; } = new ObservableCollection<AddressOfResidence>();

        [InverseProperty(nameof(PersonDocument.Person))]
        [Aggregated]
        public virtual IList<PersonDocument> Documents { get; set; } = new ObservableCollection<PersonDocument>();

        [InverseProperty(nameof(InvitationItem.Person))]
        public virtual IList<InvitationItem> InvitationItems { get; set; } = new ObservableCollection<InvitationItem>();

        [InverseProperty(nameof(RejectionItem.Person))]
        public virtual IList<RejectionItem> RejectionItems { get; set; } = new ObservableCollection<RejectionItem>();
    }
}