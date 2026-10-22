using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: kota oranımız summary counts from vw_rd_work_permit_kota_oranimiz.
/// Local headcount and ORAN come from <see cref="WorkPermitQuotaSettings"/>.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[ModelDefault("Caption", "kota oranımız")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdWorkPermitKotaOranimiz
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [ModelDefault("Caption", "TARİHİ")]
    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    public virtual DateTime? ReportDate { get; set; }

    [ModelDefault("Caption", "YABANCI PERSONEL SAYISI")]
    public virtual int ForeignPersonnelCount { get; set; }

    [ModelDefault("Caption", "İŞLEMDE OLANLAR")]
    public virtual int InProcessCount { get; set; }
}