namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// Controls which legacy rows are eligible for update during <c>--sync-visa2014</c>.
/// Inserts (legacy Oid not in id-map) are always processed when the transform row is present.
/// </summary>
internal sealed class Visa2014SyncRowFilter
{
    /// <summary>When true, every id-mapped row in the transform batch is updated.</summary>
    public bool ProcessAllMappedRows { get; init; }

    /// <summary>
    /// Legacy Oids with audit activity since the watermark. When null and
    /// <see cref="ProcessAllMappedRows"/> is false, mapped rows are not updated.
    /// </summary>
    public HashSet<Guid>? ChangedLegacyOids { get; init; }

    public bool ShouldUpdateMappedRow(Guid legacyOid)
    {
        if (ProcessAllMappedRows)
            return true;

        return ChangedLegacyOids != null && ChangedLegacyOids.Contains(legacyOid);
    }
}

internal sealed class Visa2014SyncContext
{
    public required Visa2014SyncRowFilter RowFilter { get; init; }

    public required Dictionary<Guid, Guid> IdMap { get; init; }

    public required string IdMapOutputPath { get; init; }

    public bool PropagateSoftDeletes { get; init; } = true;
}

internal sealed class Visa2014SyncEntityResult
{
    public int LegacyRowCount { get; init; }
    public int PreparedCount { get; init; }
    public int InsertedCount { get; init; }
    public int UpdatedCount { get; init; }
    public int SkippedUnchangedCount { get; init; }
    public int SoftDeletedCount { get; init; }
    public int FailedCount { get; init; }
    public int SkippedCount { get; init; }
    public int DedupeMergedCount { get; init; }
    public int RelinkedCount { get; init; }
    public string? IdMapPath { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = [];
}
