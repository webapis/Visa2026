using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Education row from vw_rd_education.
/// One Education per row (by-level / by-country / by-specialty).
/// </summary>
[Browsable(false)]
public class VwRdEducation
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string InstitutionName { get; set; }
    public virtual string GraduationYear { get; set; }
    public virtual string LevelLabel { get; set; }
    public virtual string CountryLabel { get; set; }
    public virtual string SpecialtyLabel { get; set; }
    /// <summary>Mirror of <see cref="Person.IsArchived"/> on the joined person (vw_rd_education).</summary>
    public virtual bool IsArchived { get; set; }
}