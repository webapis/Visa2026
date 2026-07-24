using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Invitations In Process from vw_rd_invitation_in_process
/// (one row per invitation-issuing Application without a linked Invitation; Status = progress state).
/// </summary>
[Browsable(false)]
public class VwRdInvitationInProcess
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string ApplicationNumber { get; set; }
    public virtual DateTime? ApplicationDate { get; set; }
    public virtual string StatusLabel { get; set; }
    public virtual string StatusCssClass { get; set; }
    public virtual string ProgressStateCode { get; set; }
    public virtual bool IsArchived { get; set; }
}