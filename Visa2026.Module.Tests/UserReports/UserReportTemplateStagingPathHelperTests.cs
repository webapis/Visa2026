using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.UserReports;

public class UserReportTemplateStagingPathHelperTests
{
    private static TemplateEditStagingOptions CreateOptions(string uncRoot) =>
        new()
        {
            Enabled = true,
            StagingRootUnc = uncRoot,
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
        var options = CreateOptions(@"\\127.0.0.1\Visa2026TemplateEdit");

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
        var options = CreateOptions(@"\\127.0.0.1\Visa2026TemplateEdit");
        options.FileNamePattern = "{templateId}{extension}";

        var fileName = UserReportTemplateStagingPathHelper.BuildDocumentFileName(
            options,
            templateId,
            "GT-15 Elyasow ckl",
            TemplateOutputFormat.Word);

        Assert.Equal("3fa85f6457174562b3fc2c963f66afa6.docx", fileName);
    }

    [Fact]
    public void BuildUncPath_uses_configured_unc_root()
    {
        var options = CreateOptions(@"\\127.0.0.1\Visa2026TemplateEdit");
        var unc = UserReportTemplateStagingPathHelper.BuildUncPath(options, "sample.docx");
        Assert.Equal(@"\\127.0.0.1\Visa2026TemplateEdit\sample.docx", unc);
    }

    [Fact]
    public void ResolveStagingUncRoot_rejects_local_drive_path()
    {
        var options = CreateOptions(@"D:\Visa2026TemplateEdit");
        var ex = Assert.Throws<InvalidOperationException>(() =>
            UserReportTemplateStagingPathHelper.ResolveStagingUncRoot(options));
        Assert.Contains("UNC", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveStagingUncRoot_rejects_relative_path()
    {
        var options = CreateOptions("TemplateEdit");
        var ex = Assert.Throws<InvalidOperationException>(() =>
            UserReportTemplateStagingPathHelper.ResolveStagingUncRoot(options));
        Assert.Contains("UNC", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveStagingIoRoot_prefers_local_path_when_configured()
    {
        var options = CreateOptions(@"\\server\Visa2026TemplateEdit-Prod");
        options.StagingLocalPath = @"C:\ProgramData\Visa2026\TemplateEdit\prod";

        var ioRoot = UserReportTemplateStagingPathHelper.ResolveStagingIoRoot(options);
        var uncRoot = UserReportTemplateStagingPathHelper.ResolveStagingUncRoot(options);

        Assert.Equal(options.StagingLocalPath, ioRoot);
        Assert.Equal(@"\\server\Visa2026TemplateEdit-Prod", uncRoot);
    }

    [Fact]
    public void BuildDocumentPath_uses_local_io_root_but_unc_for_officer_path()
    {
        var templateId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var options = CreateOptions(@"\\server\Visa2026TemplateEdit-Prod");
        options.StagingLocalPath = @"C:\ProgramData\Visa2026\TemplateEdit\prod";

        var documentPath = UserReportTemplateStagingPathHelper.BuildDocumentPath(
            options,
            templateId,
            "GT-15 Migrasiya ckl hat",
            TemplateOutputFormat.Word);
        var uncPath = UserReportTemplateStagingPathHelper.BuildUncPath(options, "sample.docx");

        Assert.StartsWith(@"C:\ProgramData\Visa2026\TemplateEdit\prod\", documentPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(@"\\server\Visa2026TemplateEdit-Prod\sample.docx", uncPath);
    }

    [Fact]
    public void TryBuildOfficeOpenUrl_builds_ms_word_link_for_unc_path()
    {
        var url = UserReportTemplateStagingPathHelper.TryBuildOfficeOpenUrl(
            @"\\127.0.0.1\Visa2026TemplateEdit\sample.docx",
            TemplateOutputFormat.Word);

        Assert.NotNull(url);
        Assert.StartsWith("ms-word:ofe|u|%5C%5C", url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("127.0.0.1", url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sample.docx", url, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("file://", url, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryBuildOfficeOpenUrl_returns_null_for_local_drive_path()
    {
        var url = UserReportTemplateStagingPathHelper.TryBuildOfficeOpenUrl(
            @"C:\Visa2026TemplateEdit\sample.docx",
            TemplateOutputFormat.Word);

        Assert.Null(url);
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
