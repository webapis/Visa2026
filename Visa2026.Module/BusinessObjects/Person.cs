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
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Lookup/Person")]
    [DefaultProperty(nameof(FullName))]
    [Appearance("EmployeeOnly", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "!IsEmployee", Context = "DetailView", TargetItems = "Company;IsSubcontractorEmployee;Email;CurrentWorkPermitItem;CurrentPositionHistory;CurrentEmployeeContract;CurrentBusinessTrip;HireDate;WorkPermitItems;FamilyMembers;PositionHistory;EmployeeContracts;BusinessTrips;CurrentSalary;Salaries")]
    [Appearance("FamilyMemberOnly", Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide, Criteria = "IsEmployee", Context = "DetailView", TargetItems = "SponsoringEmployee;Relationship;Images")]
    public class Person : BaseObject, IObjectSpaceLink, ISoftDelete
    {
        public Person()
        {
            Educations = new ObservableCollection<Education>();
            Passports = new ObservableCollection<Passport>();
            MedicalRecords = new ObservableCollection<MedicalRecord>();
            AddressesOfResidence = new ObservableCollection<AddressOfResidence>();
            Documents = new ObservableCollection<PersonDocument>();
            Images = new ObservableCollection<FamilyMemberImage>();
            WorkPermitItems = new ObservableCollection<WorkPermitItem>();
            FamilyMembers = new ObservableCollection<Person>();
            PositionHistory = new ObservableCollection<EmployeePositionHistory>();
            EmployeeContracts = new ObservableCollection<EmployeeContract>();
            Salaries = new ObservableCollection<EmployeeSalary>();
            BusinessTrips = new ObservableCollection<BusinessTrip>();
            InvitationItems = new ObservableCollection<InvitationItem>();
            RejectionItems = new ObservableCollection<RejectionItem>();
            Registrations = new ObservableCollection<Registration>();
            TravelHistories = new ObservableCollection<TravelHistory>();
            ApplicationItems = new ObservableCollection<ApplicationItem>();
        }

        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string FirstName { get; set; }

        [MaxLength(100)]
        [RuleRequiredField]
        public virtual string LastName { get; set; }

        [MaxLength(100)]
        public virtual string MiddleName { get; set; }

        /// <summary>
        /// National / civil personal identifier — stable for this person across all passports (canonical source vs legacy per-passport copy).
        /// </summary>
        [MaxLength(50)]
        [RuleRequiredField]
        [ToolTip("National or civil ID number; unique per active person except 0 (use when the passport has no personal number). Same value applies to every passport for this person.")]
        public virtual string PersonalNumber { get; set; }

        /// <summary>
        /// Enforces uniqueness of <see cref="PersonalNumber"/> among non-deleted persons (trimmed, case-insensitive).
        /// The sentinel value <c>0</c> is exempt so multiple foreign employees without an ID on the passport can share it.
        /// </summary>
        [RuleFromBoolProperty("Person_PersonalNumberUniqueAmongActive", DefaultContexts.Save, "Another active person already uses this personal number.")]
        [Browsable(false)]
        public bool IsPersonalNumberUniqueAmongActive
        {
            get
            {
                if (string.IsNullOrWhiteSpace(PersonalNumber))
                {
                    return true;
                }

                if (ObjectSpace == null)
                {
                    return true;
                }

                var normalized = PersonalNumber.Trim().ToUpperInvariant();
                if (normalized == "0")
                {
                    return true;
                }

                var currentId = ID;

                return !ObjectSpace.GetObjectsQuery<Person>()
                    .Where(p => !p.IsDeleted && p.ID != currentId && p.PersonalNumber != null)
                    .Any(p => p.PersonalNumber.Trim().ToUpper() == normalized);
            }
        }

        public string FullName => string.Join(" ", new[] { FirstName, MiddleName, LastName }.Where(s => !string.IsNullOrEmpty(s)));

        private DateTime dateOfBirth;
       [RuleRequiredField]
        [ImmediatePostData]
        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
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
[RuleRequiredField]
        public virtual string BirthPlace { get; set; }
[RuleRequiredField]
        public virtual Country CountryOfBirth { get; set; }

[RuleRequiredField]
        public virtual Gender Gender { get; set; }
[RuleRequiredField]
        public virtual MaritalStatus MaritalStatus { get; set; }

        [RuleRequiredField]
        public virtual Country Nationality { get; set; }
[RuleRequiredField]

        [MaxLength(255)]
        public virtual string ForeignAddress { get; set; }
        public virtual Country ForeignAddressCountry { get; set; }
[RuleRequiredField]
        public virtual ProjectContract ProjectContract { get; set; }

        public virtual bool IsArchived { get; set; }
		[ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
		public virtual byte[] Photo { get; set; }

        [ImmediatePostData]
        [Description("Specifies if this person record represents an Employee or a Family Member.")]
        [ModelDefault("AllowEdit", "False")]
        [Browsable(false)]
        public virtual bool IsEmployee { get; set; }

        // --- Properties from Employee ---
        [ModelDefault("AllowEdit", "False")]
        [RuleRequiredField]
        [ImmediatePostData]
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
        public virtual EmployeeSalary CurrentSalary { get; set; }

        [ModelDefault("AllowEdit", "False")]
        public virtual BusinessTrip CurrentBusinessTrip { get; set; }

        [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
        [ModelDefault("EditMask", "dd.MM.yyyy")]
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
        public virtual IList<Education> Educations { get; set; }

        [InverseProperty(nameof(Passport.Person))]
        [Aggregated]
        public virtual IList<Passport> Passports { get; set; }

        [InverseProperty(nameof(MedicalRecord.Person))]
        [Aggregated]
        public virtual IList<MedicalRecord> MedicalRecords { get; set; }

        [InverseProperty(nameof(AddressOfResidence.Person))]
        [Aggregated]
        public virtual IList<AddressOfResidence> AddressesOfResidence { get; set; }

        [InverseProperty(nameof(PersonDocument.Person))]
        [Aggregated]
        public virtual IList<PersonDocument> Documents { get; set; }

        [InverseProperty(nameof(FamilyMemberImage.Person))]
        [Aggregated]
        public virtual IList<FamilyMemberImage> Images { get; set; }

        [InverseProperty(nameof(WorkPermitItem.Person))]
        [Aggregated]
        [ModelDefault("AllowEdit", "False")]
        public virtual IList<WorkPermitItem> WorkPermitItems { get; set; }

        [InverseProperty(nameof(SponsoringEmployee))]
        [Aggregated]
        public virtual IList<Person> FamilyMembers { get; set; }

        [InverseProperty(nameof(EmployeePositionHistory.Person))]
        [Aggregated]
        public virtual IList<EmployeePositionHistory> PositionHistory { get; set; }

        [InverseProperty(nameof(EmployeeContract.Person))]
        [Aggregated]
        public virtual IList<EmployeeContract> EmployeeContracts { get; set; }

        [InverseProperty(nameof(EmployeeSalary.Person))]
        [Aggregated]
        public virtual IList<EmployeeSalary> Salaries { get; set; }

        [InverseProperty(nameof(BusinessTrip.Person))]
        public virtual IList<BusinessTrip> BusinessTrips { get; set; }

        [InverseProperty(nameof(InvitationItem.Person))]
        [Aggregated]
         [ModelDefault("AllowEdit", "False")]
        public virtual IList<InvitationItem> InvitationItems { get; set; }

        [InverseProperty(nameof(RejectionItem.Person))]
        [Aggregated]
         [ModelDefault("AllowEdit", "False")]
        public virtual IList<RejectionItem> RejectionItems { get; set; }

        [InverseProperty(nameof(Registration.Person))]
        [Aggregated]
      
        public virtual IList<Registration> Registrations { get; set; }

        [InverseProperty(nameof(TravelHistory.Person))]
        [Aggregated]
   
        public virtual IList<TravelHistory> TravelHistories { get; set; }

        [InverseProperty(nameof(ApplicationItem.Person))]
         [ModelDefault("AllowEdit", "False")]
        public virtual IList<ApplicationItem> ApplicationItems { get; set; }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }

        public override void OnCreated()
        {
            base.OnCreated();
            if (ObjectSpace != null)
            {
                Company = ObjectSpace.GetObjectsQuery<Company>().FirstOrDefault(c => c.IsDefault);
                Nationality = ObjectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
                CountryOfBirth = ObjectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
                ForeignAddressCountry = ObjectSpace.GetObjectsQuery<Country>().FirstOrDefault(c => c.IsDefault);
                Gender = ObjectSpace.GetObjectsQuery<Gender>().FirstOrDefault(g => g.IsDefault);
                MaritalStatus = ObjectSpace.GetObjectsQuery<MaritalStatus>().FirstOrDefault(m => m.IsDefault);
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

        public override void OnSaving()
        {
            base.OnSaving();
            if (PersonalNumber != null)
            {
                PersonalNumber = PersonalNumber.Trim();
            }

            if (Photo != null && Photo.Length > 0)
            {
                // Crop to 3:4 passport ratio and resize to match the mail merge template frame.
                // Target: 3 cm × 4 cm at 96 DPI = 113 × 151 px.
                // DevExpress mail merge renders images at their actual pixel size,
                // so these dimensions must match the <<Photo>> frame in the template.
                Photo = ProcessPassportPhoto(Photo, 113, 151);
            }
        }

        private byte[] ProcessPassportPhoto(byte[] imageBytes, int targetWidth, int targetHeight)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(imageBytes))
                {
                    using (Image img = Image.FromStream(ms))
                    {
                        float targetRatio = (float)targetWidth / targetHeight;
                        int sourceWidth = img.Width;
                        int sourceHeight = img.Height;
                        float sourceRatio = (float)sourceWidth / sourceHeight;

                        int cropWidth = sourceWidth;
                        int cropHeight = sourceHeight;
                        int cropX = 0;
                        int cropY = 0;

                        if (sourceRatio > targetRatio) // Too wide: crop sides
                        {
                            cropWidth = (int)(sourceHeight * targetRatio);
                            cropX = (sourceWidth - cropWidth) / 2;
                        }
                        else if (sourceRatio < targetRatio) // Too tall: crop top/bottom
                        {
                            cropHeight = (int)(sourceWidth / targetRatio);
                            cropY = (sourceHeight - cropHeight) / 2;
                        }

                        using (Bitmap newImg = new Bitmap(targetWidth, targetHeight))
                        {
                            // Set explicit DPI so the document renders at exactly the right physical size.
                            // 96 DPI matches the template frame dimensions (3 cm × 4 cm).
                            newImg.SetResolution(96f, 96f);

                            using (Graphics g = Graphics.FromImage(newImg))
                            {
                                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                g.SmoothingMode = SmoothingMode.HighQuality;
                                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                g.DrawImage(img, new Rectangle(0, 0, targetWidth, targetHeight),
                                    new Rectangle(cropX, cropY, cropWidth, cropHeight), GraphicsUnit.Pixel);
                            }
                            using (MemoryStream outMs = new MemoryStream())
                            {
                                newImg.Save(outMs, ImageFormat.Png);
                                return outMs.ToArray();
                            }
                        }
                    }
                }
            }
            catch { return imageBytes; } // Fallback to original if processing fails
        }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        #endregion
    }
}