using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Join row: which <see cref="ProjectContract"/> a <see cref="UserReportTemplate"/> applies to
/// (exact match on <see cref="Application.ProjectContract"/>).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class UserReportTemplateProjectContract : BaseObject
{
    [Browsable(false)]
    public virtual Guid UserReportTemplateId { get; set; }

    [Browsable(false)]
    public virtual UserReportTemplate UserReportTemplate { get; set; } = null!;

    [RuleRequiredField]
    [XafDisplayName("Project Contract")]
    public virtual ProjectContract ProjectContract { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ProjectContractId { get; set; }
}
