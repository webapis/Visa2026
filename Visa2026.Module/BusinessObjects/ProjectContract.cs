using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;

namespace Visa2026.Module.BusinessObjects
{
    /// <summary>
    /// Lookup: project / contract identifier on <see cref="Application"/> (Name, NameTm, Code).
    /// Legacy <see cref="Description"/>, <see cref="Company"/>, <see cref="Ministry"/>, <see cref="Images"/>,
    /// and <see cref="Documents"/> remain in the database for reports and import but are hidden from the UI.
    /// </summary>
    [DefaultClassOptions]
    [DefaultProperty(nameof(Name))]
    [NavigationItem("Lookup/Organization")]
    public class ProjectContract : LookupBase
    {
        public ProjectContract()
        {
            Images = new ObservableCollection<ProjectContractImage>();
            Documents = new ObservableCollection<ProjectContractDocument>();
        }

        /// <summary>Legacy contract narrative for ministry letters; use Word report static text for new work.</summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        [MaxLength(4000)]
        public virtual string Description { get; set; }

        /// <summary>Legacy FK; organization identity uses <see cref="CompanyProfile"/> singleton.</summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public virtual Company Company { get; set; }

        /// <summary>Legacy ministry addressee source; ministry letter templates use static or Application-level data.</summary>
        [Browsable(false)]
        [VisibleInDetailView(false)]
        [VisibleInListView(false)]
        [VisibleInLookupListView(false)]
        [ModelDefault("AllowEdit", "False")]
        public virtual Ministry Ministry { get; set; }

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
