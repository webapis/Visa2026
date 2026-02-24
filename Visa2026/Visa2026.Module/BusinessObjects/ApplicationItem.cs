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
        [ModelDefault("AllowEdit", "False")]
        public virtual Person Person { get; protected set; }

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

        

        public virtual EmployeePositionHistory CurrentPositionHistory { get; set; }

        public string PersonName => Person?.FullName;

        [RuleRequiredField]
        public virtual Passport CurrentPassport { get; set; }

        [Appearance("PreviousPassportVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowPreviousPassport", Context = "DetailView")]
        public virtual Passport PreviousPassport { get; set; }

        [Appearance("VisaVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentVisa", Context = "DetailView")]
        public virtual Visa CurrentVisa { get; set; }

        [Appearance("WorkPermitVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentWorkPermit", Context = "DetailView")]
        public virtual WorkPermit CurrentWorkPermit { get; set; }

        [Appearance("InvitationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentInvitation", Context = "DetailView")]
        public virtual Invitation CurrentInvitation { get; set; }

        [Appearance("AddressOfResidenceVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentAddressOfResidence", Context = "DetailView")]
        public virtual AddressOfResidence CurrentAddressOfResidence { get; set; }

        [Appearance("RegistrationVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentRegistration", Context = "DetailView")]
        public virtual Registration CurrentRegistration { get; set; }

        [Appearance("EmployeeContractVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentEmployeeContract", Context = "DetailView")]
        public virtual EmployeeContract CurrentEmployeeContract { get; set; }

        [Appearance("MedicalRecordVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Application.ApplicationType is null or !Application.ApplicationType.ShowCurrentMedicalRecord", Context = "DetailView")]
        public virtual MedicalRecord CurrentMedicalRecord { get; set; }

    }
}
