using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: valid work permits from vw_rd_work_permit
/// (Status = closed days-to-expiry bucket; multiple valid items per person allowed).
/// </summary>
[Browsable(false)]
public class VwRdWorkPermit
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string WorkPermitNumber { get; set; }
    public virtual DateTime? ExpirationDate { get; set; }
    public virtual int DaysRemaining { get; set; }
    public virtual string ValidityLabel { get; set; }
    public virtual string ValidityCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}
