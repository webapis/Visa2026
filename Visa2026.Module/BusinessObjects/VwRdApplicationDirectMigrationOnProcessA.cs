using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Application (direct migration) — On Process (A)
/// from <c>vw_rd_application_direct_migration_on_process_a</c> (one row per ApplicationItem).
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(ApplicationNumber))]
[ModelDefault("Caption", "On Process (A)")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdApplicationDirectMigrationOnProcessA
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
    [Browsable(false)]
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

    [ModelDefault("Caption", "App Type")]
    public virtual string ApplicationTypeLabel { get; set; }

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
}