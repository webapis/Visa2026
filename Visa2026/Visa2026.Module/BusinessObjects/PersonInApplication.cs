using System;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class PersonInApplication : BaseObject
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

        [Appearance("PreviousPassportVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!(!Application.IsForFamily && Application.EmployeeApplicationType = 'ApplicationForChangingPassport')", Context = "DetailView")]
        public virtual Passport PreviousPassport { get; set; }

        [Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType In ('ApplicationForVisaExtention', 'ApplicationForChangingVisaCategory', 'ApplicationForCancellingVisa', 'ApplicationForCancellingVisaAndWorkpermit', 'ApplicationForBorderZonePermision')) || (Application.IsForFamily && Application.FamilyApplicationType = 'ApplicationForVisaExtention'))", Context = "DetailView")]
        public virtual Visa Visa { get; set; }

        [Appearance("WorkPermitVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!(!Application.IsForFamily && ((Application.EmployeeApplicationType = 'ApplicationForVisaExtention' && Application.IsWorkPermitRequired) || Application.EmployeeApplicationType In ('ApplicationForCancellingWorkPermit', 'ApplicationForCancellingVisaAndWorkpermit', 'RugsatnamaMöhletineGöräÇakylyk', 'ApplicationForExtendingWorkPermitLocation')))", Context = "DetailView")]
        public virtual WorkPermit WorkPermit { get; set; }

        [Appearance("PositionVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType In ('ApplicationForInvitation', 'ApplicationForChangingInvitation', 'ApplicationForVisaExtention', 'ApplicationForChangingVisaCategory', 'ApplicationForChangingPassport', 'ApplicationForCancellingVisa', 'ApplicationForCancellingWorkPermit', 'ApplicationForCancellingVisaAndWorkpermit', 'RugsatnamaMöhletineGöräÇakylyk', 'ApplicationForExtendingWorkPermitLocation', 'ApplicationForBorderZonePermision')) || (Application.IsForFamily && Application.FamilyApplicationType In ('ApplicationForInvitation', 'ApplicationForVisaExtention')))", Context = "DetailView")]
        public virtual Position Position { get; set; }

        [Appearance("AddressOfResidenceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType In ('ApplicationForRegistrationUpOnArrival', 'ApplicationForRegistrationExtention', 'ApplicationForStrikeOffRegister', 'ApplicationForRegisteringToANewLocation')) || (Application.IsForFamily && Application.FamilyApplicationType In ('ApplicationForRegistrationUpOnArrival', 'ApplicationForRegistrationExtention', 'ApplicationForStrikeOffRegister')))", Context = "DetailView")]
        public virtual AddressOfResidence AddressOfResidence { get; set; }

        [Appearance("CheckPointVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType = 'ApplicationForRegistrationUpOnArrival') || (Application.IsForFamily && Application.FamilyApplicationType = 'ApplicationForRegistrationUpOnArrival'))", Context = "DetailView")]
        public virtual CheckPoint CheckPoint { get; set; }

        [Appearance("EntryDateVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType = 'ApplicationForRegistrationUpOnArrival') || (Application.IsForFamily && Application.FamilyApplicationType = 'ApplicationForRegistrationUpOnArrival'))", Context = "DetailView")]
        public virtual DateTime? EntryDate { get; set; }

        [Appearance("VisaIssuedPlaceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType = 'ApplicationForRegistrationUpOnArrival') || (Application.IsForFamily && Application.FamilyApplicationType = 'ApplicationForRegistrationUpOnArrival'))", Context = "DetailView")]
        public virtual VisaIssuedPlace VisaIssuedPlace { get; set; }

        [Appearance("PurposeOfTravelVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!((!Application.IsForFamily && Application.EmployeeApplicationType = 'ApplicationForRegistrationUpOnArrival') || (Application.IsForFamily && Application.FamilyApplicationType = 'ApplicationForRegistrationUpOnArrival'))", Context = "DetailView")]
        public virtual PurposeOfTravel PurposeOfTravel { get; set; }

        [MaxLength(50)]
        public virtual string RegistrationNumber { get; set; }

        public virtual DateTime? RegistrationDate { get; set; }

        public virtual string Status
        {
            get
            {
                if (Cancelled) return "Cancelled";
                if (Rejection != null) return "Rejected";
                if (IssuedVisa != null) return "VisaIssued";
                if (IssuedWorkPermit != null) return "WorkPermitIssued";
                if (Invitation != null) return "Invited";
                return "Pending";
            }
        }

        public virtual bool Cancelled { get; set; }

        public virtual Invitation Invitation { get; set; }

        public virtual Visa IssuedVisa { get; set; }

        public virtual WorkPermit IssuedWorkPermit { get; set; }

        public virtual Rejection Rejection { get; set; }
    }
}