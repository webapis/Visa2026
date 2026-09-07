using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationPersonDocumentLinksTests
{
    [Fact]
    public void Diff_ReplacesLatestWithApplicationSnapshot()
    {
        var current = Guid.NewGuid();
        var previous = Guid.NewGuid();
        var latest = Guid.NewGuid();

        Visa2014ApplicationPersonDocumentLinks.Diff(
            existing: [latest],
            desired: [previous, current],
            out var remove,
            out var add);

        Assert.Equal([latest], remove);
        Assert.Equal(2, add.Count);
        Assert.Contains(previous, add);
        Assert.Contains(current, add);
    }

    [Fact]
    public void Diff_AlreadyPinned_NoChange()
    {
        var current = Guid.NewGuid();
        var previous = Guid.NewGuid();

        Visa2014ApplicationPersonDocumentLinks.Diff(
            existing: [previous, current],
            desired: [current, previous],
            out var remove,
            out var add);

        Assert.Empty(remove);
        Assert.Empty(add);
    }

    [Fact]
    public void MapLegacyOids_SkipsMissingAndDuplicates()
    {
        var legacyCurrent = Guid.NewGuid();
        var legacyPrevious = Guid.NewGuid();
        var missing = Guid.NewGuid();
        var targetCurrent = Guid.NewGuid();
        var targetPrevious = Guid.NewGuid();
        var map = new Dictionary<Guid, Guid>
        {
            [legacyCurrent] = targetCurrent,
            [legacyPrevious] = targetPrevious,
        };

        var ids = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            map, legacyPrevious, legacyCurrent, missing, legacyCurrent);

        Assert.Equal([targetPrevious, targetCurrent], ids);
    }
}
