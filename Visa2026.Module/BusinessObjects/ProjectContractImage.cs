using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    public class ProjectContractImage : ImageBase
    {
        [RuleRequiredField]
        public virtual ProjectContract ProjectContract { get; set; }
    }
}