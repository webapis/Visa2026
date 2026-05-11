using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using DevExpress.Persistent.Validation;

namespace Visa2026.Module.BusinessObjects;

public enum PdfGenerationBatchStatus
{
    Queued = 0,
    Running = 1,
    Completed = 2,
    Failed = 3
}

[DefaultClassOptions]
[NavigationItem("Application")]
[DefaultProperty(nameof(DisplayName))]
public class PdfGenerationBatch : BaseObject, IObjectSpaceLink
{
    public PdfGenerationBatch()
    {
        CreatedOnUtc = DateTime.UtcNow;
        Status = PdfGenerationBatchStatus.Queued;
        // Default “full application package” (see docs/APPLICATION_DIPLOMA_PACKAGE_PLAN.md §4.2).
        IncludeDiplomaFiles = true;
        DiplomaScope = PdfBatchDiplomaScope.AllEducations;
        SupportingZipMergeOption = PdfSupportingZipMergeOption.IndividualFilesAndMergedPdfs;
        IncludeMergedDiplomaPdf = false;
        IncludePassportCopies = true;
        IncludeVisaCopies = true;
        IncludeMedicalRecordCopies = true;
        IncludeAddressOfResidenceCopies = true;
        IncludeWorkPermitCopies = true;
        IncludeInvitationCopies = true;
        IncludeFamilyRelationshipCopies = true;
    }

    [NotMapped]
    [Browsable(false)]
    public string DisplayName => $"PDF Batch ({Status}) — {TotalItems} item(s)";

    [RuleRequiredField]
    public virtual DateTime CreatedOnUtc { get; set; }

    [MaxLength(256)]
    public virtual string RequestedBy { get; set; }

    public virtual PdfGenerationBatchStatus Status { get; set; }

    public virtual int TotalItems { get; set; }

    public virtual int ProcessedItems { get; set; }

    [MaxLength(1024)]
    public virtual string ErrorMessage { get; set; }

    /// <summary>
    /// When background PDF generation skips <see cref="PdfFormMapping"/> Property/Expression rules because
    /// <c>PdfMappingSourceGate</c> does not allow them for the application (same intent as on-screen visibility),
    /// those skips are summarized here so clerks can tell intentional blanks from mapping errors.
    /// </summary>
    [FieldSize(FieldSizeAttribute.Unlimited)]
    public virtual string PdfMappingVisibilityNotes { get; set; }

    /// <summary>Pack <see cref="EducationDocument"/> under <c>Diplomas/</c> when true.</summary>
    public virtual bool IncludeDiplomaFiles { get; set; }

    /// <summary>Which education rows to include when <see cref="IncludeDiplomaFiles"/> is true.</summary>
    public virtual PdfBatchDiplomaScope DiplomaScope { get; set; }

    /// <summary>
    /// How supporting documents are written into the ZIP: per-file entries only, merged batch PDFs only, or both.
    /// <see cref="IncludeMergedDiplomaPdf"/> is honored when this is not <see cref="PdfSupportingZipMergeOption.IndividualFilesOnly"/>.
    /// </summary>
    public virtual PdfSupportingZipMergeOption SupportingZipMergeOption { get; set; }

    /// <summary>When true with diplomas, also emit <c>_AllDiplomas_merged.pdf</c> for PDF sources only (no image conversion). Ignored when <see cref="SupportingZipMergeOption"/> is <see cref="PdfSupportingZipMergeOption.IndividualFilesOnly"/>.</summary>
    public virtual bool IncludeMergedDiplomaPdf { get; set; }

    public virtual bool IncludePassportCopies { get; set; }

    public virtual bool IncludeVisaCopies { get; set; }

    public virtual bool IncludeMedicalRecordCopies { get; set; }

    public virtual bool IncludeAddressOfResidenceCopies { get; set; }

    public virtual bool IncludeWorkPermitCopies { get; set; }

    public virtual bool IncludeInvitationCopies { get; set; }

    public virtual bool IncludeFamilyRelationshipCopies { get; set; }

    [RuleRequiredField]
    [MaxLength(512)]
    public virtual string ItemKeyType { get; set; }

    [RuleRequiredField]
    public virtual string ItemKeysJson { get; set; }

    [ExpandObjectMembers(ExpandObjectMembers.Never)]
    public virtual FileData ZipFile { get; set; }

    #region IObjectSpaceLink
    [NotMapped]
    public IObjectSpace ObjectSpace { get; set; }
    #endregion
}
