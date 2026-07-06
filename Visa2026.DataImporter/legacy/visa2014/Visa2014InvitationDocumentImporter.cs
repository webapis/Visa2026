using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014InvitationDocumentImporter
{
    private static readonly Visa2014PassportCopyLinkSpec Spec = new()
    {
        LegacyParentFkColumnPrefix = "Implicit_IApplicationResult_",
        LegacyParentTable = "dbo.ApplicationResult",
        LegacyParentNumberColumn = "Number",
        TargetDocumentType = typeof(Bo.InvitationDocument),
        TargetParentNavigationProperty = "Invitation",
        BuildFileName = Visa2014LegacyFileNameHelper.BuildInvitationCopyFileName,
    };

    public static Task<Visa2014PassportCopyLinkedDocumentImportResult> RunAsync(
        IVisa2014ImportTarget target,
        string legacyConnectionString,
        string invitationIdMapPath,
        string? documentIdMapOutputPath,
        int? maxRows,
        bool dryRun,
        bool verbose) =>
        Visa2014PassportCopyLinkedDocumentImporter.RunAsync(
            target,
            legacyConnectionString,
            invitationIdMapPath,
            documentIdMapOutputPath,
            Spec,
            maxRows,
            dryRun,
            verbose);
}
