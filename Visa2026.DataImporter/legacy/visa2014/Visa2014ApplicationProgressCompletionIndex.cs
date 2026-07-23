namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Legacy application completion evidence from issued invitation (ApplicationResult),
/// work permit (PersonInApplication.WorkPermit), or full visa coverage on extension subtype 7 (ProcessNumber or next sibling after PIA.Visa).
/// Mirrors Visa2026 Application.Invitations / WorkPermits / Visa.IssuingApplicationItem after import.
/// </summary>
internal sealed record Visa2014ApplicationProgressCompletionEvidence(
    DateTime? CompletionDate,
    string SourceLabel,
    string? SourceValue)
{
    public bool HasCompletion =>
        IsLegacyDateSet(CompletionDate) || !string.IsNullOrWhiteSpace(SourceValue);

    private static bool IsLegacyDateSet(DateTime? date) =>
        date.HasValue && date.Value >= new DateTime(2000, 1, 1);
}

internal static class Visa2014ApplicationProgressCompletionIndex
{
    private static readonly DateTime LegacyDateThreshold = new(2000, 1, 1);

    internal const string InvitationWorkPermitLoadSql = """
        SELECT
            CAST(a.Oid AS varchar(36)) AS ApplicationOid,
            CONVERT(varchar(10), inv.IssuedDate, 23) AS InvitationIssuedDate,
            inv.Number AS InvitationNumber,
            CONVERT(varchar(10), wp.IssuedDate, 23) AS WorkPermitIssuedDate,
            wp.Number AS WorkPermitNumber
        FROM dbo.Application a
        OUTER APPLY (
            SELECT TOP 1
                ar.IssuedDate,
                ar.Number
            FROM dbo.ApplicationResult ar
            WHERE ar.Application = a.Oid
              AND ar.GCRecord IS NULL
              AND ar.Result = 0
              AND EXISTS (
                  SELECT 1
                  FROM dbo.PersonInInvitation pii
                  WHERE pii.Invitation = ar.Oid
                    AND pii.GCRecord IS NULL)
            ORDER BY ar.IssuedDate DESC, ar.Oid
        ) inv
        OUTER APPLY (
            SELECT TOP 1
                COALESCE(wp.StartDateOfWorkPermit, wpl.Date) AS IssuedDate,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(wp.AppruvalNumber)), ''),
                    NULLIF(LTRIM(RTRIM(wpl.Number)), '')) AS Number
            FROM dbo.PersonInApplication pia
            INNER JOIN dbo.WorkPermit wp ON wp.Oid = pia.WorkPermit AND wp.GCRecord IS NULL
            LEFT JOIN dbo.WorkPermitLetter wpl ON wpl.Oid = wp.WorkPermitLetter AND wpl.GCRecord IS NULL
            WHERE pia.Application = a.Oid
              AND pia.GCRecord IS NULL
              AND pia.WorkPermit IS NOT NULL
            ORDER BY COALESCE(wp.StartDateOfWorkPermit, wpl.Date) DESC, wp.Oid
        ) wp
        WHERE a.GCRecord IS NULL
          AND (
              (inv.IssuedDate IS NOT NULL AND inv.IssuedDate >= '2000-01-01')
              OR (wp.IssuedDate IS NOT NULL AND wp.IssuedDate >= '2000-01-01')
              OR NULLIF(LTRIM(RTRIM(inv.Number)), '') IS NOT NULL
              OR NULLIF(LTRIM(RTRIM(wp.Number)), '') IS NOT NULL)
        """;

