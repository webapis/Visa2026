using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

[DefaultClassOptions]
[NavigationItem("Documents")]
[XafDisplayName("Work permit location grant document")]
public class WorkPermitLocationGrantDocument : DocumentBase
{
    [RuleRequiredField]
    public virtual WorkPermitLocationGrant Grant { get; set; }
}