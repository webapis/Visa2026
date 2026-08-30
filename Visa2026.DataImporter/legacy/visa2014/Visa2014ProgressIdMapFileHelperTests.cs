using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014ProgressIdMapFileHelperTests
{
    [Fact]
    public void PruneByLegacyApplicationPrefixes_drops_matching_app_keys_only()
    {
        var appA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var appB = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var existing = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [$"{appA:D}:prepare"] = Guid.NewGuid().ToString(),
            [$"{appA:D}:ministry1"] = Guid.NewGuid().ToString(),
            [$"{appB:D}:prepare"] = Guid.NewGuid().ToString(),
            ["not-a-guid-key"] = Guid.NewGuid().ToString(),
        };

        var pruned = Visa2014ProgressIdMapFileHelper.PruneByLegacyApplicationPrefixes(existing, [appA]);

        Assert.Equal(2, pruned.Count);
        Assert.True(pruned.ContainsKey($"{appB:D}:prepare"));
        Assert.True(pruned.ContainsKey("not-a-guid-key"));
        Assert.False(pruned.ContainsKey($"{appA:D}:prepare"));
    }

    [Fact]
    public void PruneByLegacyApplicationPrefixes_empty_oids_keeps_all()
    {
        var existing = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["a"] = "1",
            ["b"] = "2",
        };

        var pruned = Visa2014ProgressIdMapFileHelper.PruneByLegacyApplicationPrefixes(existing, []);

        Assert.Equal(2, pruned.Count);
    }

    [Fact]
    public void ApplyUpdates_overwrites_and_adds()
    {
        var keep = Guid.NewGuid();
        var overwrite = Guid.NewGuid();
        var added = Guid.NewGuid();
        var existing = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["keep"] = keep.ToString(),
            ["overwrite"] = Guid.NewGuid().ToString(),
        };

        Visa2014ProgressIdMapFileHelper.ApplyUpdates(existing, new Dictionary<string, Guid>
        {
            ["overwrite"] = overwrite,
            ["new"] = added,
        });

        Assert.Equal(keep.ToString(), existing["keep"]);
        Assert.Equal(overwrite.ToString(), existing["overwrite"]);
        Assert.Equal(added.ToString(), existing["new"]);
    }

    [Fact]
    public void MergeFileUpdates_creates_file_and_merges()
    {
        var dir = Path.Combine(Path.GetTempPath(), "visa2014-progress-idmap-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "ApplicationProgress.json");
        try
        {
            var id = Guid.NewGuid();
            Visa2014ProgressIdMapFileHelper.MergeFileUpdates(path, new Dictionary<string, Guid>
            {
                ["k1"] = id,
            });

            Assert.True(File.Exists(path));
            var text = File.ReadAllText(path);
            Assert.Contains(id.ToString(), text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("k1", text, StringComparison.Ordinal);
        }
        finally
        {
            try { Directory.Delete(dir, recursive: true); } catch { /* ignore */ }
        }
    }
}
