using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;
using System.Linq;
using System.Collections.Generic;
using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [DefaultProperty(nameof(ApplicationItemName))]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
    public class ApplicationItem : BaseObject, IObjectSpaceLink, ISoftDelete    //10
    {
        public ApplicationItem()
        {
        }

        [RuleRequiredField]
        [ImmediatePostData] // Ensure changes to Application trigger updates
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        [ImmediatePostData]
        [DataSourceProperty("AvailablePeople")]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(Person));
                    }
                }
            }
        }
        private Person person;

        [Browsable(false)]
        [NotMapped]
        public IList<Person> AvailablePeople
        {
            get
            {
                if (ObjectSpace == null) return new List<Person>();

                if (Application == null) return new List<Person>();

                if (Application.Category == ApplicationTypeCategory.Both)
                {
                    return ObjectSpace.GetObjectsQuery<Person>().ToList();
                }

                bool isEmployee = Application.Category == ApplicationTypeCategory.Employee;
                return ObjectSpace.GetObjectsQuery<Person>().Where(p => p.IsEmployee == isEmployee).ToList();
            }
        }

        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        [XafDisplayName("Person Full Name")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonFullName => Person?.FullName;

        [XafDisplayName("Person Gender")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonGender => Person?.Gender?.Name;

        [XafDisplayName("Person Marital Status")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonMaritalStatus => Person?.MaritalStatus?.Name;

        [XafDisplayName("Person Birth Place")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonBirthPlace => Person?.BirthPlace;

        [XafDisplayName("Person Country of Birth")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCountryOfBirth => Person?.CountryOfBirth?.Name;

        [XafDisplayName("Person Country of Birth Full")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCountryOfBirthFull => Person?.CountryOfBirth != null ? $"{Person.CountryOfBirth.Code}, {Person.CountryOfBirth.Name}" : null;

        [XafDisplayName("Person Nationality")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonNationality => Person?.Nationality?.Name;

        [XafDisplayName("Person Nationality Full")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonNationalityFull => Person?.Nationality != null ? $"{Person.Nationality.Code}, {Person.Nationality.Name}" : null;

        [XafDisplayName("Person Photo")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
        public byte[] PersonPhoto => Person?.Photo;

        [XafDisplayName("Person Position")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPosition => CurrentPositionHistory?.Position?.Name;

        [XafDisplayName("Person Foreign Address")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonForeignAddress => Person?.ForeignAddress;

        [XafDisplayName("Person Foreign Address Country Full")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonForeignAddressCountryFull => Person?.ForeignAddressCountry != null ? $"{Person.ForeignAddressCountry.Code}, {Person.ForeignAddressCountry.Name}" : null;

        [XafDisplayName("Person Department")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonDepartment => CurrentPositionHistory?.Department?.Name;

        [XafDisplayName("Person Passport Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportNumber => CurrentPassport?.PassportNumber;

        [XafDisplayName("Person Passport Personal Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportPersonalNumber => CurrentPassport?.PersonalNumber;

        [XafDisplayName("Person Passport Type")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportType => CurrentPassport?.PassportType?.Name;

        [XafDisplayName("Person Passport Authority")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportAuthority => CurrentPassport?.Authority;

        [XafDisplayName("Person Passport Country")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportCountry => CurrentPassport?.IssuedCountry?.Name;

        [XafDisplayName("Person Passport Country Full")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportCountryFull => CurrentPassport?.IssuedCountry != null ? $"{CurrentPassport.IssuedCountry.Code}, {CurrentPassport.IssuedCountry.Name}" : null;

        [XafDisplayName("Person Passport Issue Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonPassportIssueDate => CurrentPassport?.IssueDate;

        [XafDisplayName("Person Passport Issue Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportIssueDateText => $"{CurrentPassport?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Person Passport Expiration Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonPassportExpirationDate => CurrentPassport?.ExpirationDate;

        [XafDisplayName("Person Passport Expiration Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonPassportExpirationDateText => $"{CurrentPassport?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Passport Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportNumber => PreviousPassport?.PassportNumber;

        [XafDisplayName("Previous Passport Personal Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportPersonalNumber => PreviousPassport?.PersonalNumber;

        [XafDisplayName("Previous Passport Type")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportType => PreviousPassport?.PassportType?.Name;

        [XafDisplayName("Previous Passport Authority")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportAuthority => PreviousPassport?.Authority;

        [XafDisplayName("Previous Passport Country")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportCountry => PreviousPassport?.IssuedCountry?.Name;

        [XafDisplayName("Previous Passport Country Full")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportCountryFull => PreviousPassport?.IssuedCountry != null ? $"{PreviousPassport.IssuedCountry.Code}, {PreviousPassport.IssuedCountry.Name}" : null;

        [XafDisplayName("Previous Passport Issue Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PreviousPassportIssueDate => PreviousPassport?.IssueDate;

        [XafDisplayName("Previous Passport Issue Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportIssueDateText => $"{PreviousPassport?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Passport Expiration Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PreviousPassportExpirationDate => PreviousPassport?.ExpirationDate;

        [XafDisplayName("Previous Passport Expiration Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassportExpirationDateText => $"{PreviousPassport?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Person Date of Birth")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonDateOfBirth => Person?.DateOfBirth;

        [XafDisplayName("Person Date of Birth (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonDateOfBirthText => $"{Person?.DateOfBirth:dd.MM.yyyy}";

        [XafDisplayName("Person Current Address")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddress => CurrentAddressOfResidence?.FullAddress;

        [XafDisplayName("Person Current Address Type")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddressType => CurrentAddressOfResidence?.Type?.ToString();

        [XafDisplayName("Person Current Address Region")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddressRegion => CurrentAddressOfResidence?.Region?.Name;

        [XafDisplayName("Person Current Address City")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddressCity => CurrentAddressOfResidence?.City?.Name;

        [XafDisplayName("Person Current Address Start Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonCurrentAddressStartDate => CurrentAddressOfResidence?.StartDate;

        [XafDisplayName("Person Current Address Start Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddressStartDateText => $"{CurrentAddressOfResidence?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Person Current Address Expiration Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonCurrentAddressExpirationDate => CurrentAddressOfResidence?.ExpirationDate;

        [XafDisplayName("Person Current Address Expiration Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonCurrentAddressExpirationDateText => $"{CurrentAddressOfResidence?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Person Salary")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public decimal? PersonSalary => CurrentEmployeeContract?.Salary;

        [XafDisplayName("Person Salary (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonSalaryText => $"{CurrentEmployeeContract?.Salary:#,##0.00}";

        [XafDisplayName("Person Visa Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaNumber => CurrentVisa?.VisaNumber;

        [XafDisplayName("Person Visa Category")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaCategory => CurrentVisa?.VisaCategory?.Name;

        [XafDisplayName("Person Visa Type")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaType => CurrentVisa?.VisaType?.Name;

        [XafDisplayName("Person Visa Expiration Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonVisaExpirationDate => CurrentVisa?.ExpirationDate;

        [XafDisplayName("Person Visa Start Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonVisaStartDate => CurrentVisa?.StartDate;

        [XafDisplayName("Person Visa Start Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaStartDateText => $"{CurrentVisa?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Person Visa Expiration Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaExpirationDateText => $"{CurrentVisa?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Person Visa Issued Place")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaIssuedPlace => CurrentVisa?.VisaIssuedPlace?.Name;

        [XafDisplayName("Person Visa Issue Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonVisaIssueDate => CurrentVisa?.IssueDate;

        [XafDisplayName("Person Visa Issue Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonVisaIssueDateText => $"{CurrentVisa?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Person Education Level")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonEducationLevel => CurrentEducation?.EducationLevel?.Name;

        [XafDisplayName("Person Education Institution")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonEducationInstitution => CurrentEducation?.EducationInstitution?.Name;

        [XafDisplayName("Person Education Country")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonEducationCountry => CurrentEducation?.EducationCountry?.Name;

        [XafDisplayName("Person Education Country Full")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonEducationCountryFull => CurrentEducation?.EducationCountry != null ? $"{CurrentEducation.EducationCountry.Code}, {CurrentEducation.EducationCountry.Name}" : null;

        [XafDisplayName("Person Education Specialty")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonEducationSpecialty => CurrentEducation?.Specialty?.Name;

        [XafDisplayName("Person Education Graduation Year")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public int? PersonEducationGraduationYear => CurrentEducation?.GraduationYear;

        [XafDisplayName("Person Work Permit Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonWorkPermitNumber => CurrentWorkPermitItem?.WorkPermitNumber;

        [XafDisplayName("Person Work Permit Expiration Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonWorkPermitExpirationDate => CurrentWorkPermitItem?.ExpirationDate;

        [XafDisplayName("Person Work Permit Expiration Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonWorkPermitExpirationDateText => $"{CurrentWorkPermitItem?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Person Medical Record Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonMedicalRecordNumber => CurrentMedicalRecord?.DocumentNumber;

        [XafDisplayName("Person Medical Record Issue Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonMedicalRecordIssueDate => CurrentMedicalRecord?.IssueDate;

        [XafDisplayName("Person Medical Record Expiration Date")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PersonMedicalRecordExpirationDate => CurrentMedicalRecord?.ExpirationDate;

        [XafDisplayName("Person Medical Record Expiration Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string PersonMedicalRecordExpirationDateText => $"{CurrentMedicalRecord?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Application Full Number")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string ApplicationFullNumber => Application?.FullApplicationNumber;

        [XafDisplayName("Application Date (Text)")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string ApplicationDateText => $"{Application?.ApplicationDate:dd.MM.yyyy}";

        [XafDisplayName("Sponsor Company Name")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string SponsorName => Application?.Company?.Name;

        [XafDisplayName("Sponsor Authorized Signatory")]
        [VisibleInDetailView(false), VisibleInListView(false)]
        public string SponsorAuthorizedSignatory => Application?.CompanyHead?.FullName;

        [RuleRequiredField]
        public virtual Passport CurrentPassport { get; set; }

        [Appearance("PreviousPassportVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousPassport", Context = "DetailView,ListView")]
        public virtual Passport PreviousPassport { get; set; }

        private Visa currentVisa;
        [ImmediatePostData]
        [XafDisplayName("Target Visa")]
        [InverseProperty(nameof(Visa.AssociatedApplicationItems))]
        [Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentVisa", Context = "DetailView,ListView")]
        [ForeignKey(nameof(CurrentVisaId))] // Explicitly define foreign key
        public virtual Visa CurrentVisa
        {
            get => currentVisa;
            set
            {
                if (currentVisa != value)
                {
                    var oldValue = currentVisa;
                    currentVisa = value;

                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(CurrentVisa), oldValue);
                    }
                }
            }
        }
        // Foreign key property for CurrentVisa
        public virtual Guid? CurrentVisaId { get; set; }

        private WorkPermitItem currentWorkPermitItem;
        [ImmediatePostData]
        [Appearance("WorkPermitItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentWorkPermitItem", Context = "DetailView,ListView")]
        public virtual WorkPermitItem CurrentWorkPermitItem
        {
            get => currentWorkPermitItem;
            set
            {
                if (currentWorkPermitItem != value)
                {
                    var oldValue = currentWorkPermitItem;
                    currentWorkPermitItem = value;

                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(CurrentWorkPermitItem), oldValue);
                    }
                }
            }
        }

        private InvitationItem currentInvitationItem;
        [ImmediatePostData]
        [Appearance("InvitationItemVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentInvitationItem", Context = "DetailView,ListView")]
        public virtual InvitationItem CurrentInvitationItem
        {
            get => currentInvitationItem;
            set
            {
                if (currentInvitationItem != value)
                {
                    var oldValue = currentInvitationItem;
                    currentInvitationItem = value;

                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(CurrentInvitationItem), oldValue);
                    }
                }
            }
        }

        [Appearance("AddressOfResidenceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentAddressOfResidence", Context = "DetailView,ListView")]
        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        [Appearance("RegistrationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentRegistration", Context = "DetailView,ListView")]
        public virtual Registration CurrentRegistration { get; set; }

        [Appearance("EmployeeContractVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentEmployeeContract", Context = "DetailView,ListView")]
        public virtual EmployeeContract CurrentEmployeeContract { get; set; }

        [Appearance("MedicalRecordVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentMedicalRecord", Context = "DetailView,ListView")]
        public virtual MedicalRecord CurrentMedicalRecord { get; set; }

        [Appearance("EducationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentEducation", Context = "DetailView,ListView")]
        public virtual Education CurrentEducation { get; set; }

        [Appearance("InvitationIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowInvitationItemIsIssued", Context ="DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool InvitationItemIsIssued { get; set; }

        [Appearance("WorkPermitIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowWorkPermitItemIsIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool WorkPermitItemIsIssued { get; set; }

        [Appearance("RejectionIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowRejectionIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool RejectionIssued { get; set; }

        [Appearance("VisaIssuedColumnVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisaIssued", Context = "DetailView,ListView")]
         [ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIssued { get; set; }

		[Appearance("InvitationItemIsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowInvitationItemIsCancelled", Context = "DetailView,ListView")]
		public virtual bool InvitationItemIsCancelled { get; set; }

		[Appearance("IsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowWorkPermitItemIsCancelled", Context = "DetailView,ListView")]
		public virtual bool IsCancelled { get; set; }

		[Appearance("InvitationItemIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowInvitationItemIsChanged", Context = "DetailView,ListView")]
		public virtual bool InvitationItemIsChanged { get; set; }

		[Appearance("WorkPermitItemIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowWorkPermitItemIsChanged", Context = "DetailView,ListView")]
		public virtual bool WorkPermitItemIsChanged { get; set; }

		[Appearance("VisaIsCancelledVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisaIsCancelled", Context = "DetailView,ListView")]
		[ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIsCancelled { get; set; }

		[Appearance("VisaIsChangedVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisaIsChanged", Context = "DetailView,ListView")]
		[ModelDefault("AllowEdit", "False")]
        public virtual bool VisaIsChanged { get; set; }

        public virtual bool ApplicationItemsIsCancelled { get; set; }

        [Browsable(false)]
        public virtual bool IsDeleted { get; set; }

        [Browsable(false)]
        public virtual DateTime? DateDeleted { get; set; }

        [Browsable(false)]
        public virtual ApplicationUser DeletedBy { get; set; }

        #region IObjectSpaceLink
        [NotMapped]
        [Browsable(false)]
        public IObjectSpace ObjectSpace { get; set; }
        #endregion

        [MaxLength(255)]
        [ModelDefault("AllowEdit", "False")]
        public virtual string ApplicationItemName { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            ApplicationItemName = $"{Person?.FullName} - {Application?.FullApplicationNumber}";
        }
    }
}
