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

    /// Tenant lookup: project/contract identity. Belongs to one <see cref="ApprovalLegProfile"/> (ministry route).

    /// Selected on <see cref="Application"/> and <see cref="Person"/> when <see cref="ApplicationType.ShowProjectContract"/> applies.

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

            AllowedApprovalLegProfiles = new ObservableCollection<ProjectContractApprovalLegProfile>();

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
        public virtual Guid? ApprovalLegProfileId { get; set; }

        /// <summary>Parent approval route — filters contract choices on <see cref="Application"/> DetailView.</summary>
        [VisibleInListView(false)]
        public virtual ApprovalLegProfile ApprovalLegProfile { get; set; }

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



        /// <summary>Legacy many-to-many join — use <see cref="ApprovalLegProfile"/> FK instead.</summary>
        [Obsolete("Use ApprovalLegProfile FK (one profile, many contracts). Retained for migration backfill only.")]
        [Browsable(false)]
        [Aggregated]
        [InverseProperty(nameof(ProjectContractApprovalLegProfile.ProjectContract))]
        public virtual IList<ProjectContractApprovalLegProfile> AllowedApprovalLegProfiles { get; set; }

    }

}


