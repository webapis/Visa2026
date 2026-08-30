using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014PassportIdMapAliasApplierTests
{
    [Fact]
    public void ApplyDedupeAliases_adds_merged_oid_when_canonical_mapped()
    {
        var canonical = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var merged = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var target = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [canonical.ToString()] = target.ToString(),
        };

        var added = Visa2014PassportIdMapAliasApplier.ApplyDedupeAliases(
            idMap,
            new Dictionary<Guid, Guid> { [merged] = canonical });

        Assert.Equal(1, added);
        Assert.Equal(target.ToString(), idMap[merged.ToString()]);
        Assert.Equal(2, idMap.Count);
    }

    [Fact]
    public void ApplyDedupeAliases_skips_when_canonical_missing()
    {
        var canonical = Guid.NewGuid();
        var merged = Guid.NewGuid();
        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var added = Visa2014PassportIdMapAliasApplier.ApplyDedupeAliases(
            idMap,
            new Dictionary<Guid, Guid> { [merged] = canonical });

        Assert.Equal(0, added);
        Assert.Empty(idMap);
    }

    [Fact]
    public void ApplyDedupeAliases_does_not_overwrite_existing_merged_key()
    {
        var canonical = Guid.NewGuid();
        var merged = Guid.NewGuid();
        var keepTarget = Guid.NewGuid();
        var canonicalTarget = Guid.NewGuid();
        var idMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [canonical.ToString()] = canonicalTarget.ToString(),
            [merged.ToString()] = keepTarget.ToString(),
        };

        var added = Visa2014PassportIdMapAliasApplier.ApplyDedupeAliases(
            idMap,
            new Dictionary<Guid, Guid> { [merged] = canonical });

        Assert.Equal(0, added);
        Assert.Equal(keepTarget.ToString(), idMap[merged.ToString()]);
    }
}
