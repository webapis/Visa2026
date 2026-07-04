using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Join: which <see cref="ApprovalLegProfile"/> options are allowed for a <see cref="ProjectContract"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
public class ProjectContractApprovalLegProfile : BaseObject
{
    [Browsable(false)]
    public virtual Guid ProjectContractId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ProjectContractId))]
    public virtual ProjectContract ProjectContract { get; set; } = null!;

    [Browsable(false)]
    public virtual Guid ApprovalLegProfileId { get; set; }

    [RuleRequiredField]
    [ForeignKey(nameof(ApprovalLegProfileId))]
    public virtual ApprovalLegProfile ApprovalLegProfile { get; set; } = null!;

    public override void OnSaving()
    {
        if (ProjectContract != null)
            ProjectContractId = ProjectContract.ID;
        if (ApprovalLegProfile != null)
            ApprovalLegProfileId = ApprovalLegProfile.ID;
        base.OnSaving();
    }
}
