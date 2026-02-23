using System;
using System.Collections.Generic;
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
    [DefaultProperty(nameof(InvitationItemName))]
    public class InvitationItem : SingleActiveBaseObject<Person, InvitationItem>
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

        [RuleFromBoolProperty("InvitationItem_PersonIsValid", DefaultContexts.Save, "The selected person is not part of the parent application.")]
        [Browsable(false)]
        public bool IsPersonValid
        {
            get
            {
                if (Person == null || Invitation?.Application == null) return true;
                return Invitation.Application.ApplicationItems.Any(ai => ai.Person?.ID == Person.ID);
            }
        }

        public override Person GetParent()
        {
            return Person;
        }

        public override IList<InvitationItem> GetSiblings(Person parent)
        {
            return parent?.InvitationItems;
        }

        public override void SetParentActiveItem(Person parent, InvitationItem item)
        {
            parent.CurrentInvitationItem = item;
        }

        public override bool IsParentActiveItem(Person parent, InvitationItem item)
        {
            return parent.CurrentInvitationItem == item;
        }

        public string InvitationItemName => $"{Person?.FullName} - {Invitation?.InvitationNumber}";
    }
}