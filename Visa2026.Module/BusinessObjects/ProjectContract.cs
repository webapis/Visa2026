using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Tenant lookup: one row = one approval process (project/contract + ordered ministry legs).
    /// Selected on <see cref="Application"/> and <see cref="Person"/> when <see cref="ApplicationType.ShowProjectContract"/> applies.
    /// Multiple rows may share the same <see cref="LookupBase.Code"/> (e.g. Şatlyk‑1 with 1, 2, or 3 ministry legs).
    /// </summary>
    [DefaultClassOptions]
    [DefaultProperty(nameof(NameTm))]
    [NavigationItem("Configuration")]
    [Appearance(
        "ProjectContract_HideCatalogScalars",
        AppearanceItemType = "ViewItem",
        TargetItems = "Code;IsDefault;Description",
        Visibility = ViewItemVisibility.Hide,
        Context = "DetailView,ListView,LookupListView")]
    public class ProjectContract : LookupBase
    {
        public ProjectContract()
        {
            Images = new ObservableCollection<ProjectContractImage>();
            Documents = new ObservableCollection<ProjectContractDocument>();
            MinistryLegs = new ObservableCollection<ProjectContractMinistryLeg>();
        }

        /// <summary>Legacy DB column — not used for workflow.</summary>
        [Obsolete("Ministry legs are defined on MinistryLegs. Retained for DB/import compatibility only.")]
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public virtual MinistryReviewDepth MinistryReviewDepth { get; set; } = MinistryReviewDepth.FirstMinistryOnly;

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [MaxLength(500)]
        public virtual string Description { get; set; }

        public virtual bool IsActive { get; set; } = true;

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        [InverseProperty(nameof(ProjectContractImage.ProjectContract))]
        [Aggregated]
        public virtual IList<ProjectContractImage> Images { get; set; }

        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        [InverseProperty(nameof(ProjectContractDocument.ProjectContract))]
        [Aggregated]
        public virtual IList<ProjectContractDocument> Documents { get; set; }

        [Aggregated]
        [InverseProperty(nameof(ProjectContractMinistryLeg.ProjectContract))]
        public virtual IList<ProjectContractMinistryLeg> MinistryLegs { get; set; }

        public override void OnSaving()
        {
            ProjectContractMinistryHelper.WireMinistryLegs(this);
            base.OnSaving();
        }
    }
}
