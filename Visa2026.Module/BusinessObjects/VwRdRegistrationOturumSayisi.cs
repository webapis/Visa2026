using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: Oturum sayısı from vw_rd_registration_oturum_sayisi (one row per department).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(DepartmentLabel))]
[ModelDefault("Caption", "Oturum sayısı")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdRegistrationOturumSayisi
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [ModelDefault("Caption", "Projeler")]
    public virtual string DepartmentLabel { get; set; }

    [ModelDefault("Caption", "Balkan kampda yasayanlar")]
    public virtual int BalkanKampCount { get; set; }

    [ModelDefault("Caption", "Balkan adresde yasayanlar")]
    public virtual int BalkanAdresCount { get; set; }

    [ModelDefault("Caption", "Aşgabat")]
    public virtual int AsgabatCount { get; set; }

    [Browsable(false)]
    public virtual int AsgabatKampCount { get; set; }

    [Browsable(false)]
    public virtual int AsgabatAdresCount { get; set; }

    [ModelDefault("Caption", "Ahal")]
    public virtual int AhalCount { get; set; }

    [Browsable(false)]
    public virtual int AhalKampCount { get; set; }

    [Browsable(false)]
    public virtual int AhalAdresCount { get; set; }

    [ModelDefault("Caption", "Lebap")]
    public virtual int LebapCount { get; set; }

    [Browsable(false)]
    public virtual int LebapKampCount { get; set; }

    [Browsable(false)]
    public virtual int LebapAdresCount { get; set; }

    [ModelDefault("Caption", "Mary adres")]
    public virtual int MaryAdresCount { get; set; }

    [ModelDefault("Caption", "Mary - kamp şatlık-kamp")]
    public virtual int MaryKampCount { get; set; }

    [Browsable(false)]
    public virtual int TotalCount { get; set; }

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