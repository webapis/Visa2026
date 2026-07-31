namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Full-application rejection evidence: ApplicationItem count equals RejectionItem count
/// (legacy PersonInApplication vs PersonInInvitation under ApplicationResult Result=Rejection).
/// Used with OR legacy Application.Rejected=1 to synthesize PROCESS_REJECTED.
/// </summary>
internal sealed record Visa2014ApplicationProgressRejectionEvidence(
    int ApplicationItemCount,
    int RejectionItemCount,
    DateTime? RejectionDate,
    string? RejectionNumbers)
{
    public bool HasFullCoverage =>
        ApplicationItemCount > 0 && ApplicationItemCount == RejectionItemCount;
}

internal static class Visa2014ApplicationProgressRejectionIndex
{
    private static readonly DateTime LegacyDateThreshold = new(2000, 1, 1);

    /// <summary>
    /// Applications where PIA count &gt; 0 and equals total Rejection items (Result = 1).
    /// </summary>
    internal const string LoadSql = """
        SELECT
            CAST(a.Oid AS varchar(36)) AS ApplicationOid,
            pia.ItemCount AS ApplicationItemCount,
            rej.RejectionItemCount,
            CONVERT(varchar(10), rej.MaxIssuedDate, 23) AS MaxRejectionIssuedDate,
            rej.RejectionNumbers
        FROM dbo.Application a
        CROSS APPLY (
            SELECT COUNT_BIG(*) AS ItemCount
            FROM dbo.PersonInApplication pia
            WHERE pia.Application = a.Oid
              AND pia.GCRecord IS NULL
        ) pia
        CROSS APPLY (
            SELECT
                COUNT_BIG(pii.Oid) AS RejectionItemCount,
                MAX(ar.IssuedDate) AS MaxIssuedDate,
                -- STRING_AGG WITHOUT WITHIN GROUP (SQL Server 2017+; WITHIN GROUP needs 2022+)
                STRING_AGG(CAST(NULLIF(LTRIM(RTRIM(ar.Number)), '') AS nvarchar(100)), ', ') AS RejectionNumbers
            FROM dbo.ApplicationResult ar
            LEFT JOIN dbo.PersonInInvitation pii
                ON pii.Invitation = ar.Oid
               AND pii.GCRecord IS NULL
            WHERE ar.Application = a.Oid
              AND ar.GCRecord IS NULL
              AND ar.Result = 1
        ) rej
        WHERE a.GCRecord IS NULL
          AND pia.ItemCount > 0
          AND pia.ItemCount = rej.RejectionItemCount
        """;

    public static IReadOnlyDictionary<Guid, Visa2014ApplicationProgressRejectionEvidence> Load(
        string connectionString,
        bool verbose)
    {
        var dictRows = Visa2014SqlCmdReader.Query(connectionString, LoadSql, verbose);
        var map = new Dictionary<Guid, Visa2014ApplicationProgressRejectionEvidence>();
        foreach (var row in dictRows)
        {
            if (!row.TryGetValue("ApplicationOid", out var oidText)
                || !Guid.TryParse(oidText?.Trim(), out var applicationOid))
                continue;

            var evidence = BuildEvidence(row);
            if (evidence.HasFullCoverage)
                map[applicationOid] = evidence;
        }

        if (verbose)
            Console.WriteLine(
                $"INF ApplicationProgress rejection coverage index: {map.Count} legacy application(s) with full RejectionItem coverage.");

        return map;
    }

    internal static Visa2014ApplicationProgressRejectionEvidence BuildEvidence(
        IReadOnlyDictionary<string, string?> row)
    {
        var itemCount = ParseCount(row.GetValueOrDefault("ApplicationItemCount"));
        var rejectionItemCount = ParseCount(row.GetValueOrDefault("RejectionItemCount"));
        var rejectionDate = TryParseDate(row.GetValueOrDefault("MaxRejectionIssuedDate"));
        var numbers = NormalizeRef(row.GetValueOrDefault("RejectionNumbers"));
        return new Visa2014ApplicationProgressRejectionEvidence(
            itemCount,
            rejectionItemCount,
            IsLegacyDateSet(rejectionDate) ? rejectionDate : null,
            numbers);
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