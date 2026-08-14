using System.Text.Json;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014SyncUpsertHelperTests
{
    [Fact]
    public void WriteSyncProgressFile_NullPath_DoesNotThrow()
    {
        Visa2014SyncUpsertHelper.WriteSyncProgressFile(
            idMapOutputPath: null,
            entityName: "Person",
            processed: 1,
            total: 10,
            updated: 0,
            inserted: 1,
            skippedUnchanged: 0,
            failed: 0);
    }

    [Fact]
    public void WriteSyncProgressFile_WritesSidecarJsonWithPhaseAndPercent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "visa2014-sync-progress-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var idMapPath = Path.Combine(tempDir, "person.idmap.json");
            File.WriteAllText(idMapPath, "{}");

            Visa2014SyncUpsertHelper.WriteSyncProgressFile(
                idMapOutputPath: idMapPath,
                entityName: "Person",
                processed: 25,
                total: 100,
                updated: 3,
                inserted: 20,
                skippedUnchanged: 1,
                failed: 1,
                phase: "posting");

            var progressPath = Path.Combine(tempDir, "Person.sync-progress.json");
            Assert.True(File.Exists(progressPath));

            using var doc = JsonDocument.Parse(File.ReadAllText(progressPath));
            var root = doc.RootElement;
            Assert.Equal("Person", root.GetProperty("entity").GetString());
            Assert.Equal(25, root.GetProperty("processed").GetInt32());
            Assert.Equal(100, root.GetProperty("total").GetInt32());
            Assert.Equal(25, root.GetProperty("percent").GetDouble());
            Assert.Equal(3, root.GetProperty("updated").GetInt32());
            Assert.Equal(20, root.GetProperty("inserted").GetInt32());
            Assert.Equal(1, root.GetProperty("skippedUnchanged").GetInt32());
            Assert.Equal(1, root.GetProperty("failed").GetInt32());
            Assert.Equal("posting", root.GetProperty("phase").GetString());
            Assert.False(string.IsNullOrWhiteSpace(root.GetProperty("utc").GetString()));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void WriteSyncProgressFile_ZeroTotal_WritesZeroPercentWithoutPhase()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "visa2014-sync-progress-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var idMapPath = Path.Combine(tempDir, "visa.idmap.json");

            Visa2014SyncUpsertHelper.WriteSyncProgressFile(
                idMapOutputPath: idMapPath,
                entityName: "Visa",
                processed: 0,
                total: 0,
                updated: 0,
                inserted: 0,
                skippedUnchanged: 0,
                failed: 0);

            var progressPath = Path.Combine(tempDir, "Visa.sync-progress.json");
            using var doc = JsonDocument.Parse(File.ReadAllText(progressPath));
            Assert.Equal(0, doc.RootElement.GetProperty("percent").GetDouble());
            Assert.False(doc.RootElement.TryGetProperty("phase", out _));
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void ReportImportLoopProgress_SkipsNonIntervalUntilFinal()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "visa2014-sync-progress-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var idMapPath = Path.Combine(tempDir, "item.idmap.json");

            Visa2014SyncUpsertHelper.ReportImportLoopProgress(
                idMapPath, "Item", processed: 50, total: 200, inserted: 40, failed: 0, skipped: 5, interval: 100);
            Assert.False(File.Exists(Path.Combine(tempDir, "Item.sync-progress.json")));

            Visa2014SyncUpsertHelper.ReportImportLoopProgress(
                idMapPath, "Item", processed: 100, total: 200, inserted: 90, failed: 1, skipped: 5, interval: 100);
            Assert.True(File.Exists(Path.Combine(tempDir, "Item.sync-progress.json")));

            File.Delete(Path.Combine(tempDir, "Item.sync-progress.json"));

            Visa2014SyncUpsertHelper.ReportImportLoopProgress(
                idMapPath, "Item", processed: 200, total: 200, inserted: 190, failed: 2, skipped: 8, interval: 100);
            using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(tempDir, "Item.sync-progress.json")));
            Assert.Equal(200, doc.RootElement.GetProperty("processed").GetInt32());
            Assert.Equal(190, doc.RootElement.GetProperty("inserted").GetInt32());
            Assert.Equal(8, doc.RootElement.GetProperty("skippedUnchanged").GetInt32());
            Assert.Equal("posting", doc.RootElement.GetProperty("phase").GetString());
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { /* best-effort */ }
        }
    }

    [Fact]
    public void ReportImportLoopProgress_ZeroTotal_IsNoOp()
    {
        Visa2014SyncUpsertHelper.ReportImportLoopProgress(
            idMapOutputPath: null,
            entityName: "Person",
            processed: 0,
            total: 0,
            inserted: 0,
            failed: 0);
    }
}
