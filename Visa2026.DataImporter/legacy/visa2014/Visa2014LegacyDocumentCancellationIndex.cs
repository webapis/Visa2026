namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Indexes legacy cancellation evidence from <c>PersonInApplication</c> onto document OIDs
/// (<c>dbo.Visa</c>, <c>dbo.WorkPermit</c>) for import-time <c>IsCancelled</c> backfill.
/// </summary>
internal sealed class Visa2014LegacyDocumentCancellationIndex
{
    private readonly HashSet<Guid> _cancelledVisaOids = [];
    private readonly HashSet<Guid> _cancelledWorkPermitOids = [];

    internal const string EvidenceExtractSql = """
        SELECT
            CAST(pia.Visa AS varchar(36)) AS VisaOid,
            CAST(pia.WorkPermit AS varchar(36)) AS WorkPermitOid,
            CASE WHEN ISNULL(pia.Cancelled, 0) = 1 THEN '1' ELSE '0' END AS Cancelled,
            CASE WHEN ISNULL(pia.IsComplete, 0) = 1 THEN '1' ELSE '0' END AS IsComplete,
            CASE WHEN ISNULL(a.ForEmployee, 0) = 1 THEN '1' ELSE '0' END AS ForEmployee,
            CASE WHEN ISNULL(a.ForFamilyMember, 0) = 1 THEN '1' ELSE '0' END AS ForFamilyMember,
            ate.TypeOfApplicationForEmployee AS EmployeeSubtypeId,
            atfm.TypeOfApplicationForFamilyMember AS FamilySubtypeId,
            CASE WHEN a.IsInvitationWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasInvitationWpFk,
            iwp.InvitationAndWorkPermitRequired,
            CASE WHEN a.IsWizaWithWorkPermit IS NULL THEN '0' ELSE '1' END AS HasWizaWpFk,
            wwp.WizaAndWorkPermitRequired,
            a.ChangeInformation
        FROM dbo.PersonInApplication pia
        INNER JOIN dbo.Application a ON a.Oid = pia.Application AND a.GCRecord IS NULL
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        LEFT JOIN dbo.IsInvitationWithWorkPermit iwp ON iwp.Oid = a.IsInvitationWithWorkPermit
        LEFT JOIN dbo.IsWizaWithWorkPermit wwp ON wwp.Oid = a.IsWizaWithWorkPermit
        WHERE pia.GCRecord IS NULL
          AND (
            ISNULL(pia.Cancelled, 0) = 1
            OR (
              ISNULL(pia.IsComplete, 0) = 1
              AND (
                ate.TypeOfApplicationForEmployee IN (12, 21, 22)
                OR atfm.TypeOfApplicationForFamilyMember IN (12, 21, 22)
              )
            )
          )
          AND (pia.Visa IS NOT NULL OR pia.WorkPermit IS NOT NULL)
        """;

    public static Visa2014LegacyDocumentCancellationIndex Load(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var visibility = ApplicationTypeVisibilityCatalog.Load();
        var dictRows = Visa2014SqlCmdReader.Query(connectionString, EvidenceExtractSql, verbose);

        var index = new Visa2014LegacyDocumentCancellationIndex();
        foreach (var row in dictRows)
            index.ApplyEvidenceRow(row, catalogs, visibility);

        if (verbose)
        {
            Console.WriteLine(
                $"INF Legacy document cancellation index: {index._cancelledVisaOids.Count} visa, " +
                $"{index._cancelledWorkPermitOids.Count} work-permit row(s) with evidence.");
        }

        return index;
    }

    public bool IsVisaCancelled(Guid legacyVisaOid) => _cancelledVisaOids.Contains(legacyVisaOid);

    public bool IsWorkPermitCancelled(Guid legacyWorkPermitOid) => _cancelledWorkPermitOids.Contains(legacyWorkPermitOid);

    private void ApplyEvidenceRow(
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        ApplicationTypeVisibilityCatalog visibility)
    {
        var cancelled = row.GetValueOrDefault("Cancelled") == "1";
        var isComplete = row.GetValueOrDefault("IsComplete") == "1";
        var forEmployee = row.GetValueOrDefault("ForEmployee") == "1";
        var forFamilyMember = row.GetValueOrDefault("ForFamilyMember") == "1";
        var employeeSubtypeId = ParseNullableInt(row.GetValueOrDefault("EmployeeSubtypeId"));
        var familySubtypeId = ParseNullableInt(row.GetValueOrDefault("FamilySubtypeId"));
        var subtypeId = forEmployee ? employeeSubtypeId : forFamilyMember ? familySubtypeId : null;

        var flags = default(Visa2014ApplicationItemCancelledFlagsMapper.LegacyDocumentCancellationFlags);
        if (isComplete)
        {
            flags = Visa2014ApplicationItemCancelledFlagsMapper.Merge(
                flags,
                Visa2014ApplicationItemCancelledFlagsMapper.ResolveFromCompletedCancelSubtype(subtypeId));
        }

        if (cancelled)
        {
            var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
                forEmployee,
                forFamilyMember,
                employeeSubtypeId,
                familySubtypeId,
                row.GetValueOrDefault("HasInvitationWpFk") == "1",
                ParseNullableInt(row.GetValueOrDefault("InvitationAndWorkPermitRequired")),
                row.GetValueOrDefault("HasWizaWpFk") == "1",
                ParseNullableInt(row.GetValueOrDefault("WizaAndWorkPermitRequired")),
                ParseNullableInt(row.GetValueOrDefault("ChangeInformation")));

            string? applicationTypeName = null;
            if (Visa2014LookupTranslator.TryTranslate(catalogs, "ApplicationType", composite, out var target, out _) &&
                !string.IsNullOrWhiteSpace(target))
            {
                applicationTypeName = target;
            }

            flags = Visa2014ApplicationItemCancelledFlagsMapper.Merge(
                flags,
                Visa2014ApplicationItemCancelledFlagsMapper.ResolveDocumentCancellation(
                    applicationTypeName,
                    visibility,
                    legacyCancelled: true));
        }

        if (!flags.Visa && !flags.WorkPermitItem)
            return;

        if (flags.Visa && TryParseGuid(row.GetValueOrDefault("VisaOid"), out var visaOid))
            _cancelledVisaOids.Add(visaOid);

        if (flags.WorkPermitItem && TryParseGuid(row.GetValueOrDefault("WorkPermitOid"), out var workPermitOid))
            _cancelledWorkPermitOids.Add(workPermitOid);
    }

    private static int? ParseNullableInt(string? text) =>
        int.TryParse(text?.Trim(), out var value) ? value : null;

    private static bool TryParseGuid(string? text, out Guid oid) =>
        Guid.TryParse(text?.Trim(), out oid);
}