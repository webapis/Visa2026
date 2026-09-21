using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: last registration application per visa (vw_rd_registration).
/// Visa.IsCancelled and expiry are ignored (still checked-in until Check-Out). Type tabs use ProgressStateLabel;
/// Expiring State / Check in by City use expiry/city buckets (one last visa per person in C#).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(VisaNumber))]
[ModelDefault("Caption", "Registration (Dashboard)")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdRegistration
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "Person")]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }
    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }
    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }
    public virtual string VisaNumber { get; set; }
    public virtual DateTime? VisaExpirationDate { get; set; }
    public virtual string ApplicationNumber { get; set; }
    public virtual DateTime? ApplicationDate { get; set; }
    [Browsable(false)]
    public virtual string ApplicationTypeName { get; set; }
    public virtual string ApplicationTypeLabel { get; set; }
    public virtual string ProgressStateLabel { get; set; }
    [Browsable(false)]
    public virtual string ProgressStateCssClass { get; set; }
    [Browsable(false)]
    public virtual string ProgressStateCode { get; set; }
    public virtual int DaysRemaining { get; set; }
    public virtual string ExpiryBucketLabel { get; set; }
    [Browsable(false)]
    public virtual string ExpiryBucketCssClass { get; set; }
    [Browsable(false)]
    public virtual bool IsArchived { get; set; }
    /// <summary>City from last registration roster line resolved address link.</summary>
    public virtual string CityLabel { get; set; }
}
