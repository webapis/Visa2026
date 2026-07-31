using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Used Invitations from vw_rd_invitation_used
/// (InvitationItems with IsUsed; Status = Project).
/// </summary>
[Browsable(false)]
public class VwRdInvitationUsed
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string InvitationNumber { get; set; }
    public virtual DateTime? ExpirationDate { get; set; }
    public virtual DateTime? IssuedDate { get; set; }
    public virtual string StatusLabel { get; set; }
    public virtual string StatusCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}