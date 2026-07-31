using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Application category row from vw_rd_application.
/// One header Application per row (by-progress / by-type).
/// </summary>
[Browsable(false)]
public class VwRdApplication
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string ApplicationNumber { get; set; }
    public virtual DateTime? ApplicationDate { get; set; }
    public virtual string ProgressStateLabel { get; set; }
    public virtual string ProgressStateCssClass { get; set; }
    public virtual string ProgressStateCode { get; set; }
    public virtual string TypeLabel { get; set; }
    public virtual bool IsArchived { get; set; }
}
