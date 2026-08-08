using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("BorderZone")]
    public class BorderZoneItem  : BaseObject
    {
        [RuleRequiredField]
        public virtual BorderZone BorderZone { get; set; }

        private Person person;
        [RuleRequiredField]
        [DataSourceProperty(nameof(BorderZone) + "." + nameof(IPersonLinkParent.AvailablePeople))]
        public virtual Person Person
        {
            get => person;
            set
            {
                if (person != value)
                {
                    person = value;
                    if (person != null)
                        Passport = ApplicationPersonValidItems.ResolvePassport(person);
                }
            }
        }

        [RuleRequiredField]
        public virtual Passport Passport { get; set; }

        public virtual bool IsCancelled { get; set; }


        public override void OnSaving()
        {
            base.OnSaving();
            CrossObjectSyncHelper.SyncOnSave(this);
        }

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            Visa2026.Module.Localization.VisaUiMessages.Get("BorderZoneDocumentCopies.List.ColumnLink");
    }
}