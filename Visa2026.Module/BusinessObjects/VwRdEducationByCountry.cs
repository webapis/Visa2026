using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Education By Country row from vw_rd_education_by_country.
/// Dedicated view optimized for education-country sub-report.
/// </summary>
[Browsable(false)]
public class VwRdEducationByCountry
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
    public virtual string CountryLabel { get; set; }
    public virtual bool IsArchived { get; set; }
}
