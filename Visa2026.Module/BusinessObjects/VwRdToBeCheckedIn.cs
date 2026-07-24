using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: valid visas with no registration CurrentVisa link (vw_rd_to_be_checked_in).
/// Chart uses days since latest ExternalArrival.
/// </summary>
[Browsable(false)]
public class VwRdToBeCheckedIn
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
    public virtual DateTime? EntryDate { get; set; }
    public virtual int? DaysSinceEntry { get; set; }
    public virtual string EntryBucketLabel { get; set; }
    public virtual string EntryBucketCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}
