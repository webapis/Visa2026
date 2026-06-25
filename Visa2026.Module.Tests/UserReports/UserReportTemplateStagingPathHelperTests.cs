using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.UserReports;

public class UserReportTemplateStagingPathHelperTests
{
    private static TemplateEditStagingOptions CreateOptions() =>
        new()
        {
            Enabled = true,
            FileNamePattern = "{safeName}{extension}",
        };

    [Fact]
    public void SanitizeTemplateName_replaces_invalid_characters()
    {
        var sanitized = UserReportTemplateStagingPathHelper.SanitizeTemplateName("GT-15/Elyasow: ckl");
        Assert.DoesNotContain('/', sanitized);
        Assert.DoesNotContain(':', sanitized);
        Assert.Contains("GT-15", sanitized);
    }

    [Fact]
    public void BuildDocumentFileName_uses_safe_template_name_and_extension()
    {
        var templateId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var options = CreateOptions();

        var fileName = UserReportTemplateStagingPathHelper.BuildDocumentFileName(
            options,
            templateId,
            "GT-15 Elyasow ckl",
            TemplateOutputFormat.Word);

        Assert.Equal("GT-15 Elyasow ckl.docx", fileName);
    }

    [Fact]
    public void BuildDocumentFileName_supports_template_id_token_when_configured()
    {
        var templateId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var options = CreateOptions();
        options.FileNamePattern = "{templateId}{extension}";

        var fileName = UserReportTemplateStagingPathHelper.BuildDocumentFileName(
            options,
            templateId,
            "GT-15 Elyasow ckl",
            TemplateOutputFormat.Word);

        Assert.Equal("3fa85f6457174562b3fc2c963f66afa6.docx", fileName);
    }

    [Fact]
    public void Meta_round_trip_writes_and_reads_json()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "visa2026-staging-meta-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        try
        {
            var metaPath = Path.Combine(tempDir, "sample.docx.meta.json");
            var meta = new UserReportTemplateStagingMeta
            {
                TemplateId = Guid.NewGuid(),
                TemplateName = "Test template",
                OutputFormat = TemplateOutputFormat.Excel,
                DocumentFileName = "sample.xlsx",
                ExportedAtUtc = DateTime.UtcNow,
                ExportedByUserName = "tester",
                SourceContentHashSha256 = "ABC123",
            };

            meta.WriteToFile(metaPath);
            var read = UserReportTemplateStagingMeta.ReadFromFile(metaPath);

            Assert.Equal(meta.TemplateId, read.TemplateId);
            Assert.Equal(meta.TemplateName, read.TemplateName);
            Assert.Equal(TemplateOutputFormat.Excel, read.OutputFormat);
            Assert.Equal(meta.SourceContentHashSha256, read.SourceContentHashSha256);
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }
}