    /// <summary>
    /// Extension apps (employee/FM subtype 7) where every PIA is covered by a Visa via
    /// ProcessNumber OR an immediate next passport sibling after PIA.Visa (sticky invitation FK).
    /// </summary>
    internal const string VisaExtensionLoadSql = """
        SELECT
            CAST(a.Oid AS varchar(36)) AS ApplicationOid,
            pia.ItemCount AS ApplicationItemCount,
            linked.VisaLinkedCount,
            CONVERT(varchar(10), sample.MaxVisaIssuedDate, 23) AS MaxVisaIssuedDate,
            sample.SampleVisaNumber
        FROM dbo.Application a
        LEFT JOIN dbo.ApplicationTypeForEmployee ate ON ate.Oid = a.ApplicationTypeForEmployee
        LEFT JOIN dbo.ApplicationTypeForFamilyMember atfm ON atfm.Oid = a.ApplicationTypeForFamilyMember
        CROSS APPLY (
            SELECT COUNT_BIG(*) AS ItemCount
            FROM dbo.PersonInApplication pia
            WHERE pia.Application = a.Oid
              AND pia.GCRecord IS NULL
        ) pia
        CROSS APPLY (
            SELECT COUNT_BIG(*) AS VisaLinkedCount
            FROM dbo.PersonInApplication pia2
            WHERE pia2.Application = a.Oid
              AND pia2.GCRecord IS NULL
              AND (
                  EXISTS (
                      SELECT 1
                      FROM dbo.Visa v
                      WHERE v.ProcessNumber = pia2.Oid
                        AND v.GCRecord IS NULL)
                  OR (
                      pia2.Visa IS NOT NULL
                      AND EXISTS (
                          SELECT 1
                          FROM dbo.Visa prev
                          INNER JOIN dbo.Visa nextv
                              ON nextv.Passport = prev.Passport
                             AND nextv.GCRecord IS NULL
                             AND nextv.VisaIssuedDate > prev.VisaIssuedDate
                          WHERE prev.Oid = pia2.Visa
                            AND prev.GCRecord IS NULL
                            AND NOT EXISTS (
                                SELECT 1
                                FROM dbo.Visa mid
                                WHERE mid.Passport = prev.Passport
                                  AND mid.GCRecord IS NULL
                                  AND mid.VisaIssuedDate > prev.VisaIssuedDate
                                  AND mid.VisaIssuedDate < nextv.VisaIssuedDate)))
              )
        ) linked
        OUTER APPLY (
            SELECT
                MAX(issued.VisaIssuedDate) AS MaxVisaIssuedDate,
                (
                    SELECT TOP 1 NULLIF(LTRIM(RTRIM(issued2.VisaNumber)), '')
                    FROM (
                        SELECT v.VisaNumber, v.VisaIssuedDate, v.Oid
                        FROM dbo.PersonInApplication pia3
                        INNER JOIN dbo.Visa v ON v.ProcessNumber = pia3.Oid AND v.GCRecord IS NULL
                        WHERE pia3.Application = a.Oid AND pia3.GCRecord IS NULL
                        UNION ALL
                        SELECT nextv.VisaNumber, nextv.VisaIssuedDate, nextv.Oid
                        FROM dbo.PersonInApplication pia3b
                        INNER JOIN dbo.Visa prev ON prev.Oid = pia3b.Visa AND prev.GCRecord IS NULL
                        INNER JOIN dbo.Visa nextv
                            ON nextv.Passport = prev.Passport
                           AND nextv.GCRecord IS NULL
                           AND nextv.VisaIssuedDate > prev.VisaIssuedDate
                        WHERE pia3b.Application = a.Oid
                          AND pia3b.GCRecord IS NULL
                          AND pia3b.Visa IS NOT NULL
                          AND NOT EXISTS (
                              SELECT 1
                              FROM dbo.Visa mid
                              WHERE mid.Passport = prev.Passport
                                AND mid.GCRecord IS NULL
                                AND mid.VisaIssuedDate > prev.VisaIssuedDate
                                AND mid.VisaIssuedDate < nextv.VisaIssuedDate)
                    ) issued2
                    ORDER BY issued2.VisaIssuedDate DESC, issued2.Oid
                ) AS SampleVisaNumber
            FROM (
                SELECT v.VisaIssuedDate
                FROM dbo.PersonInApplication pia4
                INNER JOIN dbo.Visa v ON v.ProcessNumber = pia4.Oid AND v.GCRecord IS NULL
                WHERE pia4.Application = a.Oid AND pia4.GCRecord IS NULL
                UNION ALL
                SELECT nextv.VisaIssuedDate
                FROM dbo.PersonInApplication pia4b
                INNER JOIN dbo.Visa prev ON prev.Oid = pia4b.Visa AND prev.GCRecord IS NULL
                INNER JOIN dbo.Visa nextv
                    ON nextv.Passport = prev.Passport
                   AND nextv.GCRecord IS NULL
                   AND nextv.VisaIssuedDate > prev.VisaIssuedDate
                WHERE pia4b.Application = a.Oid
                  AND pia4b.GCRecord IS NULL
                  AND pia4b.Visa IS NOT NULL
                  AND NOT EXISTS (
                      SELECT 1
                      FROM dbo.Visa mid
                      WHERE mid.Passport = prev.Passport
                        AND mid.GCRecord IS NULL
                        AND mid.VisaIssuedDate > prev.VisaIssuedDate
                        AND mid.VisaIssuedDate < nextv.VisaIssuedDate)
            ) issued
        ) sample
        WHERE a.GCRecord IS NULL
          AND (
              ate.TypeOfApplicationForEmployee = 7
              OR atfm.TypeOfApplicationForFamilyMember = 7)
          AND pia.ItemCount > 0
          AND pia.ItemCount = linked.VisaLinkedCount
        """;

