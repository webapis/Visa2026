using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects
{
    [DefaultClassOptions]
    public class InvitationDocument : DocumentBase
    {
        [RuleRequiredField]
        public virtual Invitation Invitation { get; set; }
    }
}