using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard ApplicationProfileInstance (via ministry) — Visa Extension on Process (P)
/// from <c>vw_rd_application_via_ministry_visa_extension_on_process</c> (one row per ApplicationRosterMergeLine).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(ApplicationNumber))]
[ModelDefault("Caption", "Visa Extension on Process (P)")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdApplicationViaMinistryVisaExtensionOnProcess : IVwRdApplicationViaMinistryRow
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? ApplicationProfileInstanceOid { get; set; }

    [ForeignKey(nameof(ApplicationProfileInstanceOid))]
    [ModelDefault("Caption", "App #")]
    public virtual ApplicationProfileInstance ApplicationProfileInstance { get; set; }

    [Browsable(false)]
    public virtual Guid? ApplicationItemOid { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "Person")]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual Guid? CurrentStateID { get; set; }

    [ForeignKey(nameof(CurrentStateID))]
    [Browsable(false)]
    public virtual ApplicationState CurrentState { get; set; }

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

    [ModelDefault("Caption", "Position")]
    public virtual string PositionLabel { get; set; }

    [ModelDefault("Caption", "App Type")]
    public virtual string ApplicationTypeLabel { get; set; }
    [ModelDefault("Caption", "Visa Period")]
    public virtual string VisaPeriodLabel { get; set; }

    [ModelDefault("Caption", "Visa Type")]
    public virtual string VisaTypeLabel { get; set; }

    [Browsable(false)]
    public virtual string ApplicationNumber { get; set; }

    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    [ModelDefault("EditMask", "dd.MM.yyyy")]
    [ModelDefault("Caption", "App Date")]
    public virtual DateTime? ApplicationDate { get; set; }

    [Browsable(false)]
    public virtual string ProgressStateCode { get; set; }

    [ModelDefault("Caption", "State")]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }

    [Browsable(false)]
    public virtual string PeriodLabel { get; set; }

    [Browsable(false)]
    public virtual string CategoryLabel { get; set; }

    [Browsable(false)]
    public virtual string TypeLabel { get; set; }
}