    // Backward-compatible alias used by older call sites / docs.
    internal const string LoadSql = InvitationWorkPermitLoadSql;

    public static IReadOnlyDictionary<Guid, Visa2014ApplicationProgressCompletionEvidence> Load(
        string connectionString,
        bool verbose)
    {
        var map = new Dictionary<Guid, Visa2014ApplicationProgressCompletionEvidence>();

        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, InvitationWorkPermitLoadSql, verbose))
        {
            if (!TryParseApplicationOid(row, out var applicationOid))
                continue;

            var evidence = BuildInvitationWorkPermitEvidence(row);
            if (evidence.HasCompletion)
                map[applicationOid] = evidence;
        }

        var invitationWorkPermitCount = map.Count;
        var visaExtensionAdded = 0;

        foreach (var row in Visa2014SqlCmdReader.Query(connectionString, VisaExtensionLoadSql, verbose))
        {
            if (!TryParseApplicationOid(row, out var applicationOid))
                continue;
            if (map.ContainsKey(applicationOid))
                continue;

            var evidence = BuildVisaExtensionEvidence(row);
            if (!evidence.HasCompletion)
                continue;

            map[applicationOid] = evidence;
            visaExtensionAdded++;
        }

        if (verbose)
        {
            Console.WriteLine(
                $"INF ApplicationProgress completion index: {map.Count} legacy application(s) " +
                $"(invitation/work-permit={invitationWorkPermitCount}, visa-extension-added={visaExtensionAdded}).");
        }

        return map;
    }

    internal static Visa2014ApplicationProgressCompletionEvidence BuildEvidence(
        IReadOnlyDictionary<string, string?> row) =>
        BuildInvitationWorkPermitEvidence(row);

    internal static Visa2014ApplicationProgressCompletionEvidence BuildInvitationWorkPermitEvidence(
        IReadOnlyDictionary<string, string?> row)
    {
        var invitationDate = TryParseDate(row.GetValueOrDefault("InvitationIssuedDate"));
        var workPermitDate = TryParseDate(row.GetValueOrDefault("WorkPermitIssuedDate"));
        var invitationNumber = NormalizeRef(row.GetValueOrDefault("InvitationNumber"));
        var workPermitNumber = NormalizeRef(row.GetValueOrDefault("WorkPermitNumber"));

        var hasInvitation = IsLegacyDateSet(invitationDate) || invitationNumber != null;
        var hasWorkPermit = IsLegacyDateSet(workPermitDate) || workPermitNumber != null;
        if (!hasInvitation && !hasWorkPermit)
            return new Visa2014ApplicationProgressCompletionEvidence(null, "", null);

        var useInvitation = hasInvitation && (!hasWorkPermit || PreferInvitation(invitationDate, workPermitDate));
        if (useInvitation)
        {
            return new Visa2014ApplicationProgressCompletionEvidence(
                invitationDate ?? workPermitDate,
                "InvitationNumber",
                invitationNumber ?? workPermitNumber);
        }

        return new Visa2014ApplicationProgressCompletionEvidence(
            workPermitDate ?? invitationDate,
            "WorkPermitNumber",
            workPermitNumber ?? invitationNumber);
    }

    internal static Visa2014ApplicationProgressCompletionEvidence BuildVisaExtensionEvidence(
        IReadOnlyDictionary<string, string?> row)
    {
        var itemCount = ParseCount(row.GetValueOrDefault("ApplicationItemCount"));
        var visaLinkedCount = ParseCount(row.GetValueOrDefault("VisaLinkedCount"));
        if (itemCount <= 0 || itemCount != visaLinkedCount)
            return new Visa2014ApplicationProgressCompletionEvidence(null, "", null);

        var completionDate = TryParseDate(row.GetValueOrDefault("MaxVisaIssuedDate"));
        var visaNumber = NormalizeRef(row.GetValueOrDefault("SampleVisaNumber"));
        if (!IsLegacyDateSet(completionDate) && visaNumber == null)
            return new Visa2014ApplicationProgressCompletionEvidence(null, "", null);

        return new Visa2014ApplicationProgressCompletionEvidence(
            IsLegacyDateSet(completionDate) ? completionDate : null,
            "VisaNumber",
            visaNumber);
    }

    /// <summary>
    /// Merge helper for tests: invitation/work-permit wins over visa-extension for the same app.
    /// </summary>
    internal static Dictionary<Guid, Visa2014ApplicationProgressCompletionEvidence> Merge(
        IEnumerable<(Guid ApplicationOid, Visa2014ApplicationProgressCompletionEvidence Evidence)> invitationWorkPermit,
        IEnumerable<(Guid ApplicationOid, Visa2014ApplicationProgressCompletionEvidence Evidence)> visaExtension)
    {
        var map = new Dictionary<Guid, Visa2014ApplicationProgressCompletionEvidence>();
        foreach (var (oid, evidence) in invitationWorkPermit)
        {
            if (evidence.HasCompletion)
                map[oid] = evidence;
        }

        foreach (var (oid, evidence) in visaExtension)
        {
            if (!evidence.HasCompletion || map.ContainsKey(oid))
                continue;
            map[oid] = evidence;
        }

        return map;
    }

    private static bool TryParseApplicationOid(
        IReadOnlyDictionary<string, string?> row,
        out Guid applicationOid)
    {
        applicationOid = default;
        return row.TryGetValue("ApplicationOid", out var oidText)
            && Guid.TryParse(oidText?.Trim(), out applicationOid);
    }

    private static bool PreferInvitation(DateTime? invitationDate, DateTime? workPermitDate)
    {
        if (!IsLegacyDateSet(invitationDate))
            return false;
        if (!IsLegacyDateSet(workPermitDate))
            return true;
        return invitationDate!.Value >= workPermitDate!.Value;
    }

    private static int ParseCount(string? text) =>
        int.TryParse(text?.Trim(), out var n) && n >= 0 ? n : 0;

    private static string? NormalizeRef(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? TryParseDate(string? text) =>
        DateTime.TryParse(text, out var parsed) ? parsed : null;

    private static bool IsLegacyDateSet(DateTime? date) =>
        date.HasValue && date.Value >= LegacyDateThreshold;
}