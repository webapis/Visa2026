namespace Visa2026.Module.BusinessObjects;

/// <summary>Which <see cref="Education"/> rows are packed when <see cref="PdfGenerationBatch.IncludeDiplomaFiles"/> is true.</summary>
public enum PdfBatchDiplomaScope
{
    /// <summary>All non-deleted educations on <see cref="ApplicationItem.Person"/> (ordered by graduation year desc).</summary>
    AllEducations = 0,

    /// <summary>Only <see cref="ApplicationItem.CurrentEducation"/> when set on the line (§1.3).</summary>
    CurrentEducationOnly = 1
}
