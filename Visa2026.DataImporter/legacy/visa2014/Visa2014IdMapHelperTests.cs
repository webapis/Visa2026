using System.Text.Json;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014IdMapHelperTests
{
    [Fact]
    public void LoadOrEmpty_MissingOrBlankPath_ReturnsEmpty()
    {
        Assert.Empty(Visa2014IdMapHelper.LoadOrEmpty(null));
        Assert.Empty(Visa2014IdMapHelper.LoadOrEmpty(""));
        Assert.Empty(Visa2014IdMapHelper.LoadOrEmpty(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json")));
    }

    [Fact]
    public async Task SaveAndLoad_RoundTripsGuidMap_SkippingInvalidLegacyKeys()
    {
        var dir = Path.Combine(Path.GetTempPath(), "visa2014-idmap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "Person.idmap.json");

        try
        {
            var legacy = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var target = Guid.Parse("22222222-2222-2222-2222-222222222222");
            await Visa2014IdMapHelper.SaveAsync(path, new Dictionary<Guid, Guid> { [legacy] = target });

            // Inject a non-Guid legacy key that Load must skip.
            var raw = JsonSerializer.Deserialize<Dictionary<string, string>>(await File.ReadAllTextAsync(path))!;
            raw["not-a-guid"] = Guid.NewGuid().ToString();
            raw[legacy.ToString()] = target.ToString();
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(raw));

            var map = Visa2014IdMapHelper.Load(path);

            Assert.Single(map);
            Assert.Equal(target, map[legacy]);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void LoadStringKeyMap_MissingFile_Throws()
    {
        var missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        Assert.Throws<FileNotFoundException>(() => Visa2014IdMapHelper.LoadStringKeyMap(missing));
    }

    [Fact]
    public void LoadStringKeyMap_SkipsUnparsableTargetGuids()
    {
        var dir = Path.Combine(Path.GetTempPath(), "visa2014-idmap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "string-keys.json");

        try
        {
            var goodTarget = Guid.Parse("33333333-3333-3333-3333-333333333333");
            File.WriteAllText(path, JsonSerializer.Serialize(new Dictionary<string, string>
            {
                ["legacy-a"] = goodTarget.ToString(),
                ["legacy-b"] = "not-guid",
            }));

            var map = Visa2014IdMapHelper.LoadStringKeyMap(path);

            Assert.Single(map);
            Assert.Equal(goodTarget, map["legacy-a"]);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* best-effort */ }
        }
    }
}
