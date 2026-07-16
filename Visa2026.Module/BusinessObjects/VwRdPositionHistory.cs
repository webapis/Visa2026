using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Position History row from vw_rd_position_history.
/// One EmployeePositionHistory per row (by-status / by-position).
/// </summary>
[Browsable(false)]
public class VwRdPositionHistory
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string PositionName { get; set; }
    public virtual DateTime? StartDate { get; set; }
    public virtual string StatusLabel { get; set; }
    public virtual string StatusCssClass { get; set; }
    public virtual string PositionLabel { get; set; }
    public virtual bool IsArchived { get; set; }
}