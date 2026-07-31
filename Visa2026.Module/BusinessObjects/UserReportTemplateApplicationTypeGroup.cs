using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Join row: which <see cref="ApplicationTypeGroup"/> a <see cref="UserReportTemplate"/> applies to.
/// Combined with <see cref="UserReportTemplateApplicationType"/> (union) for Resminamalar visibility.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class UserReportTemplateApplicationTypeGroup : BaseObject
{
    [Browsable(false)]
    public virtual Guid UserReportTemplateId { get; set; }

    [Browsable(false)]
    public virtual UserReportTemplate UserReportTemplate { get; set; } = null!;

    [RuleRequiredField]
    [XafDisplayName("Application Type Group")]
    public virtual ApplicationTypeGroup ApplicationTypeGroup { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ApplicationTypeGroupId { get; set; }
}