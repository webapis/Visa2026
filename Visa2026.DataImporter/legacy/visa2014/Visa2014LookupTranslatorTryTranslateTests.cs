using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014LookupTranslatorTryTranslateTests
{
    [Fact]
    public void TryTranslate_BlankLegacy_SucceedsWithNullTarget()
    {
        var catalogs = Catalog("Gender", blockRow: true, ("Erkek", "Male"));

        Assert.True(Visa2014LookupTranslator.TryTranslate(catalogs, "Gender", "  ", out var target, out var reason));
        Assert.Null(target);
        Assert.Null(reason);
    }

    [Fact]
    public void TryTranslate_UnknownCatalog_Fails()
    {
        var catalogs = Catalog("Gender", blockRow: true, ("Erkek", "Male"));

        Assert.False(Visa2014LookupTranslator.TryTranslate(catalogs, "Missing", "Erkek", out var target, out var reason));
        Assert.Null(target);
        Assert.Equal("unknown_catalog:Missing", reason);
    }

    [Fact]
    public void TryTranslate_ExactMap_ReturnsTarget()
    {
        var catalogs = Catalog("Gender", blockRow: true, ("Erkek", "Male"));

        Assert.True(Visa2014LookupTranslator.TryTranslate(catalogs, "Gender", "Erkek", out var target, out var reason));
        Assert.Equal("Male", target);
        Assert.Null(reason);
    }

    [Fact]
    public void TryTranslate_FoldMatchOnLegacyKey_ReturnsTarget()
    {
        // NormalizeKey folds Ý→y and lowercases; KeysEqual matches folded forms.
        var catalogs = Catalog("Relationship", blockRow: true, ("Aýaly", "Wife"));

        Assert.True(Visa2014LookupTranslator.TryTranslate(catalogs, "Relationship", "ayaly", out var target, out _));
        Assert.Equal("Wife", target);
    }

    [Fact]
    public void TryTranslate_FoldMatchOnTargetValue_ReturnsTarget()
    {
        var catalogs = Catalog("CityByName", blockRow: true, ("Ashgabat", "Aşgabat"));

        Assert.True(Visa2014LookupTranslator.TryTranslate(catalogs, "CityByName", "asgabat", out var target, out _));
        Assert.Equal("Aşgabat", target);
    }

    [Fact]
    public void TryTranslate_IdentityPassThrough_ReturnsTrimmedLegacy()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal)
        {
            ["ProjectContract"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "ProjectContract",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "block_row",
                IdentityPassThrough = true,
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal),
            },
        };

        Assert.True(Visa2014LookupTranslator.TryTranslate(
            catalogs, "ProjectContract", "  C-100  ", out var target, out var reason));
        Assert.Equal("C-100", target);
        Assert.Null(reason);
    }

    [Fact]
    public void TryTranslate_UnmappedBlockRow_FailsWithReason()
    {
        var catalogs = Catalog("Gender", blockRow: true, ("Erkek", "Male"));

        Assert.False(Visa2014LookupTranslator.TryTranslate(catalogs, "Gender", "Unknown", out var target, out var reason));
        Assert.Null(target);
        Assert.Equal("unmapped_lookup:Gender:Unknown", reason);
    }

    [Theory]
    [InlineData("allow_null")]
    [InlineData("skip_row")]
    public void TryTranslate_UnmappedAllowPolicies_SucceedWithNullTarget(string policy)
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal)
        {
            ["Gender"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "Gender",
                TargetMatchProperty = "Name",
                UnmappedPolicy = policy,
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Erkek"] = "Male",
                },
            },
        };

        Assert.True(Visa2014LookupTranslator.TryTranslate(catalogs, "Gender", "Unknown", out var target, out var reason));
        Assert.Null(target);
        Assert.Equal("unmapped_lookup:Gender:Unknown", reason);
    }

    private static Dictionary<string, Visa2014LookupCatalog> Catalog(
        string name,
        bool blockRow,
        params (string Legacy, string Target)[] pairs)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (legacy, target) in pairs)
            map[legacy] = target;

        return new Dictionary<string, Visa2014LookupCatalog>(StringComparer.Ordinal)
        {
            [name] = new Visa2014LookupCatalog
            {
                TargetCatalog = name,
                TargetMatchProperty = "Name",
                UnmappedPolicy = blockRow ? "block_row" : "allow_null",
                LegacyToTarget = map,
            },
        };
    }
}
