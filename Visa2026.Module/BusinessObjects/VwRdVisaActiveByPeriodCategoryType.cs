using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Active Visa (V) from <c>vw_rd_visa_active_by_period_category_type</c>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(VisaNumber))]
[ModelDefault("Caption", "Active Visa (V)")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdVisaActiveByPeriodCategoryType
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
    public virtual Guid? PassportID { get; set; }

    [ForeignKey(nameof(PassportID))]
    [ModelDefault("Caption", "Passport")]
    public virtual Passport Passport { get; set; }

    [ForeignKey(nameof(ID))]
    [ModelDefault("Caption", "Visa #")]
    public virtual Visa Visa { get; set; }

    [Browsable(false)]
    public virtual string PassportNumber { get; set; }

    [Browsable(false)]
    public virtual string PersonName { get; set; }

    [ModelDefault("Caption", "Project")]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }

    [Browsable(false)]
    public virtual string VisaNumber { get; set; }

    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    [ModelDefault("EditMask", "dd.MM.yyyy")]
    [ModelDefault("Caption", "Expiry")]
    public virtual DateTime? ExpirationDate { get; set; }

    [Browsable(false)]
    public virtual int PeriodDays { get; set; }

    [Browsable(false)]
    public virtual string PeriodLabel { get; set; }

    [ModelDefault("Caption", "Status")]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [ModelDefault("DisplayFormat", "{0} days")]
    [ModelDefault("Caption", "Days Remaining")]
    public virtual int DaysRemaining { get; set; }

    [Browsable(false)]
    public virtual bool IsOneLastValidPerPerson { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }
}