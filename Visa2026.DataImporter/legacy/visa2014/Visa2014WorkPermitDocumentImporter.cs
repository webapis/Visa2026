using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014WorkPermitDocumentImporter
{
    private static readonly Visa2014PassportCopyLinkSpec Spec = new()
    {
        LegacyParentFkColumnPrefix = "Implicit_IWorkPermitLetter_",
        LegacyParentTable = "dbo.WorkPermitLetter",
        LegacyParentNumberColumn = "Number",
        TargetDocumentType = typeof(Bo.WorkPermitDocument),
        TargetParentNavigationProperty = "WorkPermit",
        BuildFileName = Visa2014LegacyFileNameHelper.BuildWorkPermitCopyFileName,
    };

    public static Task<Visa2014PassportCopyLinkedDocumentImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        string workPermitIdMapPath,
        string? documentIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose) =>
        Visa2014PassportCopyLinkedDocumentImporter.RunAsync(
            target,
            legacyConnectionString,
            workPermitIdMapPath,
            documentIdMapOutputPath,
            Spec,
            maxRows,
            dryRun,
            verbose);
}
