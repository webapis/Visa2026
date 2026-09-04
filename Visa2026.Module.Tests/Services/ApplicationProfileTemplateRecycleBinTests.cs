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
    public void CanMoveToRecycleBin_CategoryOrGlobal_False()
    {
        var category = ProfileSpecific();
        category.CatalogScope = ApplicationProfileTemplateCatalogScope.Category;
        var global = ProfileSpecific();
        global.CatalogScope = ApplicationProfileTemplateCatalogScope.Global;

        Assert.False(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(category));
        Assert.False(ApplicationProfileTemplateRecycleBin.CanMoveToRecycleBin(global));
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
    public void Recycle_Category_Throws()
    {
        var template = ProfileSpecific();
        template.CatalogScope = ApplicationProfileTemplateCatalogScope.Category;
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
    public void ShouldDeleteLinkedUserReportTemplate_OnlyUniqueProfileSpecific()
    {
        Assert.True(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific, otherNestedUsesSameName: false));
        Assert.False(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific, otherNestedUsesSameName: true));
        Assert.False(ApplicationProfileTemplateRecycleBin.ShouldDeleteLinkedUserReportTemplate(
            ApplicationProfileTemplateCatalogScope.Category, otherNestedUsesSameName: false));
    }

    private static ApplicationProfileTemplate ProfileSpecific() =>
        new()
        {
            CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            TemplateKind = ApplicationProfileTemplateKind.Excel,
            TemplateName = "SANAW_CLK_013",
        };
}
