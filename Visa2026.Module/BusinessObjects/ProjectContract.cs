using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Editors;


namespace Visa2026.Module.BusinessObjects
{
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

        [MaxLength(255)]
        public virtual string Description { get; set; }


        [MaxLength(5)]
        [RuleRequiredField]
        [ModelDefault("AllowEdit", "True")]
        public override string Code { get; set; }

        [FieldSize(FieldSizeAttribute.Unlimited)]
        [EditorAlias("RichText")]
        public virtual string Content { get; set; }


        public virtual Ministry Ministry { get; set; }

        public virtual WorkPermitLocation WorkPermitLocation { get; set; }

        public virtual BorderZone BorderZone { get; set; }

        [InverseProperty(nameof(ProjectContractImage.ProjectContract))]
        [Aggregated]
        public virtual IList<ProjectContractImage> Images { get; set; }

        [InverseProperty(nameof(ProjectContractDocument.ProjectContract))]
        [Aggregated]
        public virtual IList<ProjectContractDocument> Documents { get; set; }
    }
}