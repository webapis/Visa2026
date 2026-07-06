namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Indexes legacy invitation-line cancellation onto <c>PersonInInvitation.Oid</c>
/// for import-time <see cref="Bo.InvitationItem.IsCancelled"/> backfill.
/// </summary>
internal sealed class Visa2014LegacyInvitationItemCancellationIndex
{
    private readonly HashSet<Guid> _cancelledInvitationItemOids = [];

    internal const string ApplicationResultCancelledSql = """
        SELECT CAST(pii.Oid AS varchar(36)) AS InvitationItemOid
        FROM dbo.PersonInInvitation pii
        INNER JOIN dbo.ApplicationResult ar ON ar.Oid = pii.Invitation AND ar.GCRecord IS NULL
        WHERE pii.GCRecord IS NULL
          AND ar.Result = 1
        """;

    internal const string PiaCancelledEvidenceSql = """
        SELECT
            CAST(invMatch.InvitationItemOid AS varchar(36)) AS InvitationItemOid,
            CASE WHEN ISNULL(pia.Cancelled, 0) = 1 THEN '1' ELSE '0' END AS Cancelled,
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
        OUTER APPLY (
            SELECT TOP 1 CAST(pii.Oid AS varchar(36)) AS InvitationItemOid
            FROM dbo.PersonInInvitation pii
            INNER JOIN dbo.ApplicationResult ar ON ar.Oid = pii.Invitation AND ar.GCRecord IS NULL
            WHERE pii.GCRecord IS NULL
              AND ar.Application = pia.Application
              AND (
                  (pia.Employee IS NOT NULL AND pii.Employee = pia.Employee)
                  OR (pia.FamilyMember IS NOT NULL AND pii.FamilyMember = pia.FamilyMember))
            ORDER BY ar.IssuedDate DESC, pii.Oid
        ) invMatch
        WHERE pia.GCRecord IS NULL
          AND ISNULL(pia.Cancelled, 0) = 1
          AND invMatch.InvitationItemOid IS NOT NULL
        """;

    public static Visa2014LegacyInvitationItemCancellationIndex Load(
        string connectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        bool verbose)
    {
        var catalogs = Visa2014LookupTranslator.Load(lookupTranslationPaths);
        var visibility = ApplicationTypeVisibilityCatalog.Load();
        var index = new Visa2014LegacyInvitationItemCancellationIndex();

        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, ApplicationResultCancelledSql, verbose))
        {
            if (TryParseGuid(row.GetValueOrDefault("InvitationItemOid"), out var invitationItemOid))
                index._cancelledInvitationItemOids.Add(invitationItemOid);
        }

        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, PiaCancelledEvidenceSql, verbose))
            index.ApplyPiaCancelledRow(row, catalogs, visibility);

        if (verbose)
        {
            Console.WriteLine(
                $"INF Legacy invitation-item cancellation index: {index._cancelledInvitationItemOids.Count} " +
                "PersonInInvitation row(s) with evidence.");
        }

        return index;
    }

    public bool IsInvitationItemCancelled(Guid legacyPersonInInvitationOid) =>
        _cancelledInvitationItemOids.Contains(legacyPersonInInvitationOid);

    public static bool ResolveIsCancelled(
        int? applicationResultResult,
        Guid legacyPersonInInvitationOid,
        Visa2014LegacyInvitationItemCancellationIndex index) =>
        applicationResultResult == 1 || index.IsInvitationItemCancelled(legacyPersonInInvitationOid);

    internal static Visa2014LegacyInvitationItemCancellationIndex FromLegacyOidsForTests(IEnumerable<Guid> legacyPersonInInvitationOids)
    {
        var index = new Visa2014LegacyInvitationItemCancellationIndex();
        foreach (var oid in legacyPersonInInvitationOids)
            index._cancelledInvitationItemOids.Add(oid);
        return index;
    }

    private void ApplyPiaCancelledRow(
        IReadOnlyDictionary<string, string?> row,
        IReadOnlyDictionary<string, Visa2014LookupCatalog> catalogs,
        ApplicationTypeVisibilityCatalog visibility)
    {
        if (row.GetValueOrDefault("Cancelled") != "1")
            return;

        if (!TryParseGuid(row.GetValueOrDefault("InvitationItemOid"), out var invitationItemOid))
            return;

        var forEmployee = row.GetValueOrDefault("ForEmployee") == "1";
        var forFamilyMember = row.GetValueOrDefault("ForFamilyMember") == "1";
        var employeeSubtypeId = ParseNullableInt(row.GetValueOrDefault("EmployeeSubtypeId"));
        var familySubtypeId = ParseNullableInt(row.GetValueOrDefault("FamilySubtypeId"));

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

        var flags = Visa2014ApplicationItemCancelledFlagsMapper.ResolveDocumentCancellation(
            applicationTypeName,
            visibility,
            legacyCancelled: true);

        if (flags.InvitationItem)
            _cancelledInvitationItemOids.Add(invitationItemOid);
    }

    private static int? ParseNullableInt(string? text) =>
        int.TryParse(text?.Trim(), out var value) ? value : null;

    private static bool TryParseGuid(string? text, out Guid oid) =>
        Guid.TryParse(text?.Trim(), out oid);
}