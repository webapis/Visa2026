using System;
using System.IO;
using System.Text.Json;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Deterministic inbox file write / skip-if-exists for Cursor runtime-error triage.
/// Uses isolated temp directories (no shared filesystem).
/// </summary>
public sealed class ApplicationRuntimeLogCursorInboxFileHelperTests
{
    [Fact]
    public void TryWriteInboxFile_WritesJsonAndAppendsJsonl()
    {
        var dir = CreateTempInboxDir();
        try
        {
            var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var row = new ApplicationRuntimeLog
            {
                ID = id,
                OccurredAtUtc = new DateTime(2026, 9, 3, 10, 0, 0, DateTimeKind.Utc),
                Severity = ApplicationRuntimeLogSeverity.Error,
                ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
                ErrorCode = "E_TEST",
                Category = ApplicationRuntimeLogCategories.DocumentCopiesComponent,
                Message = "boom",
            };

            var wrote = ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                row,
                dir,
                skipIfExists: true,
                sourceSlot: "Demo",
                sourceDatabase: "visa2026_demo",
                out var writtenPath);

            Assert.True(wrote);
            Assert.Equal(Path.Combine(dir, $"{id:D}.json"), writtenPath);
            Assert.True(File.Exists(writtenPath!));

            using var doc = JsonDocument.Parse(File.ReadAllText(writtenPath!));
            Assert.Equal(id.ToString(), doc.RootElement.GetProperty("id").GetGuid().ToString());
            Assert.Equal("E_TEST", doc.RootElement.GetProperty("errorCode").GetString());
            Assert.Equal("Demo", doc.RootElement.GetProperty("sourceSlot").GetString());
            Assert.Equal("visa2026_demo", doc.RootElement.GetProperty("sourceDatabase").GetString());
            Assert.Equal(
                ApplicationRuntimeLogCategories.DocumentCopiesComponent,
                doc.RootElement.GetProperty("category").GetString());

            var jsonl = File.ReadAllText(Path.Combine(dir, "inbox.jsonl"));
            Assert.Contains("E_TEST", jsonl);
            Assert.Contains("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", jsonl);
        }
        finally
        {
            TryDeleteDir(dir);
        }
    }

    [Fact]
    public void TryWriteInboxFile_SkipIfExists_DoesNotOverwrite()
    {
        var dir = CreateTempInboxDir();
        try
        {
            var id = Guid.NewGuid();
            var path = Path.Combine(dir, $"{id:D}.json");
            File.WriteAllText(path, "{\"id\":\"" + id + "\",\"message\":\"original\"}");

            var row = new ApplicationRuntimeLog
            {
                ID = id,
                Message = "should-not-replace",
                Severity = ApplicationRuntimeLogSeverity.Error,
            };

            var wrote = ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                row,
                dir,
                skipIfExists: true,
                sourceSlot: null,
                sourceDatabase: null,
                out var writtenPath);

            Assert.False(wrote);
            Assert.Null(writtenPath);
            Assert.Contains("original", File.ReadAllText(path));
            Assert.False(File.Exists(Path.Combine(dir, "inbox.jsonl")));
        }
        finally
        {
            TryDeleteDir(dir);
        }
    }

    [Fact]
    public void TryWriteInboxFile_NullRow_Throws()
    {
        var dir = CreateTempInboxDir();
        try
        {
            Assert.Throws<ArgumentNullException>(() =>
                ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                    null!,
                    dir,
                    skipIfExists: false,
                    sourceSlot: null,
                    sourceDatabase: null,
                    out _));
        }
        finally
        {
            TryDeleteDir(dir);
        }
    }

    private static string CreateTempInboxDir() =>
        Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "visa2026-inbox-" + Guid.NewGuid().ToString("N"))).FullName;

    private static void TryDeleteDir(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // best-effort cleanup
        }
    }
}
