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

        #region Person
        [XafDisplayName("Full Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_FullName => Person?.FullName;

        [XafDisplayName("Birth Place"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_BirthPlace => Person?.BirthPlace;

        [XafDisplayName("Foreign Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_ForeignAddress => Person?.ForeignAddress;

        [XafDisplayName("Photo"), VisibleInDetailView(false), VisibleInListView(false)]
        [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
        public byte[] Person_Photo => Person?.Photo;

        [XafDisplayName("Date of Birth"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Person_DateOfBirth => Person?.DateOfBirth;

        [XafDisplayName("Date of Birth (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_DateOfBirthText => $"{Person?.DateOfBirth:dd.MM.yyyy}";

        [XafDisplayName("Nationality (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_NationalityTm => Person?.Nationality?.NameTm;

        [XafDisplayName("Country of Birth (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Person_CountryOfBirthTm => Person?.CountryOfBirth?.NameTm;
        #endregion

        #region Position
        [XafDisplayName("Position (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_PositionTm => CurrentPositionHistory?.Position?.NameTm;

        [XafDisplayName("Department (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Position_DepartmentTm => CurrentPositionHistory?.Department?.NameTm;
        #endregion

        #region Passport
        [XafDisplayName("Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Number => CurrentPassport?.PassportNumber;

        [XafDisplayName("Passport Personal Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_PersonalNumber => CurrentPassport?.PersonalNumber;

        [XafDisplayName("Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_Authority => CurrentPassport?.Authority;

        [XafDisplayName("Passport Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Passport_IssueDate => CurrentPassport?.IssueDate;

        [XafDisplayName("Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_IssueDateText => $"{CurrentPassport?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Passport Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Passport_ExpirationDate => CurrentPassport?.ExpirationDate;

        [XafDisplayName("Passport Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_ExpirationDateText => $"{CurrentPassport?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Passport Country (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Passport_CountryTm => CurrentPassport?.IssuedCountry?.NameTm;
        #endregion

        #region PreviousPassport
        [XafDisplayName("Previous Passport Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_Number => PreviousPassport?.PassportNumber;

        [XafDisplayName("Previous Passport Personal Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_PersonalNumber => PreviousPassport?.PersonalNumber;

        [XafDisplayName("Previous Passport Authority"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_Authority => PreviousPassport?.Authority;

        [XafDisplayName("Previous Passport Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PreviousPassport_IssueDate => PreviousPassport?.IssueDate;

        [XafDisplayName("Previous Passport Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_IssueDateText => $"{PreviousPassport?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Previous Passport Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? PreviousPassport_ExpirationDate => PreviousPassport?.ExpirationDate;

        [XafDisplayName("Previous Passport Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string PreviousPassport_ExpirationDateText => $"{PreviousPassport?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region Visa
        [XafDisplayName("Visa Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_Number => CurrentVisa?.VisaNumber;

        [XafDisplayName("Visa Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Visa_IssueDate => CurrentVisa?.IssueDate;

        [XafDisplayName("Visa Issue Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_IssueDateText => $"{CurrentVisa?.IssueDate:dd.MM.yyyy}";

        [XafDisplayName("Visa Start Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Visa_StartDate => CurrentVisa?.StartDate;

        [XafDisplayName("Visa Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_StartDateText => $"{CurrentVisa?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Visa Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Visa_ExpirationDate => CurrentVisa?.ExpirationDate;

        [XafDisplayName("Visa Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_ExpirationDateText => $"{CurrentVisa?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Visa Issued Place (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_IssuedPlaceTm => CurrentVisa?.VisaIssuedPlace?.NameTm;

        [XafDisplayName("Visa Category (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_CategoryTm => CurrentVisa?.VisaCategory?.NameTm;

        [XafDisplayName("Visa Type (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Visa_TypeTm => CurrentVisa?.VisaType?.NameTm;
        #endregion

        #region Address
        [XafDisplayName("Address Full Address"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_FullAddress => CurrentAddressOfResidence?.FullAddress;

        [XafDisplayName("Address Type"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_Type => CurrentAddressOfResidence?.Type?.ToString();

        [XafDisplayName("Address Start Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Address_StartDate => CurrentAddressOfResidence?.StartDate;

        [XafDisplayName("Address Start Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_StartDateText => $"{CurrentAddressOfResidence?.StartDate:dd.MM.yyyy}";

        [XafDisplayName("Address Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? Address_ExpirationDate => CurrentAddressOfResidence?.ExpirationDate;

        [XafDisplayName("Address Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_ExpirationDateText => $"{CurrentAddressOfResidence?.ExpirationDate:dd.MM.yyyy}";

        [XafDisplayName("Address Region (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_RegionTm => CurrentAddressOfResidence?.Region?.NameTm;

        [XafDisplayName("Address City (Tm)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Address_CityTm => CurrentAddressOfResidence?.City?.NameTm;
        #endregion

        #region Contract
        [XafDisplayName("Contract Salary"), VisibleInDetailView(false), VisibleInListView(false)]
        public decimal? Contract_Salary => CurrentEmployeeContract?.Salary;

        [XafDisplayName("Contract Salary (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Contract_SalaryText => $"{CurrentEmployeeContract?.Salary:#,##0.00}";
        #endregion

        #region Education
        [XafDisplayName("Education Graduation Year"), VisibleInDetailView(false), VisibleInListView(false)]
        public int? Education_GraduationYear => CurrentEducation?.GraduationYear;
        #endregion

        #region WorkPermit
        [XafDisplayName("Work Permit Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit_Number => CurrentWorkPermitItem?.WorkPermitNumber;

        [XafDisplayName("Work Permit Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? WorkPermit_ExpirationDate => CurrentWorkPermitItem?.ExpirationDate;

        [XafDisplayName("Work Permit Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string WorkPermit_ExpirationDateText => $"{CurrentWorkPermitItem?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region MedicalRecord
        [XafDisplayName("Medical Record Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MedicalRecord_Number => CurrentMedicalRecord?.DocumentNumber;

        [XafDisplayName("Medical Record Issue Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? MedicalRecord_IssueDate => CurrentMedicalRecord?.IssueDate;

        [XafDisplayName("Medical Record Expiration Date"), VisibleInDetailView(false), VisibleInListView(false)]
        public DateTime? MedicalRecord_ExpirationDate => CurrentMedicalRecord?.ExpirationDate;

        [XafDisplayName("Medical Record Expiration Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string MedicalRecord_ExpirationDateText => $"{CurrentMedicalRecord?.ExpirationDate:dd.MM.yyyy}";
        #endregion

        #region Application
        [XafDisplayName("Application Full Number"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_FullNumber => Application?.FullApplicationNumber;

        [XafDisplayName("Application Date (Text)"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_DateText => $"{Application?.ApplicationDate:dd.MM.yyyy}";

        [XafDisplayName("Sponsor Name"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_SponsorName => Application?.Company?.Name;

        [XafDisplayName("Sponsor Authorized Signatory"), VisibleInDetailView(false), VisibleInListView(false)]
        public string Application_SponsorSignatory => Application?.CompanyHead?.FullName;
        #endregion

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
