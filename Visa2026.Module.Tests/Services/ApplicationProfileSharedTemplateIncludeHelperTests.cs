using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Visa2026.Module.Services.WordReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileSharedTemplateIncludeHelperTests
{
    [Fact]
    public void ShowsSharedChip_True_ForCategoryAndGlobal()
    {
        Assert.True(ApplicationProfileSharedTemplateIncludeHelper.ShowsSharedChip(
            ApplicationProfileTemplateCatalogScope.Category));
        Assert.True(ApplicationProfileSharedTemplateIncludeHelper.ShowsSharedChip(
            ApplicationProfileTemplateCatalogScope.Global));
        Assert.False(ApplicationProfileSharedTemplateIncludeHelper.ShowsSharedChip(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific));
    }

    [Fact]
    public void CatalogEntry_ShowsSharedChip_FollowsCatalogScope()
    {
        var shared = new ApplicationWordReportPackageCatalogEntry
        {
            EntryKey = "profile:" + Guid.NewGuid().ToString("D"),
            DisplayName = "SANAW_CKL_002",
            CatalogScope = ApplicationProfileTemplateCatalogScope.Global,
            IsSharedIncluded = true,
        };
        var local = new ApplicationWordReportPackageCatalogEntry
        {
            EntryKey = "profile:" + Guid.NewGuid().ToString("D"),
            DisplayName = "SAZAKOW_5",
            CatalogScope = ApplicationProfileTemplateCatalogScope.ProfileSpecific,
        };

        Assert.True(shared.ShowsSharedChip);
        Assert.True(shared.IsSharedIncluded);
        Assert.False(local.ShowsSharedChip);
        Assert.False(local.IsSharedIncluded);
    }

    [Fact]
    public void UserEntryKey_UsesUserPrefix()
    {
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        Assert.Equal("user:aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee", ApplicationWordReportPackageCatalogService.BuildUserEntryKey(id));
    }
}