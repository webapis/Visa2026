using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Editors;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    public class FamilyMemberImage : BaseObject
    {
        [RuleRequiredField]
        [DataSourceCriteria("IsEmployee = false")]
        public virtual Person Person { get; set; }

        [RuleRequiredField]
        [ImageEditor(ListViewImageEditorCustomHeight = 75, DetailViewImageEditorFixedHeight = 150)]
        public virtual byte[] Image { get; set; }

        [MaxLength(255)]
        public virtual string Description { get; set; }
    }
}