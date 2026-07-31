using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014TurkmenistanGeographyStoreTests
{
    [Fact]
    public void Rebuild_And_PreferLegacy_WhenMatchesReference()
    {
        var db = Visa2014TurkmenistanGeographyDbBuilder.Rebuild(
            Path.Combine(Path.GetTempPath(), $"tm-geo-{Guid.NewGuid():N}.db"));
        try
        {
            using var store = Visa2014TurkmenistanGeographyStore.Open(db);

            Assert.True(store.TryResolvePreferredRegionNameTm(
                "Serhetabat etraby", "Mary welayaty", out var keep));
            Assert.Contains("Mary", keep, StringComparison.OrdinalIgnoreCase);

            Assert.True(store.TryResolvePreferredRegionNameTm(
                "Serhetabat etraby", "Lebap welayaty", out var corrected));
            Assert.Contains("Mary", corrected, StringComparison.OrdinalIgnoreCase);

            Assert.True(store.TryResolvePreferredRegionNameTm(
                "Türkmenabat şäheri", "Balkan welayaty", out var lebap));
            Assert.Contains("Lebap", lebap, StringComparison.OrdinalIgnoreCase);

            Assert.True(store.TryResolvePreferredRegionNameTm(
                "Akbugday etraby", "Asgabat saheri", out var ahal));
            Assert.Contains("Ahal", ahal, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            try { File.Delete(db); } catch { /* ignore */ }
        }
    }
}