using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Report Dashboard Incomplete persons from <c>vw_rd_incomplete_persons_by_missing_area</c>.
/// One row per incomplete Person; chart buckets are built in the loader from flag columns.
/// </summary>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(PersonName))]
[ModelDefault("Caption", "Incomplete persons")]
[ModelDefault("AllowEdit", "False")]
[ModelDefault("AllowNew", "False")]
[ModelDefault("AllowDelete", "False")]
public class VwRdIncompletePersonsByMissingArea
{
    [Key]
    [Browsable(false)]
    public virtual Guid ID { get; set; }

    [Browsable(false)]
    public virtual Guid? PersonOid { get; set; }

    [ForeignKey(nameof(PersonOid))]
    [ModelDefault("Caption", "Person")]
    public virtual Person Person { get; set; }

    [Browsable(false)]
    public virtual string PersonName { get; set; }

    [Browsable(false)]
    public virtual string ProjectName { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameRaw { get; set; }

    [Browsable(false)]
    public virtual string ProjectNameTm { get; set; }

    [Browsable(false)]
    public virtual int PersonRoleCode { get; set; }

    [ModelDefault("Caption", "Person type")]
    public virtual string PersonTypeLabel { get; set; }

    [ModelDefault("Caption", "Missing areas")]
    public virtual string MissingAreasLabel { get; set; }

    [ModelDefault("Caption", "Notes")]
    public virtual string IncompleteNotes { get; set; }

    [Browsable(false)]
    public virtual DateTime? IncompleteMarkedOn { get; set; }

    [Browsable(false)]
    public virtual string IncompleteMarkedBy { get; set; }

    [ModelDefault("Caption", "Marked")]
    public virtual string MarkedLabel { get; set; }

    [Browsable(false)]
    public virtual bool MissingPersonalData { get; set; }

    [Browsable(false)]
    public virtual bool MissingPassport { get; set; }

    [Browsable(false)]
    public virtual bool MissingCv { get; set; }

    [Browsable(false)]
    public virtual bool MissingPhoto { get; set; }

    [Browsable(false)]
    public virtual bool MissingEducation { get; set; }

    [Browsable(false)]
    public virtual bool MissingMedical { get; set; }

    [Browsable(false)]
    public virtual bool MissingAddress { get; set; }

    [Browsable(false)]
    public virtual bool MissingFamilyDocs { get; set; }

    [Browsable(false)]
    public virtual bool MissingOther { get; set; }

    [Browsable(false)]
    public virtual bool IsArchived { get; set; }
}