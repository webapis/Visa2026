using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: valid visas with no registration CurrentVisa link (vw_rd_to_be_checked_in).
/// Chart uses days since latest ExternalArrival.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(VisaNumber))]
[ModelDefault("Caption", "To Be Checked In")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdToBeCheckedIn
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
    public virtual DateTime? EntryDate { get; set; }
    public virtual int? DaysSinceEntry { get; set; }
    public virtual string EntryBucketLabel { get; set; }
    [Browsable(false)]
    public virtual string EntryBucketCssClass { get; set; }
    [Browsable(false)]
    public virtual bool IsArchived { get; set; }
}