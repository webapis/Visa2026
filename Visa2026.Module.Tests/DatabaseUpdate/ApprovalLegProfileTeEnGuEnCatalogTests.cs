using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate.LookupCatalogs;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Guards the shared TE-EN-GU-EN approval-leg chain seeded from tenant JSON
/// (host-start EnsureSynced path). Without this row, Choose Approval legs
/// cannot offer the four-step Türkmenenergo-Energetika-Gurluşyk-Energetika route.
/// </summary>
public class ApprovalLegProfileTeEnGuEnCatalogTests
{
    [Fact]
    public void TenantCatalog_IncludesTeEnGuEn_WithFourOrderedLegs()
    {
        var catalog = ApprovalLegProfileMinistryLegCatalogLoader.Load();
        Assert.NotNull(catalog);

        var row = Assert.Single(
            catalog!.Rows,
            r => string.Equals(r.Code, "TE-EN-GU-EN", System.StringComparison.OrdinalIgnoreCase));

        Assert.True(row.IsActive);
        Assert.Equal("te-en-gu-en", row.LocalizationKey);
        Assert.Contains("Türkmenenergo", row.NameTm, System.StringComparison.Ordinal);
        Assert.Contains("Gurluşyk", row.NameTm, System.StringComparison.Ordinal);

        var legs = row.MinistryLegs.OrderBy(l => l.Sequence).ToList();
        Assert.Equal(4, legs.Count);
        Assert.Equal(new[] { 1, 2, 3, 4 }, legs.Select(l => l.Sequence).ToArray());
        Assert.Equal(
            new[] { "Türkmenenergo", "Energetika", "Gurluşyk", "Energetika" },
            legs.Select(l => l.ApprovingMinistryShortNameTm).ToArray());
        Assert.All(legs, leg =>
        {
            Assert.Equal(10, leg.MaxDaysInReview);
            Assert.Equal(8, leg.WarningDaysBeforeMax);
        });

        Assert.Equal(
            "TE-EN-GU-EN",
            ApprovalLegProfileCodeHelper.ResolveCodeFromLegShortNames(
                legs.Select(l => l.ApprovingMinistryShortNameTm).ToArray()));
    }

    [Fact]
    public void TenantCatalog_EveryActiveRow_HasAtLeastOneLeg()
    {
        var catalog = ApprovalLegProfileMinistryLegCatalogLoader.Load();
        Assert.NotNull(catalog);
        Assert.NotEmpty(catalog!.Rows);

        Assert.All(
            catalog.Rows.Where(r => r.IsActive && !string.IsNullOrWhiteSpace(r.Code)),
            row => Assert.NotEmpty(row.MinistryLegs));
    }
}
