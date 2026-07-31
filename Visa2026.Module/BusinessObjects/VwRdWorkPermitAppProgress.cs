using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard WorkPermit Extension / Extension Result from vw_rd_work_permit_app_progress.
/// One ApplicationItem per row (App_WP_Ext / App_Visa_and_WP_Ext with CurrentWorkPermitItem).
/// Chart Status is resolved in C# as Project · ProcessState.
/// </summary>
[Browsable(false)]
public class VwRdWorkPermitAppProgress
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? ApplicationOid { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string ApplicationNumber { get; set; }
    public virtual DateTime? ApplicationDate { get; set; }
    public virtual string ProgressStateCode { get; set; }
    public virtual string ProgressStateLabel { get; set; }
    public virtual string ProgressStateCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}