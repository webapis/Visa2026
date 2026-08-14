using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard passport row from vw_rd_passport.
/// One row per ApplicationRosterMergeLine with CurrentPassport set (loader keeps one last passport per person by IssueDate).
/// ApplicationDate comes from the parent ApplicationProfileInstance (dashboard date filter).
/// </summary>
[Browsable(false)]
public class VwRdPassport
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PassportOid { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string PassportNumber { get; set; }
    public virtual DateTime? IssueDate { get; set; }
    public virtual DateTime? ExpirationDate { get; set; }
    public virtual DateTime? ApplicationDate { get; set; }
    public virtual string TypeLabel { get; set; }
    public virtual string CitizenshipLabel { get; set; }
    public virtual string ValidityLabel { get; set; }
    public virtual string ValidityCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}