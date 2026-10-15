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
using Visa2026.Module.Documentation;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Tenant lookup: project/contract identity.
    /// Selected on an application profile instance when the profile shows a project.
    /// Approval-leg chains are chosen separately and are not stored on the contract.
    /// </summary>
    [UserDocumentation("administration/configuration/contracts-and-approvals", Category = "Administration")]
    [DefaultClassOptions]
    [DefaultProperty(nameof(NameTm))]
    [NavigationItem("Configuration")]
    [Appearance(
        "ProjectContract_HideCatalogScalars",
        AppearanceItemType = "ViewItem",
        TargetItems = "Code;IsDefault",
        Visibility = ViewItemVisibility.Hide,
        Context = "DetailView,ListView,LookupListView")]
    public class ProjectContract : LookupBase
    {
        public ProjectContract()
        {
            Images = new ObservableCollection<ProjectContractImage>();
            Documents = new ObservableCollection<ProjectContractDocument>();
        }

        /// <summary>Legacy DB column — not used for workflow.</summary>
        [Obsolete("Ministry depth is defined on ApprovalLegProfile. Retained for DB/import compatibility only.")]
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public virtual MinistryReviewDepth MinistryReviewDepth { get; set; } = MinistryReviewDepth.FirstMinistryOnly;

        [MaxLength(2000)]
        [ModelDefault("RowCount", "4")]
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
    }
}
