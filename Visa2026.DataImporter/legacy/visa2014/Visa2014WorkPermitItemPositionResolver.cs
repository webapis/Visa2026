namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Resolves legacy <c>WorkPermit.Position</c> (WorkHistoryOfEmployee OID) to Visa2026
/// <c>EmployeePositionHistory</c> id-map entries, with nearest-active-history fallback.
/// </summary>
internal sealed class Visa2014WorkPermitItemPositionResolver
{
    private readonly IReadOnlyDictionary<Guid, Guid> _positionHistoryIdMap;
    private readonly Dictionary<Guid, List<ActiveWorkHistoryRow>> _activeWorkHistoryByEmployee;

    internal sealed record ActiveWorkHistoryRow(Guid LegacyOid, DateTime? StartDate);

    public Visa2014WorkPermitItemPositionResolver(
        string legacyConnectionString,
        IReadOnlyDictionary<Guid, Guid> positionHistoryIdMap,
        bool verbose)
    {
        _positionHistoryIdMap = positionHistoryIdMap;
        _activeWorkHistoryByEmployee = LoadActiveWorkHistoryByEmployee(legacyConnectionString, verbose);
    }

    public bool TryResolvePositionHistoryId(
        Guid legacyPositionOid,
        Guid legacyPersonOid,
        DateTime? permitStartDate,
        out Guid positionHistoryId,
        out string resolutionNote)
    {
        if (_positionHistoryIdMap.TryGetValue(legacyPositionOid, out positionHistoryId))
        {
            resolutionNote = "";
            return true;
        }

        if (!_activeWorkHistoryByEmployee.TryGetValue(legacyPersonOid, out var activeRows) ||
            activeRows.Count == 0)
        {
            positionHistoryId = Guid.Empty;
            resolutionNote = "EmployeePositionHistory not in id-map (no active WH fallback)";
            return false;
        }

        if (!TrySelectFallbackWorkHistoryOid(activeRows, permitStartDate, out var fallbackLegacyOid))
        {
            positionHistoryId = Guid.Empty;
            resolutionNote = "EmployeePositionHistory not in id-map (could not pick fallback WH)";
            return false;
        }

        if (!_positionHistoryIdMap.TryGetValue(fallbackLegacyOid, out positionHistoryId))
        {
            positionHistoryId = Guid.Empty;
            resolutionNote =
                $"EmployeePositionHistory not in id-map (fallback WH {fallbackLegacyOid} not mapped)";
            return false;
        }

        resolutionNote =
            $"fallback: active WH {fallbackLegacyOid} for permit position {legacyPositionOid}";
        return true;
    }

    internal static bool TrySelectFallbackWorkHistoryOid(
        IReadOnlyList<ActiveWorkHistoryRow> activeRows,
        DateTime? permitStartDate,
        out Guid fallbackLegacyOid)
    {
        fallbackLegacyOid = Guid.Empty;
        if (activeRows.Count == 0)
            return false;

        var anchor = permitStartDate?.Date ?? DateTime.MinValue;
        ActiveWorkHistoryRow? bestOnOrBefore = null;
        foreach (var row in activeRows)
        {
            var start = row.StartDate?.Date ?? DateTime.MinValue;
            if (start > anchor)
                continue;

            if (bestOnOrBefore == null ||
                (bestOnOrBefore.StartDate?.Date ?? DateTime.MinValue) < start ||
                ((bestOnOrBefore.StartDate?.Date ?? DateTime.MinValue) == start &&
                 bestOnOrBefore.LegacyOid.CompareTo(row.LegacyOid) < 0))
            {
                bestOnOrBefore = row;
            }
        }

        if (bestOnOrBefore != null)
        {
            fallbackLegacyOid = bestOnOrBefore.LegacyOid;
            return true;
        }

        var earliest = activeRows
            .OrderBy(r => r.StartDate ?? DateTime.MaxValue)
            .ThenBy(r => r.LegacyOid)
            .First();
        fallbackLegacyOid = earliest.LegacyOid;
        return true;
    }

    private static Dictionary<Guid, List<ActiveWorkHistoryRow>> LoadActiveWorkHistoryByEmployee(
        string legacyConnectionString,
        bool verbose)
    {
        const string sql = """
            SELECT
                CAST(w.Employee AS varchar(36)) AS EmployeeOid,
                CAST(w.Oid AS varchar(36)) AS WhOid,
                CONVERT(varchar(10), w.StartDateOnThisPosition, 23) AS StartDate
            FROM dbo.WorkHistoryOfEmployee w
            INNER JOIN dbo.Person p ON p.Oid = w.Employee
            WHERE w.GCRecord IS NULL
            """;

        var dictRows = Visa2014SqlCmdReader.Query(legacyConnectionString, sql, verbose: false);
        var result = new Dictionary<Guid, List<ActiveWorkHistoryRow>>();
        foreach (var dict in dictRows)
        {
            if (!dict.TryGetValue("EmployeeOid", out var employeeText) ||
                !Guid.TryParse(employeeText?.Trim(), out var employeeOid))
                continue;
            if (!dict.TryGetValue("WhOid", out var whText) ||
                !Guid.TryParse(whText?.Trim(), out var whOid))
                continue;

            DateTime? startDate = DateTime.TryParse(dict.GetValueOrDefault("StartDate"), out var start)
                ? start
                : null;

            if (!result.TryGetValue(employeeOid, out var list))
            {
                list = [];
                result[employeeOid] = list;
            }

            list.Add(new ActiveWorkHistoryRow(whOid, startDate));
        }

        if (verbose)
            Console.WriteLine($"INF Active WorkHistoryOfEmployee index: {result.Count} employee(s), {dictRows.Count} row(s).");

        return result;
    }
}