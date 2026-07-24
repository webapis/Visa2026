using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Ready Invitations from vw_rd_invitation_ready
/// (valid unused InvitationItems; Status = Project or Period · Category · Type).
/// </summary>
[Browsable(false)]
public class VwRdInvitationReady
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
    public virtual string VisaPeriodLabel { get; set; }
    public virtual string VisaCategoryLabel { get; set; }
    public virtual string VisaTypeLabel { get; set; }
    public virtual string StatusLabel { get; set; }
    public virtual string StatusCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}
