using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
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
    public class InvitationItem : PersonLinkedItemBase<InvitationItem, Invitation>, ISoftDelete
    {
        [RuleRequiredField]
        public virtual Invitation Invitation { get; set; }

        public override Invitation ParentObject => Invitation;

        [RuleRequiredField]
        [DataSourceProperty("ParentObject.AvailablePeople")]
        public override Person Person
        {
            get => base.Person;
            set
            {
                if (base.Person != value)
                {
                    base.Person = value;
                    if (base.Person != null && Invitation?.Application != null)
                    {
                        var appItem = Invitation.Application.ApplicationItems.FirstOrDefault(ai => ai.Person?.ID == base.Person.ID);
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

        [RuleFromBoolProperty("InvitationItem_PersonUniqueInInvitation", DefaultContexts.Save, "This person already has an Invitation Item in the same Invitation.")]
        [Browsable(false)]
        public bool IsPersonUniqueInInvitation
        {
            get
            {
                if (Person == null || Invitation == null) return true;
                return !Invitation.InvitationItems.Any(ii => ii.ID != ID && !ii.IsDeleted && ii.Person?.ID == Person.ID);
            }
        }

        public override IList<InvitationItem> GetSiblings(Person parent)
        {
            return parent?.InvitationItems;
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

        [MaxLength(255)]
        public virtual string InvitationItemName { get; set; }

        public override void OnSaving()
        {
            base.OnSaving();
            InvitationItemName = $"{Person?.FullName} - {Invitation?.InvitationNumber}";
            CrossObjectSyncHelper.SyncOnSave(this);
        }

	public virtual bool IsCancelled { get; set; }

	public virtual bool IsChanged { get; set; }

    // This MUST be an override to correctly interact with the SingleActiveBaseObject logic
    // and the SyncRule engine. The default value of 'true' is set in the base class's OnCreated method.
    public override bool IsActive { get; set; }

    public virtual bool IsUsed { get; set; }

    [Browsable(false)]
    public virtual bool IsDeleted { get; set; }

    [Browsable(false)]
    public virtual DateTime? DateDeleted { get; set; }

    [Browsable(false)]
    public virtual ApplicationUser DeletedBy { get; set; }
    }


}