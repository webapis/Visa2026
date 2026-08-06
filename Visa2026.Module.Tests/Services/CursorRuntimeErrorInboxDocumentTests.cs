using System.Text.Json;
using Visa2026.Module.BusinessObjects.Operations;
using Visa2026.Module.Services.RuntimeLogging;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CursorRuntimeErrorInboxDocumentTests
{
    [Fact]
    public void FromRow_CopiesFieldsAndSourceLabels()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var occurred = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        var batchId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var row = new ApplicationRuntimeLog
        {
            ID = id,
            OccurredAtUtc = occurred,
            Severity = ApplicationRuntimeLogSeverity.Error,
            ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
            ErrorCode = "PDF_FAIL",
            Category = "Pdf",
            Message = "fill failed",
            ExceptionType = "InvalidOperationException",
            StackTrace = "at X",
            UserName = "officer1",
            CorrelationId = "corr-1",
            RequestPath = "/api/x",
            MachineName = "host-a",
            DeploymentEnvironment = ApplicationRuntimeLogDeploymentEnvironment.IisProduction,
            ApplicationVersion = "1.2.3",
            RelatedBatchId = batchId,
            SentryEventId = "sentry-9"
        };

        var doc = CursorRuntimeErrorInboxDocument.FromRow(row, "Production", "visa2026_prod");

        Assert.Equal(id, doc.Id);
        Assert.Equal(occurred, doc.OccurredAtUtc);
        Assert.Equal(ApplicationRuntimeLogSeverity.Error, doc.Severity);
        Assert.Equal(ApplicationRuntimeLogResolutionStatus.Open, doc.ResolutionStatus);
        Assert.Equal("PDF_FAIL", doc.ErrorCode);
        Assert.Equal("Pdf", doc.Category);
        Assert.Equal("fill failed", doc.Message);
        Assert.Equal("InvalidOperationException", doc.ExceptionType);
        Assert.Equal("at X", doc.StackTrace);
        Assert.Equal("officer1", doc.UserName);
        Assert.Equal("corr-1", doc.CorrelationId);
        Assert.Equal("/api/x", doc.RequestPath);
        Assert.Equal("host-a", doc.MachineName);
        Assert.Equal(ApplicationRuntimeLogDeploymentEnvironment.IisProduction, doc.DeploymentEnvironment);
        Assert.Equal("1.2.3", doc.ApplicationVersion);
        Assert.Equal(batchId, doc.RelatedBatchId);
        Assert.Equal("sentry-9", doc.SentryEventId);
        Assert.Equal("Production", doc.SourceSlot);
        Assert.Equal("visa2026_prod", doc.SourceDatabase);
    }

    [Fact]
    public void TryWriteInboxFile_WritesJsonAndJsonl_SkipIfExists()
    {
        var inbox = Path.Combine(Path.GetTempPath(), "visa2026-inbox-" + Guid.NewGuid().ToString("N"));
        try
        {
            var id = Guid.NewGuid();
            var row = new ApplicationRuntimeLog
            {
                ID = id,
                OccurredAtUtc = DateTime.UtcNow,
                Severity = ApplicationRuntimeLogSeverity.Critical,
                ResolutionStatus = ApplicationRuntimeLogResolutionStatus.Open,
                Message = "boom"
            };

            Assert.True(ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                row, inbox, skipIfExists: true, "Demo", "visa2026_demo", out var writtenPath));
            Assert.True(File.Exists(writtenPath));
            Assert.True(File.Exists(Path.Combine(inbox, "inbox.jsonl")));

            using (var doc = JsonDocument.Parse(File.ReadAllText(writtenPath)))
            {
                Assert.Equal(id.ToString(), doc.RootElement.GetProperty("id").GetGuid().ToString());
                Assert.Equal("Demo", doc.RootElement.GetProperty("sourceSlot").GetString());
                Assert.Equal("visa2026_demo", doc.RootElement.GetProperty("sourceDatabase").GetString());
            }

            Assert.False(ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                row, inbox, skipIfExists: true, "Demo", "visa2026_demo", out _));

            Assert.True(ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                row, inbox, skipIfExists: false, "Demo", "visa2026_demo", out _));

            // WriteIndented JSON means each append is multi-line; count document id occurrences.
            var jsonl = File.ReadAllText(Path.Combine(inbox, "inbox.jsonl"));
            Assert.Equal(2, System.Text.RegularExpressions.Regex.Matches(jsonl, "\"id\"").Count);
        }
        finally
        {
            if (Directory.Exists(inbox))
                Directory.Delete(inbox, recursive: true);
        }
    }

    [Fact]
    public void TryWriteInboxFile_NullRow_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            ApplicationRuntimeLogCursorInboxFileHelper.TryWriteInboxFile(
                null, Path.GetTempPath(), skipIfExists: true, null, null, out _));
    }
}
