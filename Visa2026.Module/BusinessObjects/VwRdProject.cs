using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard project chip row from vw_rd_projects.
/// One row per (ProjectContract, PersonRole) with non-archived people count.
/// </summary>
[Browsable(false)]
[PrimaryKey(nameof(ProjectOid), nameof(PersonRoleCode))]
public class VwRdProject
{
    public virtual Guid ProjectOid { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual long PersonCount { get; set; }
}