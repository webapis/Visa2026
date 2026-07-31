using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Visa State row from vw_rd_visa_state.
/// Extension Started: valid last-visa on visa-extension ApplicationItem.
/// </summary>
[Browsable(false)]
public class VwRdVisaState
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
    public virtual string StateLabel { get; set; }
    public virtual string StateCssClass { get; set; }
    public virtual bool IsArchived { get; set; }
}