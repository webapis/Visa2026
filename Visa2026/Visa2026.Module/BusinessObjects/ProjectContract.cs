using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
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
    public class ProjectContract : BaseObject
    {
        [MaxLength(100)]

        [RuleRequiredField]
        public virtual string Name { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }


        [MaxLength(5)]
        [RuleRequiredField]
        public virtual string Code { get; set; }

      [FieldSize(FieldSizeAttribute.Unlimited)]
        [EditorAlias("RichText")]
        public virtual string Content { get; set; }


        public virtual Ministry Ministry { get; set; }

        public virtual WorkPermitLocation WorkPermitLocation { get; set; }

        public virtual BorderZone BorderZone { get; set; }
    }
}