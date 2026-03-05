using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    public class AddressOfResidenceImage : ImageBase
    {
        [RuleRequiredField]
        public virtual AddressOfResidence AddressOfResidence { get; set; }
    }
}