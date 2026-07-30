using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Person search from <c>vw_rd_person_search</c>.
/// One row per Person; status buckets follow the person's current visa.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(PersonName))]
[ModelDefault("Caption", "Person search")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdPersonSearch
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "Name")]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual string PersonName { get; set; }

    [ModelDefault("Caption", "Project")]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }

    [Browsable(false)]
    public virtual string PersonTypeLabel { get; set; }

    [ModelDefault("Caption", "Personal number")]
    public virtual string PersonalNumber { get; set; }

    [ModelDefault("Caption", "Passport #")]
    public virtual string PassportNumber { get; set; }

    [Browsable(false)]
    public virtual string VisaNumber { get; set; }

    [ModelDefault("DisplayFormat", "{0:dd.MM.yyyy}")]
    [ModelDefault("EditMask", "dd.MM.yyyy")]
    [ModelDefault("Caption", "Visa expiry")]
    public virtual DateTime? VisaExpirationDate { get; set; }

    [Browsable(false)]
    public virtual string VisaExpiryLabel { get; set; }

    [ModelDefault("Caption", "Status")]
    public virtual string StatusLabel { get; set; }

    [Browsable(false)]
    public virtual string StatusCssClass { get; set; }

    /// <summary>
    /// Lowercased name + personal number + every passport number, so the Preview loader and
    /// the drill-down ListView criteria can filter on the same haystack.
    /// </summary>
    [Browsable(false)]
    public virtual string SearchText { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }
}
