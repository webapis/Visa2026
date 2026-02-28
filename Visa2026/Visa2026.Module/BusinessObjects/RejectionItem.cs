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
    [NavigationItem("Application")]
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

        public override IList<RejectionItem> GetSiblings(Person parent)
        {
            return parent?.RejectionItems;
        }

        public override void SetParentActiveItem(Person parent, RejectionItem item)
        {
            parent.CurrentRejectionItem = item;
        }

        public override bool IsParentActiveItem(Person parent, RejectionItem item)
        {
            return parent.CurrentRejectionItem == item;
        }

        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }
    }
}