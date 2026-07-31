using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard: valid visas from vw_rd_visa_by_category (Status = VisaCategory only).
/// </summary>
[Browsable(false)]
public class VwRdVisaByCategory
{
    [Key]
    public virtual Guid ID { get; set; }
    public virtual Guid? PersonOid { get; set; }
    public virtual string PersonName { get; set; }
    public virtual string ProjectName { get; set; }
    public virtual string ProjectNameRaw { get; set; }
    public virtual string ProjectNameTm { get; set; }
    public virtual int PersonRoleCode { get; set; }
    public virtual string VisaNumber { get; set; }
    public virtual DateTime? ExpirationDate { get; set; }
    public virtual string CategoryLabel { get; set; }
    public virtual string StatusLabel { get; set; }
    public virtual string StatusCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}