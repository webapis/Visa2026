using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: last registration application per not-expired visa (vw_rd_registration).
/// Type tabs use ProgressStateLabel; Expiring State uses ExpiryBucketLabel (one last visa per person in C#).
/// </summary>
[Browsable(false)]
public class VwRdRegistration
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
    public virtual string ApplicationNumber { get; set; }
    public virtual DateTime? ApplicationDate { get; set; }
    public virtual string ApplicationTypeName { get; set; }
    public virtual string ApplicationTypeLabel { get; set; }
    public virtual string ProgressStateLabel { get; set; }
    public virtual string ProgressStateCssClass { get; set; }
    public virtual string ProgressStateCode { get; set; }
    public virtual int DaysRemaining { get; set; }
    public virtual string ExpiryBucketLabel { get; set; }
    public virtual string ExpiryBucketCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
    /// <summary>City from last registration ApplicationItem.CurrentAddressOfResidence.</summary>
    public virtual string CityLabel { get; set; }
}
