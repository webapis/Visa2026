using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ConditionalAppearance;
using DevExpress.ExpressApp.DC;
using DevExpress.ExpressApp.Model;
using DevExpress.Persistent.Base;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Non-persistent dialog payload for <see cref="Controllers.ApplicationItemPdfController"/> before a <see cref="PdfGenerationBatch"/> is queued.
/// The <c>[DomainComponent]</c> attribute is still required by XAF for this pattern (XAF0016); it is unrelated to legacy XPO “Domain Components” composition.
/// </summary>
[DomainComponent]
[DefaultClassOptions]
public class PdfBatchEnqueueOptions : NonPersistentBaseObject
{
    public override void OnCreated()
    {
        base.OnCreated();
        ApplyPdfBatchDefaults();
    }

    [XafDisplayName("Include diploma files")]
    public virtual bool IncludeDiplomaFiles { get; set; }

    [XafDisplayName("Diploma scope")]
    [ImmediatePostData]
    public virtual PdfBatchDiplomaScope DiplomaScope { get; set; }

    [XafDisplayName("Supporting files in ZIP")]
    [ImmediatePostData]
    public virtual PdfSupportingZipMergeOption SupportingZipMergeOption { get; set; }

    [XafDisplayName("Merged diplomas PDF per line (PDF sources only)")]
    [ImmediatePostData]
    [Appearance(
        "HideMergedDiplomaWhenIndividualFilesOnly",
        AppearanceItemType = "ViewItem",
        TargetItems = nameof(IncludeMergedDiplomaPdf),
        Criteria = "SupportingZipMergeOption = ##Enum#Visa2026.Module.BusinessObjects.PdfSupportingZipMergeOption.IndividualFilesOnly#",
        Context = "DetailView",
        Visibility = DevExpress.ExpressApp.Editors.ViewItemVisibility.Hide)]
    public virtual bool IncludeMergedDiplomaPdf { get; set; }

    [XafDisplayName("Include passport copies")]
    public virtual bool IncludePassportCopies { get; set; }

    [XafDisplayName("Include visa copies")]
    public virtual bool IncludeVisaCopies { get; set; }

    [XafDisplayName("Include medical record copies")]
    public virtual bool IncludeMedicalRecordCopies { get; set; }

    [XafDisplayName("Include address of residence copies")]
    public virtual bool IncludeAddressOfResidenceCopies { get; set; }

    [XafDisplayName("Include work permit copies (employees; per line in ZIP)")]
    public virtual bool IncludeWorkPermitCopies { get; set; }

    [XafDisplayName("Include invitation copies (per line in ZIP)")]
    public virtual bool IncludeInvitationCopies { get; set; }

    [XafDisplayName("Include family relationship copies (family members)")]
    public virtual bool IncludeFamilyRelationshipCopies { get; set; }

    /// <summary>Same defaults as <see cref="PdfGenerationBatch"/> constructor / enqueue product choice.</summary>
    public void ApplyPdfBatchDefaults()
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

    public void CopyTo(PdfGenerationBatch batch)
    {
        batch.IncludeDiplomaFiles = IncludeDiplomaFiles;
        batch.DiplomaScope = DiplomaScope;
        batch.SupportingZipMergeOption = SupportingZipMergeOption;
        batch.IncludeMergedDiplomaPdf = SupportingZipMergeOption != PdfSupportingZipMergeOption.IndividualFilesOnly && IncludeMergedDiplomaPdf;
        batch.IncludePassportCopies = IncludePassportCopies;
        batch.IncludeVisaCopies = IncludeVisaCopies;
        batch.IncludeMedicalRecordCopies = IncludeMedicalRecordCopies;
        batch.IncludeAddressOfResidenceCopies = IncludeAddressOfResidenceCopies;
        batch.IncludeWorkPermitCopies = IncludeWorkPermitCopies;
        batch.IncludeInvitationCopies = IncludeInvitationCopies;
        batch.IncludeFamilyRelationshipCopies = IncludeFamilyRelationshipCopies;
    }
}
