using System;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    public class InvitationItem : BaseObject
    {
        [RuleRequiredField]
        public virtual Invitation Invitation { get; set; }

        [RuleRequiredField]
        public virtual Person Person { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Appearance("EmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Invitation.Application.IsForFamily", Context = "DetailView")]
        public virtual Employee Employee
        {
            get => Person as Employee;
            set => Person = value;
        }

        [NotMapped]
        [ImmediatePostData]
        [Appearance("FamilyMemberVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!Invitation.Application.IsForFamily", Context = "DetailView")]
        public virtual FamilyMember FamilyMember
        {
            get => Person as FamilyMember;
            set => Person = value;
        }
    }
}