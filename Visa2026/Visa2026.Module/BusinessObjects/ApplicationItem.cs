using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    [Appearance("GrayOutIfDeleted", AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsDeleted", Context = "ListView", FontColor = "Gray")]
    public class ApplicationItem : BaseObject, IObjectSpaceLink, ISoftDelete
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        [ModelDefault("AllowEdit", "False")]
        public virtual Person Person { get; protected set; }

        private Employee employee;
        [ImmediatePostData]
        [Appearance("EmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.IsForFamily", Context = "DetailView,ListView")]
        public virtual Employee Employee
        {
            get => employee;
            set
            {
                if (employee != value)
                {
                    employee = value;
                    if (employee != null && (Application == null || !Application.IsForFamily))
                    {
                        Person = employee;
                    }
                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(Employee));
                    }
                }
            }
        }

        private FamilyMember familyMember;
        [ImmediatePostData]
        [Appearance("FamilyMemberVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!Application.IsForFamily", Context = "DetailView,ListView")]
        public virtual FamilyMember FamilyMember
        {
            get => familyMember;
            set
            {
                if (familyMember != value)
                {
                    familyMember = value;
                    if (familyMember != null)
                    {
                        Employee = familyMember.Employee;
                        Person = familyMember;
                    }
                    if (ObjectSpace != null)
                    {
                        CrossObjectSyncHelper.SyncOnPropertyChanged(this, nameof(FamilyMember));
                    }
                }
            }
        }

        

        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        public string PersonName => Person?.FullName;

        [RuleRequiredField]
        public virtual Passport CurrentPassport { get; set; }

        [Appearance("PreviousPassportVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousPassport", Context = "DetailView,ListView")]
        public virtual Passport PreviousPassport { get; set; }

        private Visa currentVisa;
        [ImmediatePostData]
        [Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentVisa", Context = "DetailView,ListView")]
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
    }
}
