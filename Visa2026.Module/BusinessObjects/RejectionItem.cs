using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services.ApplicationPersonRoster;

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

        /// <summary>Required; always visible on detail view (with <see cref="Passport"/>).</summary>
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
                    if (base.Person != null)
                        Passport = ApplicationProfileInstancePersonValidItems.ResolvePassport(base.Person);
                }
            }
        }

        /// <summary>Required; always visible on detail view (with <see cref="Person"/>).</summary>
        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        public virtual string Reason { get; set; }

        [MaxLength(255)]
        public virtual string RejectionItemName { get; set; }

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
            RejectionItemName = $"{Person?.FullName} - Rejected on {Rejection?.Date:d}";
            MarkLinkedApplicationItemRejected();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

        private void MarkLinkedApplicationItemRejected()
        {
            // ApplicationRosterMergeLine hard-removed; rejection linkage is via ResolvedLinks / M2M roster.
        }

        public virtual void OnDeleting()
        {
            CrossObjectSyncHelper.SyncOnDelete(this);
        }

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            Visa2026.Module.Localization.VisaUiMessages.Get("RejectionDocumentCopies.List.ColumnLink");
    }
}
