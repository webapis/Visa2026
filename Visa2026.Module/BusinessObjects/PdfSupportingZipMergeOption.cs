using DevExpress.ExpressApp.DC;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// How supporting-document entries are written into the batch ZIP: per-file paths, PdfSharpCore merged summaries, or both.
/// <see cref="PdfGenerationBatch.IncludeMergedDiplomaPdf"/> applies when this is not <see cref="IndividualFilesOnly"/>.
/// </summary>
public enum PdfSupportingZipMergeOption
{
    /// <summary>Only per-file copies (no <c>CurrentPassports.pdf</c>, <c>CurrentVisas.pdf</c>, <c>AllDiplomas.pdf</c>, no per-line <c>Merged/_AllDiplomas_merged.pdf</c>).</summary>
    [XafDisplayName("Individual files only (no merged PDFs)")]
    IndividualFilesOnly = 0,

    /// <summary>Per-file copies plus merged PDFs when the corresponding include/merged flags are on.</summary>
    [XafDisplayName("Individual files + merged PDF summaries")]
    IndividualFilesAndMergedPdfs = 1,

    /// <summary>
    /// Only batch merged PDFs for passport, visa, diplomas, and work permits (when those includes are on) and optional per-line merged diploma PDF;
    /// no per-document ZIP entries for those categories. Medical, address, invitation, and family documents are omitted (no batch merge).
    /// </summary>
    [XafDisplayName("Merged PDF summaries only (smaller ZIP)")]
    MergedPdfSummariesOnly = 2
}
