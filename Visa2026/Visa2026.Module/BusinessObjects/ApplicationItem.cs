using System;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Application")]
    public class ApplicationItem : BaseObject
    {
        [RuleRequiredField]
        public virtual Application Application { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        private Employee employee;
        [ImmediatePostData]
        [Appearance("EmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.IsForFamily", Context = "DetailView")]
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
                }
            }
        }

        private FamilyMember familyMember;
        [ImmediatePostData]
        [Appearance("FamilyMemberVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!Application.IsForFamily", Context = "DetailView")]
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
                }
            }
        }

        public string PersonName => Person?.FullName;

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        [Appearance("PreviousPassportVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousPassport", Context = "DetailView")]
        public virtual Passport PreviousPassport { get; set; }

        [Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisa", Context = "DetailView")]
        public virtual Visa Visa { get; set; }

        [Appearance("WorkPermitVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowWorkPermit", Context = "DetailView")]
        public virtual WorkPermit WorkPermit { get; set; }

		[Appearance("InvitationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowInvitation", Context = "DetailView")]

	   
        public virtual Invitation Invitation { get; set; }



        [Appearance("PositionVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPosition", Context = "DetailView")]
        public virtual Position Position { get; set; }

        [Appearance("AddressOfResidenceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowAddressOfResidence", Context = "DetailView")]
        public virtual AddressOfResidence AddressOfResidence { get; set; }

        [Appearance("CheckPointVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCheckPoint", Context = "DetailView")]
        public virtual CheckPoint CheckPoint { get; set; }

        [Appearance("EntryDateVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowEntryDate", Context = "DetailView")]
        public virtual DateTime? EntryDate { get; set; }

        [Appearance("VisaIssuedPlaceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowVisaIssuedPlace", Context = "DetailView")]
        public virtual VisaIssuedPlace VisaIssuedPlace { get; set; }

        [Appearance("PurposeOfTravelVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPurposeOfTravel", Context = "DetailView")]
        public virtual PurposeOfTravel PurposeOfTravel { get; set; }

        [MaxLength(50)]
        public virtual string RegistrationNumber { get; set; }

        public virtual DateTime? RegistrationDate { get; set; }

        public virtual string Status
        {
            get
            {
                if (Cancelled) return "Cancelled";
                return "Pending";
            }
        }

        public virtual bool Cancelled { get; set; }
    }
}