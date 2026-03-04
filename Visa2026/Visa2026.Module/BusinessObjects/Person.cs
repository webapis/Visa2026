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
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(FullName))]
    [Appearance("EmployeeOnly", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!IsEmployee", Context = "DetailView", TargetItems = "Company;IsSubcontractorEmployee;Email;CurrentWorkPermitItem;CurrentPositionHistory;CurrentEmployeeContract;CurrentBusinessTrip;HireDate;WorkPermitItems;FamilyMembers;PositionHistory;EmployeeContracts;BusinessTrips")]
    [Appearance("FamilyMemberOnly", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "IsEmployee", Context = "DetailView", TargetItems = "SponsoringEmployee;Relationship;Images")]
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

        [ImmediatePostData]
        [Description("Specifies if this person record represents an Employee or a Family Member.")]
        public virtual bool IsEmployee { get; set; }

        // --- Properties from Employee ---
        public virtual Company Company { get; set; }

        public virtual bool IsSubcontractorEmployee { get; set; }

        [Appearance("SubcontractorVisible", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!IsSubcontractorEmployee or !IsEmployee", Context = "DetailView")]
        public virtual Subcontractor Subcontractor { get; set; }

        [MaxLength(255)]
        [RuleRegularExpression("EmployeeEmailFormat", DefaultContexts.Save, @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$", CustomMessageTemplate = "Invalid email format.")]
        public virtual string Email { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual WorkPermitItem CurrentWorkPermitItem { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual EmployeeContract CurrentEmployeeContract { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual BusinessTrip CurrentBusinessTrip { get; set; }

        public virtual DateTime HireDate { get; set; }

        // --- Properties from FamilyMember ---
        [DataSourceCriteria("IsEmployee = true")]
        [InverseProperty(nameof(FamilyMembers))]
        public virtual Person SponsoringEmployee { get; set; }

        [RuleRequiredField(TargetCriteria = "!IsEmployee")]
        public virtual Relationship Relationship { get; set; }

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

        [InverseProperty(nameof(FamilyMemberImage.Person))]
        [Aggregated]
        public virtual IList<FamilyMemberImage> Images { get; set; } = new ObservableCollection<FamilyMemberImage>();

        [InverseProperty(nameof(WorkPermitItem.Person))]
        [Aggregated]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; } = new ObservableCollection<WorkPermitItem>();

        [InverseProperty(nameof(SponsoringEmployee))]
        [Aggregated]
        public virtual IList<Person> FamilyMembers { get; set; } = new ObservableCollection<Person>();

        [InverseProperty(nameof(EmployeePositionHistory.Person))]
        [Aggregated]
        public virtual IList<EmployeePositionHistory> PositionHistory { get; set; } = new ObservableCollection<EmployeePositionHistory>();

        [InverseProperty(nameof(EmployeeContract.Person))]
        [Aggregated]
        public virtual IList<EmployeeContract> EmployeeContracts { get; set; } = new ObservableCollection<EmployeeContract>();

        [InverseProperty(nameof(BusinessTrip.Person))]
        public virtual IList<BusinessTrip> BusinessTrips { get; set; } = new ObservableCollection<BusinessTrip>();

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

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null && IsEmployee)
            {
                Company = ObjectSpace.GetObjectsQuery<Company>().FirstOrDefault(c => c.IsDefault);
            }
        }

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