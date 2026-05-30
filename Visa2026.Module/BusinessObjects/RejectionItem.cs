using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Rejection")]
    [DefaultProperty(nameof(RejectionItemName))]
    public class RejectionItem : PersonLinkedItemBase<RejectionItem, Rejection>
    {
        [RuleRequiredField]
        public virtual Rejection Rejection { get; set; }

        public override Rejection ParentObject => Rejection;

        public virtual string Reason { get; set; }

        [NotMapped]
        public string RejectionItemName => $"{Person?.FullName} - Rejected on {Rejection?.Date:d}";

        [RuleFromBoolProperty("RejectionItem_PersonIsValid", DefaultContexts.Save, "The selected person is not part of the parent application.")]
        [Browsable(false)]
        public override bool IsPersonValid
        {
            get => base.IsPersonValid;
        }

        [RuleFromBoolProperty("RejectionItem_PersonUniqueInRejection", DefaultContexts.Save, "This person already has a Rejection Item in the same Rejection.")]
        [Browsable(false)]
        public bool IsPersonUniqueInRejection
        {
            get
            {
                if (Person == null || Rejection == null) return true;
                return !Rejection.RejectionItems.Any(ri => ri.ID != ID && ri.Person?.ID == Person.ID);
            }
        }

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

        // This method should be called by a Controller handling the ObjectDeleting event.
        public virtual void OnDeleting()
        {
            CrossObjectSyncHelper.SyncOnDelete(this);
        }
    }
}
