using System;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileNestedTemplateCatalogHelperTests
{
    [Fact]
    public void IsVisibleForInstance_UnscopedProfileTemplate_IsVisible()
    {
        var template = ProfileTemplate();
        var app = ViaMinistryApp(new ProjectContract { ID = Guid.NewGuid() });

        Assert.True(ApplicationProfileNestedTemplateCatalogHelper.IsVisibleForInstance(template, app));
    }

    [Fact]
    public void IsVisibleForInstance_ViaMinistry_MatchesProjectContract()
    {
        var contract = new ProjectContract { ID = Guid.NewGuid() };
        var template = ProfileTemplate();
        template.ApplicableProjectContract = contract;
        template.ApplicableProjectContractId = contract.ID;

        Assert.True(ApplicationProfileNestedTemplateCatalogHelper.IsVisibleForInstance(
            template, ViaMinistryApp(contract)));
        Assert.False(ApplicationProfileNestedTemplateCatalogHelper.IsVisibleForInstance(
            template, ViaMinistryApp(new ProjectContract { ID = Guid.NewGuid() })));
    }

    [Fact]
    public void IsVisibleForInstance_DirectMigration_MatchesMigrationService()
    {
        var service = new MigrationService { ID = Guid.NewGuid() };
        var template = ProfileTemplate();
        template.ApplicableMigrationService = service;
        template.ApplicableMigrationServiceId = service.ID;

        var match = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            },
            MigrationService = service,
        };
        var other = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            },
            MigrationService = new MigrationService { ID = Guid.NewGuid() },
        };

        Assert.True(ApplicationProfileNestedTemplateCatalogHelper.IsVisibleForInstance(template, match));
        Assert.False(ApplicationProfileNestedTemplateCatalogHelper.IsVisibleForInstance(template, other));
    }

    [Fact]
    public void GetOrderedTemplates_HidesNonMatchingProfileSpecific()
    {
        var contract = new ProjectContract { ID = Guid.NewGuid() };
        var shown = ProfileTemplate();
        shown.TemplateName = "Shown";
        shown.ApplicableProjectContractId = contract.ID;
        var hidden = ProfileTemplate();
        hidden.TemplateName = "Hidden";
        hidden.ApplicableProjectContractId = Guid.NewGuid();
        var category = ProfileTemplate();
        category.TemplateName = "Category";
        category.CatalogScope = ApplicationProfileTemplateCatalogScope.Category;

        var app = ViaMinistryApp(contract);
        app.ApplicationProfile!.NestedTemplates = new ObservableCollection<ApplicationProfileTemplate>
        {
            shown, hidden, category,
        };

        var names = ApplicationProfileNestedTemplateCatalogHelper.GetOrderedTemplates(app)
            .Select(t => t.TemplateName)
            .ToList();

        Assert.Contains("Shown", names);
        Assert.Contains("Category", names);
        Assert.DoesNotContain("Hidden", names);
    }

    [Fact]
    public void GetOrderedTemplates_HidesRecycledProfileSpecific()
    {
        var live = ProfileTemplate();
        live.TemplateName = "Live";
        var recycled = ProfileTemplate();
        recycled.TemplateName = "Recycled";
        recycled.RecycledAtUtc = DateTime.UtcNow;

        var app = ViaMinistryApp(new ProjectContract { ID = Guid.NewGuid() });
        app.ApplicationProfile!.NestedTemplates = new ObservableCollection<ApplicationProfileTemplate>
        {
            live, recycled,
        };

        var names = ApplicationProfileNestedTemplateCatalogHelper.GetOrderedTemplates(app)
            .Select(t => t.TemplateName)
            .ToList();

        Assert.Contains("Live", names);
        Assert.DoesNotContain("Recycled", names);
        Assert.True(ApplicationProfileNestedTemplateCatalogHelper.UsesProfileNestedCatalog(app));
        Assert.Equal("Recycled", ApplicationProfileNestedTemplateCatalogHelper.GetRecycledTemplates(app).Single().TemplateName);
    }

    [Fact]
    public void UsesProfileNestedCatalog_TrueWhenOnlyRecycledRemain()
    {
        var recycled = ProfileTemplate();
        recycled.TemplateName = "SANAW_CLK_013";
        recycled.RecycledAtUtc = DateTime.UtcNow;

        var app = ViaMinistryApp(new ProjectContract { ID = Guid.NewGuid() });
        app.ApplicationProfile!.NestedTemplates = new ObservableCollection<ApplicationProfileTemplate>
        {
            recycled,
        };

        Assert.True(ApplicationProfileNestedTemplateCatalogHelper.UsesProfileNestedCatalog(app));
        Assert.Empty(ApplicationProfileNestedTemplateCatalogHelper.GetOrderedTemplates(app));
        Assert.Single(ApplicationProfileNestedTemplateCatalogHelper.GetRecycledTemplates(app));
    }

    [Fact]
    public void UsesProfileNestedCatalog_TrueWhenProfileHasNoNestedTemplates()
    {
        var app = ViaMinistryApp(new ProjectContract { ID = Guid.NewGuid() });
        app.ApplicationProfile!.NestedTemplates = new ObservableCollection<ApplicationProfileTemplate>();

        Assert.True(ApplicationProfileNestedTemplateCatalogHelper.UsesProfileNestedCatalog(app));
        Assert.Empty(ApplicationProfileNestedTemplateCatalogHelper.GetOrderedTemplates(app));
        Assert.Empty(ApplicationProfileNestedTemplateCatalogHelper.GetRecycledTemplates(app));
    }

    [Fact]
    public void UsesProfileNestedCatalog_FalseWhenNoProfile()
    {
        Assert.False(ApplicationProfileNestedTemplateCatalogHelper.UsesProfileNestedCatalog(
            new ApplicationProfileInstance()));
    }

    [Fact]
    public void PickMergeTemplate_same_name_keeps_excel_and_word_apart()
    {
        var wordId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var excelId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var matches = new[]
        {
            new UserReportTemplate
            {
                ID = wordId,
                TemplateName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK",
                TemplateOutputFormat = TemplateOutputFormat.Word,
            },
            new UserReportTemplate
            {
                ID = excelId,
                TemplateName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK",
                TemplateOutputFormat = TemplateOutputFormat.Excel,
            },
        };

        Assert.Equal(excelId, ApplicationProfileNestedTemplateCatalogHelper.PickMergeTemplate(
            matches, ApplicationProfileTemplateKind.Excel)!.ID);
        Assert.Equal(wordId, ApplicationProfileNestedTemplateCatalogHelper.PickMergeTemplate(
            matches, ApplicationProfileTemplateKind.Word)!.ID);
    }

    [Fact]
    public void PickMergeTemplate_does_not_revive_the_other_format_when_fallback_is_off()
    {
        var word = new UserReportTemplate
        {
            ID = Guid.NewGuid(),
            TemplateName = "SANAW",
            TemplateOutputFormat = TemplateOutputFormat.Word,
        };

        Assert.Null(ApplicationProfileNestedTemplateCatalogHelper.PickMergeTemplate(
            new[] { word },
            ApplicationProfileTemplateKind.Excel,
            allowUnmatchedFallback: false));
    }

    [Fact]
    public void WithProfileFile_excel_row_downloads_as_xlsx_when_shared_user_template_is_word()
    {
        var userTemplate = new UserReportTemplate
        {
            ID = Guid.NewGuid(),
            TemplateName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK",
            TemplateOutputFormat = TemplateOutputFormat.Word,
            ExcelMergeMode = ExcelMergeMode.SingleItem,
            TemplateFile = new FileData
            {
                FileName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK.docx",
                Content = new byte[] { 1, 2, 3 },
            },
        };
        var profileTemplate = new ApplicationProfileTemplate
        {
            TemplateName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK",
            TemplateKind = ApplicationProfileTemplateKind.Excel,
            TemplateFile = new FileData
            {
                FileName = "sanaw.xlsx",
                Content = new byte[] { 9, 9, 9, 9 },
            },
        };

        var merge = ApplicationProfileNestedTemplateCatalogHelper.WithProfileFile(userTemplate, profileTemplate);

        Assert.Equal(TemplateOutputFormat.Excel, merge.GetEffectiveOutputFormat());
        Assert.Equal(ExcelMergeMode.ItemList, merge.ExcelMergeMode);
        Assert.EndsWith(".xlsx", merge.TemplateFile.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(new byte[] { 9, 9, 9, 9 }, merge.TemplateFile.Content);
        Assert.Equal(TemplateOutputFormat.Word, userTemplate.GetEffectiveOutputFormat());
    }

    [Fact]
    public void WithProfileFile_excel_kind_keeps_word_when_the_saved_file_is_a_word_package()
    {
        var userTemplate = new UserReportTemplate
        {
            ID = Guid.NewGuid(),
            TemplateName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK",
            TemplateOutputFormat = TemplateOutputFormat.Word,
            ExcelMergeMode = ExcelMergeMode.ItemList,
            TemplateFile = new FileData
            {
                FileName = "shared.docx",
                Content = MinimalOpenXml("word/document.xml"),
            },
        };
        var profileTemplate = new ApplicationProfileTemplate
        {
            TemplateName = "DASARY_YURT_RAYATLARYNYN_SANAWY_CAKYLYK",
            TemplateKind = ApplicationProfileTemplateKind.Excel,
            TemplateFile = new FileData
            {
                FileName = "Dasary_yurt_rayatlarynyn_sanawy_cakylyk.docx",
                Content = MinimalOpenXml("word/document.xml"),
            },
        };

        var merge = ApplicationProfileNestedTemplateCatalogHelper.WithProfileFile(userTemplate, profileTemplate);

        Assert.Equal(TemplateOutputFormat.Word, merge.GetEffectiveOutputFormat());
        Assert.EndsWith(".docx", merge.TemplateFile.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(profileTemplate.TemplateFile.Content, merge.TemplateFile.Content);
    }

    private static byte[] MinimalOpenXml(string entryName)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry(entryName);
            using var writer = new StreamWriter(entry.Open());
            writer.Write("<xml/>");
        }

        return stream.ToArray();
    }

    private static ApplicationProfileTemplate ProfileTemplate() =>
        new()
        {
            CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            TemplateKind = ApplicationProfileTemplateKind.Word,
            TemplateName = "T",
        };

    private static ApplicationProfileInstance ViaMinistryApp(ProjectContract contract) =>
        new()
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            },
            ProjectContract = contract,
        };
}