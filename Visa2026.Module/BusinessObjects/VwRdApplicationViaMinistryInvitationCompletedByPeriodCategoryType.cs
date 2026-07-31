using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Application (via ministry) — Invitation Completed (V)
/// from <c>vw_rd_application_via_ministry_invitation_completed_by_period_category_type</c> (one row per ApplicationItem).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(ApplicationNumber))]
[ModelDefault("Caption", "Invitation Completed (V)")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdApplicationViaMinistryInvitationCompletedByPeriodCategoryType : IVwRdApplicationViaMinistryRow
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? ApplicationOid { get; set; }

    [ForeignKey(nameof(ApplicationOid))]
    [ModelDefault("Caption", "App #")]
    public virtual Application Application { get; set; }

    [Browsable(false)]
    public virtual Guid? ApplicationItemOid { get; set; }

    [ForeignKey(nameof(ApplicationItemOid))]
    [ModelDefault("Caption", "Application Item")]
    public virtual ApplicationItem ApplicationItem { get; set; }

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
    public virtual Guid? InvitationOid { get; set; }

    /// <summary>
    /// Invitation this application issued for <see cref="Person"/> — proof the application
    /// process produced one. Empty for rejected / cancelled applications.
    /// </summary>
    [ForeignKey(nameof(InvitationOid))]
    [ModelDefault("Caption", "Invitation")]
    public virtual Invitation Invitation { get; set; }

    [Browsable(false)]
    public virtual string InvitationNumber { get; set; }

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
