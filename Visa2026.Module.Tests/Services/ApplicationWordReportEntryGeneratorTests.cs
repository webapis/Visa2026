using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWordReportEntryGeneratorTests
{
    [Fact]
    public void ResolveDownloadFileName_uses_xlsx_when_catalog_key_is_profile_not_user()
    {
        var templateId = Guid.NewGuid();
        var template = new UserReportTemplate
        {
            ID = templateId,
            TemplateName = "Sanaw_clk_09",
            TemplateOutputFormat = TemplateOutputFormat.Excel,
        };

        var catalog = new[]
        {
            new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = $"profile:{Guid.NewGuid():D}",
                DisplayName = "Sanaw_clk_09",
                OutputFileName = "Sanaw_clk_09.xlsx",
                Kind = ApplicationWordReportPackageEntryKind.UserExcel,
                UserReportTemplateId = templateId,
            }
        };

        var name = ApplicationWordReportEntryGenerator.ResolveDownloadFileName(template, catalog);

        Assert.Equal("Sanaw_clk_09.xlsx", name);
    }

    [Fact]
    public void ResolveDownloadFileName_does_not_fallback_to_dated_docx_for_excel()
    {
        var template = new UserReportTemplate
        {
            ID = Guid.NewGuid(),
            TemplateName = "Sanaw_clk_09",
            TemplateOutputFormat = TemplateOutputFormat.Excel,
        };

        var name = ApplicationWordReportEntryGenerator.ResolveDownloadFileName(
            template,
            Array.Empty<ApplicationWordReportPackageCatalogEntry>());

        Assert.EndsWith(".xlsx", name, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(".docx", name, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("Sanaw_clk_09", name, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ResolveDownloadFileName_rewrites_docx_catalog_name_when_template_is_excel()
    {
        var templateId = Guid.NewGuid();
        var template = new UserReportTemplate
        {
            ID = templateId,
            TemplateName = "Sanaw_clk_09",
            TemplateOutputFormat = TemplateOutputFormat.Excel,
        };

        var catalog = new[]
        {
            new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = $"profile:{Guid.NewGuid():D}",
                DisplayName = "Sanaw_clk_09",
                OutputFileName = "Sanaw_clk_09.docx",
                Kind = ApplicationWordReportPackageEntryKind.UserWord,
                UserReportTemplateId = templateId,
            }
        };

        var name = ApplicationWordReportEntryGenerator.ResolveDownloadFileName(template, catalog);

        Assert.Equal("Sanaw_clk_09.xlsx", name);
    }

    [Fact]
    public void ResolveDownloadFileName_keeps_docx_for_word_templates()
    {
        var templateId = Guid.NewGuid();
        var template = new UserReportTemplate
        {
            ID = templateId,
            TemplateName = "Sanaw_ckl",
            TemplateOutputFormat = TemplateOutputFormat.Word,
        };

        var catalog = new[]
        {
            new ApplicationWordReportPackageCatalogEntry
            {
                EntryKey = $"user:{templateId:D}",
                DisplayName = "Sanaw_ckl",
                OutputFileName = "Sanaw_ckl.docx",
                Kind = ApplicationWordReportPackageEntryKind.UserWord,
                UserReportTemplateId = templateId,
            }
        };

        var name = ApplicationWordReportEntryGenerator.ResolveDownloadFileName(template, catalog);

        Assert.Equal("Sanaw_ckl.docx", name);
    }
}