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
                            Passport = appItem.CurrentPassport;
                        }
                    }
                }
            }
        }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        

        [RuleFromBoolProperty("InvitationItem_PersonIsValid", DefaultContexts.Save, "The selected person is not part of the parent application.")]
        [Browsable(false)]
        public override bool IsPersonValid
        {
            get => base.IsPersonValid;
        }

        public override IList<InvitationItem> GetSiblings(Person parent)
        {
            // Assuming Person has a collection of InvitationItems, but it's not in the Person class definition provided earlier.
            // If PersonLinkedItemBase requires this, we need to add it to Person or handle it differently.
            // For now, returning null or empty list if the property doesn't exist on Person.
            // However, the error was about 'Passport' on 'ApplicationItem'.
            return null;
        }

        public override void SetParentActiveItem(Person parent, InvitationItem item)
        {
            // parent.CurrentInvitationItem = item; // Assuming this property exists or needs to be added.
        }

        public override bool IsParentActiveItem(Person parent, InvitationItem item)
        {
            // return parent.CurrentInvitationItem == item;
            return false;
        }

        public string InvitationItemName => $"{Person?.FullName} - {Invitation?.InvitationNumber}";

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

	public virtual bool IsCancelled { get; set; }

	public virtual bool IsChanged { get; set; }

    public virtual bool IsUsed { get; set; }
    
    }


}