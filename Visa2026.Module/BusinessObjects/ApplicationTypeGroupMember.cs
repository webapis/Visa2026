using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>Join row: <see cref="ApplicationType"/> membership in an <see cref="ApplicationTypeGroup"/>.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class ApplicationTypeGroupMember : BaseObject
{
    [Browsable(false)]
    public virtual Guid ApplicationTypeGroupId { get; set; }

    [Browsable(false)]
    public virtual ApplicationTypeGroup ApplicationTypeGroup { get; set; } = null!;

    [RuleRequiredField]
    [XafDisplayName("Application Type")]
    public virtual ApplicationType ApplicationType { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ApplicationTypeId { get; set; }
}