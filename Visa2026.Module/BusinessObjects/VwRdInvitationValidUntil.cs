using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Invitation Valid Until from vw_rd_invitation_valid_until
/// (valid unused InvitationItems; Status = remaining-time bucket).
/// </summary>
[Browsable(false)]
public class VwRdInvitationValidUntil
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
    public virtual int DaysRemaining { get; set; }
    public virtual string ValidityLabel { get; set; }
    public virtual string ValidityCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}