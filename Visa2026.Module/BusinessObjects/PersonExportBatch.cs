using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

public enum PersonExportBatchStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>
/// Background director hand-over export for one <see cref="Person"/>: the dossier document plus a
/// merged PDF per document-copies record. See <c>docs/PERSON_DOSSIER.md</c> phase 4.
/// </summary>
/// <remarks>
/// Deliberately separate from <see cref="PdfGenerationBatch"/>, which carries ministry slot
/// semantics (per-slot include flags, application item keys) that mean nothing for a person.
/// </remarks>
[DefaultClassOptions]
[NavigationItem(false)]
[DefaultProperty(nameof(DisplayName))]
public class PersonExportBatch : BaseObject
{
    public PersonExportBatch()
    {
        CreatedOnUtc = DateTime.UtcNow;
        Status = PersonExportBatchStatus.Queued;
    }

    [NotMapped]
    [Browsable(false)]
    public string DisplayName => $"Person export ({Status}) — {PersonDisplayName}";

    [RuleRequiredField]
    public virtual DateTime CreatedOnUtc { get; set; }

    [MaxLength(256)]
    public virtual string RequestedBy { get; set; }

    /// <summary>BCP-47 UI culture when the batch was queued, so the dossier document is generated in the officer's language.</summary>
    [MaxLength(10)]
    public virtual string RequestedCulture { get; set; }

    public virtual PersonExportBatchStatus Status { get; set; }

    public virtual Guid? PersonID { get; set; }

    [Browsable(false)]
    public virtual Person Person { get; set; }

    /// <summary>Copied at enqueue time so the toast and ZIP name survive later edits to the person.</summary>
    [MaxLength(512)]
    public virtual string PersonDisplayName { get; set; }

    /// <summary>Document-copies records to merge, plus one for the dossier document itself.</summary>
    public virtual int TotalRecords { get; set; }

    public virtual int ProcessedRecords { get; set; }

    [MaxLength(1024)]
    public virtual string ErrorMessage { get; set; }

    /// <summary>
    /// Same text as <c>EXPORT_NOTES.txt</c> in the ZIP: records that produced no readable PDF, or an
    /// explicit no-gaps line, so a director can tell an empty folder from a failed conversion.
    /// </summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string ExportNotes { get; set; }

    [ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData ZipFile { get; set; }
}
