namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Legacy application completion evidence from issued invitation (ApplicationResult) or work permit (PersonInApplication.WorkPermit).
/// Mirrors Visa2026 <c>Application.Invitations</c> / <c>Application.WorkPermits</c> being populated after import.
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

    internal const string LoadSql = """
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

    public static IReadOnlyDictionary<Guid, Visa2014ApplicationProgressCompletionEvidence> Load(
        string connectionString,
        bool verbose)
    {
        var dictRows = Visa2014SqlCmdReader.Query(connectionString, LoadSql, verbose);
        var map = new Dictionary<Guid, Visa2014ApplicationProgressCompletionEvidence>();
        foreach (var row in dictRows)
        {
            if (!row.TryGetValue("ApplicationOid", out var oidText)
                || !Guid.TryParse(oidText?.Trim(), out var applicationOid))
                continue;

            var evidence = BuildEvidence(row);
            if (evidence.HasCompletion)
                map[applicationOid] = evidence;
        }

        if (verbose)
            Console.WriteLine($"INF ApplicationProgress completion index: {map.Count} legacy application(s) with invitation/work-permit evidence.");

        return map;
    }

    internal static Visa2014ApplicationProgressCompletionEvidence BuildEvidence(IReadOnlyDictionary<string, string?> row)
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

    private static bool PreferInvitation(DateTime? invitationDate, DateTime? workPermitDate)
    {
        if (!IsLegacyDateSet(invitationDate))
            return false;
        if (!IsLegacyDateSet(workPermitDate))
            return true;
        return invitationDate!.Value >= workPermitDate!.Value;
    }

    private static string? NormalizeRef(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateTime? TryParseDate(string? text) =>
        DateTime.TryParse(text, out var parsed) ? parsed : null;

    private static bool IsLegacyDateSet(DateTime? date) =>
        date.HasValue && date.Value >= LegacyDateThreshold;
}
