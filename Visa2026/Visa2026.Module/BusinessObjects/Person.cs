using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using DevExpress.ExpressApp;
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
    public class Person : BaseObject, IObjectSpaceLink
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

        private DateTime dateOfBirth;
       [RuleRequiredField]
        [ImmediatePostData]
        public virtual DateTime DateOfBirth
        {
            get => dateOfBirth;
            set
            {
                if (dateOfBirth != value)
                {
                    dateOfBirth = value;
                    if (ObjectSpace != null)
                    {
                        if (Age < 18)
                        {
                            MaritalStatus = ObjectSpace.FirstOrDefault<MaritalStatus>(m => m.Name == "Çaga");
                        }
                        else if (MaritalStatus?.Name == "Çaga")
                        {
                            MaritalStatus = null;
                        }
                    }
                }
            }
        }

        [NotMapped]
        [ModelDefault("AllowEdit", "False")]
        public int Age
        {
            get
            {
                return CalculateAge(DateOfBirth);
            }
        }

        public virtual Gender Gender { get; set; }

        public virtual MaritalStatus MaritalStatus { get; set; }

        [RuleRequiredField]
        public virtual Country Nationality { get; set; }

        public virtual ProjectContract ProjectContract { get; set; }

        public virtual bool IsArchived { get; set; }
		[ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
		public virtual byte[] Photo { get; set; }
        [ModelDefault("AllowEdit", "False")]
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
[ModelDefault("AllowEdit", "False")]
        public virtual Registration CurrentRegistration { get; set; }
[ModelDefault("AllowEdit", "False")]
        public virtual TravelHistory CurrentTravelHistory { get; set; }

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
        [Aggregated]
        public virtual IList<InvitationItem> InvitationItems { get; set; } = new ObservableCollection<InvitationItem>();

        [InverseProperty(nameof(RejectionItem.Person))]
        [Aggregated]
        public virtual IList<RejectionItem> RejectionItems { get; set; } = new ObservableCollection<RejectionItem>();

        [InverseProperty(nameof(Registration.Person))]
        [Aggregated]
        public virtual IList<Registration> Registrations { get; set; } = new ObservableCollection<Registration>();

        [InverseProperty(nameof(TravelHistory.Person))]
        [Aggregated]
        public virtual IList<TravelHistory> TravelHistories { get; set; } = new ObservableCollection<TravelHistory>();

        [InverseProperty(nameof(ApplicationItem.Person))]
        public virtual IList<ApplicationItem> ApplicationItems { get; set; } = new ObservableCollection<ApplicationItem>();

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }

        #endregion

        #region Helper Methods

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age))
                age--;
            return age < 0 ? 0 : age;
        }

        #endregion
    }
}