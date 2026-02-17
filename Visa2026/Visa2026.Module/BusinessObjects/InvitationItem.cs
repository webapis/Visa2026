using System;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    [RuleValidApplicationPerson]
    public class InvitationItem : BaseObject, IApplicationItemChild
    {
        [RuleRequiredField]
        public virtual Invitation Invitation { get; set; }

        private Person person;
        [RuleRequiredField]
        [DataSourceProperty("Invitation.AvailablePeople")]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (person != null && Invitation?.Application != null)
                    {
                        var appItem = Invitation.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == person.ID);
                        if (appItem != null)
                        {
                            Passport = appItem.Passport;
                        }
                    }
                }
            }
        }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [DataSourceProperty("Invitation.AvailablePeople")]
        [Appearance("EmployeeVisible", Visibility = ViewItemVisibility.Hide, Criteria = "Invitation.Application.IsForFamily", Context = "DetailView")]
        public virtual Employee Employee
        {
            get => Person as Employee;
            set => Person = value;
        }

        [NotMapped]
        [ImmediatePostData]
        [DataSourceProperty("Invitation.AvailablePeople")]
        [Appearance("FamilyMemberVisible", Visibility = ViewItemVisibility.Hide, Criteria = "!Invitation.Application.IsForFamily", Context = "DetailView")]
        public virtual FamilyMember FamilyMember
        {
            get => Person as FamilyMember;
            set => Person = value;
        }

        [Browsable(false)]
        Application IApplicationItemChild.Application => Invitation?.Application;
    }
}