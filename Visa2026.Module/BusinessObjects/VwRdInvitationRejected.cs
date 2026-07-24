using System;
using System.ComponentModel;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Invitations Rejected from vw_rd_invitation_rejected
/// (RejectionItems union PROCESS_REJECTED apps without Rejection; Status = Project).
/// </summary>
[Browsable(false)]
[PrimaryKey(nameof(SourceKind), nameof(ID))]
public class VwRdInvitationRejected
{
    public virtual Guid ID { get; set; }
    public virtual string SourceKind { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string DocumentNumber { get; set; }
    public virtual DateTime? RecordDate { get; set; }
    public virtual string StatusLabel { get; set; }
    public virtual string StatusCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}