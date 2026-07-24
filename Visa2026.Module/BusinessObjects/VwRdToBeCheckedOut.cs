using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: valid visas expiring within 1 week without check-out app (vw_rd_to_be_checked_out).
/// </summary>
[Browsable(false)]
public class VwRdToBeCheckedOut
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string VisaNumber { get; set; }
    public virtual DateTime? VisaExpirationDate { get; set; }
    public virtual int DaysRemaining { get; set; }
    public virtual string ExpiryBucketLabel { get; set; }
    public virtual string ExpiryBucketCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}
