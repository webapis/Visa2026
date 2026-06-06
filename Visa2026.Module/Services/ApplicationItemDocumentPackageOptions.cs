using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>Package ZIP options for Document copies (mirrors <see cref="PdfBatchEnqueueOptions"/>).</summary>
public sealed class ApplicationItemDocumentPackageOptions
{
    public bool IncludeDiplomaFiles { get; set; } = true;

    public PdfBatchDiplomaScope DiplomaScope { get; set; } = PdfBatchDiplomaScope.AllEducations;

    public PdfSupportingZipMergeOption SupportingZipMergeOption { get; set; } =
        PdfSupportingZipMergeOption.IndividualFilesAndMergedPdfs;

    public bool IncludeMergedDiplomaPdf { get; set; }

    public bool IncludePassportCopies { get; set; } = true;

    public bool IncludeVisaCopies { get; set; } = true;

    public bool IncludeMedicalRecordCopies { get; set; } = true;

    public bool IncludeAddressOfResidenceCopies { get; set; } = true;

    public bool IncludeWorkPermitCopies { get; set; } = true;

    public bool IncludeInvitationCopies { get; set; } = true;

    public bool IncludeFamilyRelationshipCopies { get; set; } = true;

    public bool ShowMergedDiplomaOption =>
        IncludeDiplomaFiles
        && SupportingZipMergeOption != PdfSupportingZipMergeOption.IndividualFilesOnly;

    public static ApplicationItemDocumentPackageOptions CreateDefaults() => new();

    public void ResetToDefaults()
    {
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

    public void ApplyTo(PdfBatchEnqueueOptions target)
    {
        target.IncludeDiplomaFiles = IncludeDiplomaFiles;
        target.DiplomaScope = DiplomaScope;
        target.SupportingZipMergeOption = SupportingZipMergeOption;
        target.IncludeMergedDiplomaPdf = ShowMergedDiplomaOption && IncludeMergedDiplomaPdf;
        target.IncludePassportCopies = IncludePassportCopies;
        target.IncludeVisaCopies = IncludeVisaCopies;
        target.IncludeMedicalRecordCopies = IncludeMedicalRecordCopies;
        target.IncludeAddressOfResidenceCopies = IncludeAddressOfResidenceCopies;
        target.IncludeWorkPermitCopies = IncludeWorkPermitCopies;
        target.IncludeInvitationCopies = IncludeInvitationCopies;
        target.IncludeFamilyRelationshipCopies = IncludeFamilyRelationshipCopies;
    }
}
