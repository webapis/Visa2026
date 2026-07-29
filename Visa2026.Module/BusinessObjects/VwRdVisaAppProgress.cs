using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Visa On Extension / Extension Result row from <c>vw_rd_visa_app_progress</c>.
/// One ApplicationItem per row (visa-extension types with CurrentVisa).
/// Shared by dashboard preview and Open ListView (not <see cref="VisaExtensionStatus"/>).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(ApplicationNumber))]
[ModelDefault("Caption", "Visa Extension (Dashboard)")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdVisaAppProgress
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? ApplicationOid { get; set; }

    [ForeignKey(nameof(ApplicationOid))]
    [ModelDefault("Caption", "Application")]
    public virtual Application Application { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "Person")]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual Guid? ExpiringVisaID { get; set; }

    [ForeignKey(nameof(ExpiringVisaID))]
    [ModelDefault("Caption", "Expiring Visa")]
    public virtual Visa ExpiringVisa { get; set; }

    [Browsable(false)]
    public virtual Guid? PassportID { get; set; }

    [ForeignKey(nameof(PassportID))]
    [ModelDefault("Caption", "Passport")]
    public virtual Passport Passport { get; set; }

    [Browsable(false)]
    public virtual string PassportNumber { get; set; }

    [Browsable(false)]
    public virtual Guid? CurrentStateID { get; set; }

    [ForeignKey(nameof(CurrentStateID))]
    [ModelDefault("Caption", "Current State")]
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

    [Browsable(false)]
    public virtual string ApplicationNumber { get; set; }

    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    [ModelDefault("EditMask", "dd.MM.yyyy")]
    public virtual DateTime? ApplicationDate { get; set; }

    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    [ModelDefault("EditMask", "dd.MM.yyyy")]
    [ModelDefault("Caption", "Status Date")]
    public virtual DateTime? StatusDate { get; set; }

    [Browsable(false)]
    public virtual string ProgressStateCode { get; set; }

    [ModelDefault("Caption", "Process State")]
    public virtual string ProgressStateLabel { get; set; }

    [Browsable(false)]
    public virtual string ProgressStateCssClass { get; set; }

    [ModelDefault("DisplayFormat", "{0} days")]
    [ModelDefault("Caption", "Days Remaining")]
    public virtual int? DaysRemainingOnVisa { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }
}