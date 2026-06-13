using System;
using System.ComponentModel;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>One ordered ministry leg on a <see cref="ProjectContract"/> approval process.</summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(Sequence))]
public class ProjectContractMinistryLeg : BaseObject
{
    [Browsable(false)]
    public virtual Guid ProjectContractId { get; set; }

    [Browsable(false)]
    public virtual ProjectContract ProjectContract { get; set; } = null!;

    [RuleRequiredField]
    public virtual int? Sequence { get; set; }

    [RuleRequiredField]
    public virtual ApprovingMinistry ApprovingMinistry { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ApprovingMinistryId { get; set; }
}
