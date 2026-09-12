using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileTemplateRecycleBinTests
{
    [Fact]
    public void CanMoveToRecycleBin_ProfileSpecificWord_True()
    {
        Assert.True(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(ProfileSpecific()));
    }

    [Fact]
    public void CanMoveToRecycleBin_CategoryOrGlobal_True()
    {
        var category = ProfileSpecific();
        category.CatalogScope = ApplicationProfileTemplateCatalogScope.Category;
        var global = ProfileSpecific();
        global.CatalogScope = ApplicationProfileTemplateCatalogScope.Global;

        Assert.True(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(category));
        Assert.True(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(global));
    }

    [Fact]
    public void CanMoveToRecycleBin_PdfForm_False()
    {
        var pdf = ProfileSpecific();
        pdf.TemplateKind = ApplicationProfileTemplateKind.PdfForm;
        Assert.False(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(pdf));
    }

    [Fact]
    public void CanMoveToRecycleBin_AlreadyRecycled_False()
    {
        var template = ProfileSpecific();
        template.RecycledAtUtc = DateTime.UtcNow;
        Assert.False(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(template));
    }

    [Fact]
    public void Recycle_SetsTimestampAndUser()
    {
        var template = ProfileSpecific();
        ApplicationProfileTemplateRecycleBin.Recycle(template, " officer ");

        Assert.NotNull(template.RecycledAtUtc);
        Assert.Equal("officer", template.RecycledByUserName);
        Assert.False(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(template));
        Assert.True(ApplicationProfileTemplateRecycleBin.IsRecycled(template));
    }

    [Fact]
    public void Recycle_SharedGlobal_SetsTimestamp()
    {
        var template = ProfileSpecific();
        template.CatalogScope = ApplicationProfileTemplateCatalogScope.Global;
        ApplicationProfileTemplateRecycleBin.Recycle(template, "officer");

        Assert.NotNull(template.RecycledAtUtc);
        Assert.Equal("officer", template.RecycledByUserName);
    }

    [Fact]
    public void Recycle_PdfForm_Throws()
    {
        var template = ProfileSpecific();
        template.TemplateKind = ApplicationProfileTemplateKind.PdfForm;
        Assert.Throws<InvalidOperationException>(() =>
            ApplicationProfileTemplateRecycleBin.Recycle(template, "officer"));
    }

    [Fact]
    public void Restore_ClearsRecycleFields()
    {
        var template = ProfileSpecific();
        ApplicationProfileTemplateRecycleBin.Recycle(template, "officer");
        ApplicationProfileTemplateRecycleBin.Restore(template);

        Assert.Null(template.RecycledAtUtc);
        Assert.Null(template.RecycledByUserName);
        Assert.True(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(template));
    }

    [Fact]
    public void ShouldDeleteLinkedUserReportTemplate_WhenNameIsUnique()
    {
        Assert.True(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            otherNestedUsesSameName: false));
        Assert.False(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            otherNestedUsesSameName: true));
        Assert.True(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            ApplicationProfileTemplateCatalogScope.Category, otherNestedUsesSameName: false));
        Assert.True(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific, otherNestedUsesSameName: false));
        Assert.False(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            ApplicationProfileTemplateCatalogScope.Global, otherNestedUsesSameName: true));
    }

    private static ApplicationProfileTemplate ProfileSpecific() =>
        new()
        {
            CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            TemplateKind = ApplicationProfileTemplateKind.Excel,
            TemplateName = "SANAW_CLK_013",
        };
}
