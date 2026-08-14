using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;
using Visa2026.Module.Editors;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [NavigationItem("Invitation")]
    [DefaultProperty(nameof(InvitationItemName))]
    [RuleCriteria("InvitationItem_StatusFlagsExclusive", DefaultContexts.Save,
        "Not (IsCancelled And IsChanged) And Not (IsCancelled And IsUsed) And Not (IsChanged And IsUsed)",
        "Only one of Cancelled, Changed, or Used can be set on an invitation item.")]
    [Appearance("InvitationItem_CancelledRow", Priority = 310, AppearanceItemType = "ViewItem", TargetItems = "*",
        Criteria = "IsCancelled = true", Context = "ListView", BackColor = "LightCoral", FontColor = "Firebrick")]
    [SupportsOptionalDetailFields]
    public class InvitationItem : PersonLinkedItemBase<InvitationItem, Invitation>, IOptionalDetailFields
    {
        private bool suppressStatusSync;
        private bool isCancelled;
        private bool isChanged;
        private bool isUsed;
        [RuleRequiredField]
        public virtual Invitation Invitation { get; set; }

        public override Invitation ParentObject => Invitation;

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
                return !Invitation.InvitationItems.Any(ii => ii.ID != ID && ii.Person?.ID == Person.ID);
            }
        }

        [MaxLength(255)]
        public virtual string InvitationItemName { get; set; }

        [NotMapped]
        [ImmediatePostData]
        [Index(-1000)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [EditorAlias(OptionalDetailFieldsEditorAliases.Toggle)]
        [ModelDefault("CustomCSSClassName", "xaf-optional-fields-toggle")]
        [XafDisplayName(" ")]
        public bool ShowOptionalFields { get; set; }

        public override void OnSaving()
        {
            suppressStatusSync = true;
            try
            {
                InvitationStatusFlagsHelper.NormalizeInvitationItem(this);
                InvitationStatusFlagsHelper.AlignSiblingsFromItem(this);
                InvitationStatusFlagsHelper.NormalizeInvitationItem(this);
            }
            finally
            {
                suppressStatusSync = false;
            }

            base.OnSaving();
            InvitationItemName = $"{Person?.FullName} - {Invitation?.InvitationNumber}";
            CrossObjectSyncHelper.SyncOnSave(this);
        }

        /// <summary>Optional; mutually exclusive with <see cref="IsChanged"/> and <see cref="IsUsed"/>.</summary>
        [ImmediatePostData]
        [VisibleInListView(false)]
        public virtual bool IsCancelled
        {
            get => isCancelled;
            set => SetItemStatusCancelled(value);
        }

        /// <summary>Optional; mutually exclusive with <see cref="IsCancelled"/> and <see cref="IsUsed"/>.</summary>
        [ImmediatePostData]
        [VisibleInListView(false)]
        public virtual bool IsChanged
        {
            get => isChanged;
            set => SetItemStatusChanged(value);
        }

        /// <summary>Optional; mutually exclusive with <see cref="IsCancelled"/> and <see cref="IsChanged"/>.</summary>
        [ImmediatePostData]
        [VisibleInListView(false)]
        public virtual bool IsUsed
        {
            get => isUsed;
            set => SetItemStatusUsed(value);
        }

        internal void SetItemStatusFlags(bool cancelled, bool changed, bool used)
        {
            suppressStatusSync = true;
            try
            {
                isCancelled = cancelled;
                isChanged = changed;
                isUsed = used;
            }
            finally
            {
                suppressStatusSync = false;
            }

            ObjectSpaceHelper.Get(this)?.SetModified(this);
        }

        private void SetItemStatusCancelled(bool value)
        {
            if (suppressStatusSync)
            {
                isCancelled = value;
                return;
            }

            if (isCancelled == value)
            {
                return;
            }

            isCancelled = value;
            if (value)
            {
                isChanged = false;
                isUsed = false;
                InvitationStatusFlagsHelper.AlignSiblingsFromItem(this);
            }
        }

        private void SetItemStatusChanged(bool value)
        {
            if (suppressStatusSync)
            {
                isChanged = value;
                return;
            }

            if (isChanged == value)
            {
                return;
            }

            isChanged = value;
            if (value)
            {
                isCancelled = false;
                isUsed = false;
                InvitationStatusFlagsHelper.AlignSiblingsFromItem(this);
            }
        }

        private void SetItemStatusUsed(bool value)
        {
            if (suppressStatusSync)
            {
                isUsed = value;
                return;
            }

            if (isUsed == value)
            {
                return;
            }

            isUsed = value;
            if (value)
            {
                isCancelled = false;
                isChanged = false;
            }
        }

        /// <summary>
        /// ApplicationProfileInstance linked on the parent <see cref="Invitation"/> (if any). Read-only ListView/Detail convenience.
        /// </summary>
        [NotMapped]
        [ExcludeFromOptionalDetailFields]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(true)]
        [VisibleInDetailView(true)]
        [VisibleInLookupListView(false)]
        [XafDisplayName("Application Profile instance")]
        [ToolTip("ApplicationProfileInstance linked on the parent invitation (Invitation.ApplicationProfileInstance).")]
        public ApplicationProfileInstance ApplicationProfileInstance => Invitation?.ApplicationProfileInstance;

        /// <summary>
        /// Visa issued using this invitation line (<see cref="Visa.InvitationItem"/>). Typically 0–1 when used.
        /// Read-only; cell tint on ListView follows visa <see cref="Visa.StateSeverityLevel"/>.
        /// </summary>
        [ExcludeFromOptionalDetailFields]
        [InverseProperty(nameof(Visa.InvitationItem))]
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(true)]
        [VisibleInLookupListView(false)]
        [XafDisplayName("Issued Visa")]
        [ToolTip("Visa issued using this invitation item (Visa.InvitationItem).")]
        [Appearance("InvitationItem_IssuedVisa_Info", Priority = 100, AppearanceItemType = "ViewItem", TargetItems = "IssuedVisa",
            Criteria = "IssuedVisa is not null and IssuedVisa.StateSeverityLevel = 1", Context = "ListView", BackColor = "LightSkyBlue")]
        [Appearance("InvitationItem_IssuedVisa_Warning", Priority = 200, AppearanceItemType = "ViewItem", TargetItems = "IssuedVisa",
            Criteria = "IssuedVisa is not null and IssuedVisa.StateSeverityLevel = 2", Context = "ListView", BackColor = "LightSalmon")]
        [Appearance("InvitationItem_IssuedVisa_Critical", Priority = 300, AppearanceItemType = "ViewItem", TargetItems = "IssuedVisa",
            Criteria = "IssuedVisa is not null and IssuedVisa.StateSeverityLevel >= 3", Context = "ListView", BackColor = "LightCoral")]
        public virtual Visa IssuedVisa { get; set; }

        /// <summary>Skip-navigation M2M with <see cref="ApplicationProfileInstance"/> (input linked items). Distinct from parent Invitation header FK.</summary>
        [ModelDefault("AllowEdit", "False")]
        [VisibleInListView(false)]
        public virtual IList<ApplicationProfileInstance> ApplicationProfileInstances { get; set; } = new ObservableCollection<ApplicationProfileInstance>();

        /// <summary>ListView link column that opens header document copies in the preview slot.</summary>
        [NotMapped]
        [VisibleInDetailView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public string DocumentCopiesListLink =>
            Visa2026.Module.Localization.VisaUiMessages.Get("InvitationDocumentCopies.List.ColumnLink");

    }
}
