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
    public class InvitationItem : PersonLinkedItemBase<InvitationItem, Invitation>
    {
        [RuleRequiredField]
        public virtual Invitation Invitation { get; set; }

        public override Invitation ParentObject => Invitation;

        private Person person;
        [RuleRequiredField]
        [DataSourceProperty("ParentObject.AvailablePeople")]
        public override Person Person
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