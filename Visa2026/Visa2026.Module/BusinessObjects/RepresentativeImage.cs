using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    public class RepresentativeImage : ImageBase
    {
        [RuleRequiredField]
        public virtual Representative Representative { get; set; }
    }
}