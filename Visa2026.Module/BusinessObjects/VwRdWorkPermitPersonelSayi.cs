using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Personel sayi from vw_rd_work_permit_personel_sayi (one row per department).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(DepartmentLabel))]
[ModelDefault("Caption", "Personel sayi")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdWorkPermitPersonelSayi
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [ModelDefault("Caption", "Projeler")]
    public virtual string DepartmentLabel { get; set; }

    [ModelDefault("Caption", "Aktiv Personel sayi")]
    public virtual int ActiveCount { get; set; }

    [ModelDefault("Caption", "Aktiv olmayan personel sayi")]
    public virtual int InactiveCount { get; set; }

    [Browsable(false)]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }

    [Browsable(false)]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }
}