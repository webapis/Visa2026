using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    [DisplayName("Image")]
    [NavigationItem("Images")]
    public class InvitationImage : ImageBase
    {
        [RuleRequiredField]
        public virtual Invitation Invitation { get; set; }
    }
}