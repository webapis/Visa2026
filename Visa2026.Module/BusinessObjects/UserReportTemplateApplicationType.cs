using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Join row: which <see cref="ApplicationType"/> a <see cref="UserReportTemplate"/> applies to.
/// Replaces implicit EF many-to-many so links on one template cannot clear another template.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class UserReportTemplateApplicationType : BaseObject
{
    [Browsable(false)]
    public virtual Guid UserReportTemplateId { get; set; }

    [Browsable(false)]
    public virtual UserReportTemplate UserReportTemplate { get; set; } = null!;

    [RuleRequiredField]
    [XafDisplayName("Application Type")]
    public virtual ApplicationType ApplicationType { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ApplicationTypeId { get; set; }
}
