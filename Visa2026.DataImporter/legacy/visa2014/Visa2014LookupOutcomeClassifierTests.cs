using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014LookupOutcomeClassifierTests
{
    private static Dictionary<string, Visa2014LookupCatalog> Catalogs(
        string name,
        Dictionary<string, string> map,
        bool identityPassThrough = false) =>
        new(StringComparer.Ordinal)
        {
            [name] = new Visa2014LookupCatalog
            {
                TargetCatalog = name,
                TargetMatchProperty = "Name",
                UnmappedPolicy = "block_row",
                IdentityPassThrough = identityPassThrough,
                LegacyToTarget = new Dictionary<string, string>(map, StringComparer.Ordinal),
            },
        };

    [Fact]
    public void Classify_Blank_IsEmpty()
    {
        var catalogs = Catalogs("Country", new Dictionary<string, string>());

        var kind = Visa2014LookupOutcomeClassifier.Classify(catalogs, "Country", "  ", out var target);

        Assert.Equal(Visa2014LookupResolveKind.Empty, kind);
        Assert.Null(target);
    }

    [Fact]
    public void Classify_MissingCatalog_IsUnmapped()
    {
        var catalogs = Catalogs("Country", new Dictionary<string, string> { ["TR"] = "TUR" });

        var kind = Visa2014LookupOutcomeClassifier.Classify(catalogs, "City", "TR", out var target);

        Assert.Equal(Visa2014LookupResolveKind.Unmapped, kind);
        Assert.Null(target);
    }

    [Fact]
    public void Classify_ExactYaml_Wins()
    {
        var catalogs = Catalogs("Country", new Dictionary<string, string> { ["UAE"] = "ARE" });

        var kind = Visa2014LookupOutcomeClassifier.Classify(catalogs, "Country", " UAE ", out var target);

        Assert.Equal(Visa2014LookupResolveKind.ExactYaml, kind);
        Assert.Equal("ARE", target);
    }

    [Fact]
    public void Classify_NormalizedYaml_MatchesFoldedLegacyKey()
    {
        // KeysEqual folds Turkmen/diacritics — Ş vs S should normalize-match.
        var catalogs = Catalogs("MaritalStatus", new Dictionary<string, string>
        {
            ["Şingle"] = "Single",
        });

        var kind = Visa2014LookupOutcomeClassifier.Classify(
            catalogs,
            "MaritalStatus",
            "Single",
            out var target);

        Assert.Equal(Visa2014LookupResolveKind.NormalizedYaml, kind);
        Assert.Equal("Single", target);
    }

    [Fact]
    public void Classify_IdentityPassThrough_WhenEnabled()
    {
        var catalogs = Catalogs(
            "Country",
            new Dictionary<string, string>(),
            identityPassThrough: true);

        var kind = Visa2014LookupOutcomeClassifier.Classify(catalogs, "Country", "XYZ", out var target);

        Assert.Equal(Visa2014LookupResolveKind.IdentityPassThrough, kind);
        Assert.Equal("XYZ", target);
    }

    [Fact]
    public void Classify_Unmapped_WhenNoPassThrough()
    {
        var catalogs = Catalogs("Country", new Dictionary<string, string> { ["TR"] = "TUR" });

        var kind = Visa2014LookupOutcomeClassifier.Classify(catalogs, "Country", "ZZ", out var target);

        Assert.Equal(Visa2014LookupResolveKind.Unmapped, kind);
        Assert.Null(target);
    }

    [Theory]
    [InlineData(Visa2014LookupResolveKind.ExactYaml, null, null, SilentBuckets.ExplicitYaml)]
    [InlineData(Visa2014LookupResolveKind.NormalizedYaml, null, null, SilentBuckets.NormalizedYaml)]
    [InlineData(Visa2014LookupResolveKind.IdentityPassThrough, null, null, SilentBuckets.IdentityPassthrough)]
    public void ToSilentBucket_KnownKinds(
        Visa2014LookupResolveKind kind,
        string? expectedTarget,
        string? documentedDefault,
        string expectedBucket)
    {
        Assert.Equal(
            expectedBucket,
            Visa2014LookupOutcomeClassifier.ToSilentBucket(kind, expectedTarget, documentedDefault));
    }

    [Fact]
    public void ToSilentBucket_Empty_UsesNullAllowedOrDefaultApplied()
    {
        Assert.Equal(
            SilentBuckets.NullAllowed,
            Visa2014LookupOutcomeClassifier.ToSilentBucket(
                Visa2014LookupResolveKind.Empty,
                expectedTarget: null,
                documentedDefault: null));
        Assert.Equal(
            SilentBuckets.DefaultApplied,
            Visa2014LookupOutcomeClassifier.ToSilentBucket(
                Visa2014LookupResolveKind.Empty,
                expectedTarget: "X",
                documentedDefault: null));
    }

    [Fact]
    public void ToSilentBucket_Unmapped_WithExpected_IsDefaultApplied()
    {
        Assert.Equal(
            SilentBuckets.DefaultApplied,
            Visa2014LookupOutcomeClassifier.ToSilentBucket(
                Visa2014LookupResolveKind.Unmapped,
                expectedTarget: "Fallback",
                documentedDefault: "Other"));
        Assert.Equal(
            SilentBuckets.NullAllowed,
            Visa2014LookupOutcomeClassifier.ToSilentBucket(
                Visa2014LookupResolveKind.Unmapped,
                expectedTarget: null,
                documentedDefault: null));
    }

    [Fact]
    public void SilentBuckets_IsUnexpectedFail_OnlyActualWithoutExpected()
    {
        Assert.True(SilentBuckets.IsUnexpectedFail(SilentBuckets.ActualWithoutExpected));
        Assert.False(SilentBuckets.IsUnexpectedFail(SilentBuckets.NullAllowed));
        Assert.False(SilentBuckets.IsUnexpectedFail(SilentBuckets.Mismatch));
    }
}
