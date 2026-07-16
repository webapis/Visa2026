using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard person-type tab count from vw_rd_person_roles.
/// One row per PersonRole (non-archived people).
/// </summary>
[Browsable(false)]
public class VwRdPersonRole
{
    [Key]
    public virtual int PersonRoleCode { get; set; }
    public virtual long PersonCount { get; set; }
}