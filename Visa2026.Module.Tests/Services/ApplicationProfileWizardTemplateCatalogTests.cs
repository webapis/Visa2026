using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileWizardTemplateCatalogTests
{
    [Fact]
    public void SuggestedCategoryKeys_UsesProduceAndRegistrationFamily()
    {
        var invitationWp = new ApplicationProfile
        {
            ProduceInvitation = true,
            ProduceWorkPermit = true,
        };
        var keys = ApplicationProfileWizardTemplateCatalog.SuggestedCategoryKeys(invitationWp);
        Assert.Contains(ApplicationProfileWizardTemplateCatalog.CategoryInvitation, keys);
        Assert.Contains(ApplicationProfileWizardTemplateCatalog.CategoryWorkPermit, keys);
        Assert.DoesNotContain(ApplicationProfileWizardTemplateCatalog.CategoryRegistration, keys);

        var registration = new ApplicationProfile
        {
            ActionFamily = ApplicationProfileActionFamily.Registration,
        };
        Assert.Equal(
            new[] { ApplicationProfileWizardTemplateCatalog.CategoryRegistration },
            ApplicationProfileWizardTemplateCatalog.SuggestedCategoryKeys(registration));
    }

    [Fact]
    public void IsSuggestedForProfile_GlobalAlways_TypedMustIntersect()
    {
        var suggested = new[]
        {
            ApplicationProfileWizardTemplateCatalog.CategoryInvitation,
            ApplicationProfileWizardTemplateCatalog.CategoryWorkPermit,
        };

        Assert.True(ApplicationProfileWizardTemplateCatalog.IsSuggestedForProfile(
            Row("Borcnama", Array.Empty<string>()), suggested));
        Assert.True(ApplicationProfileWizardTemplateCatalog.IsSuggestedForProfile(
            Row("Sanaw", new[] { ApplicationProfileWizardTemplateCatalog.CategoryWorkPermit }), suggested));
        Assert.False(ApplicationProfileWizardTemplateCatalog.IsSuggestedForProfile(
            Row("Hasaba", new[] { ApplicationProfileWizardTemplateCatalog.CategoryRegistration }), suggested));
    }

    [Fact]
    public void MergeShared_OrdersGlobalAndCategoryTogether()
    {
        var merged = ApplicationProfileWizardTemplateCatalog.MergeShared(
            new[] { Row("Borcnama", Array.Empty<string>(), sort: 49) },
            new[] { Row("Forma 16", new[] { ApplicationProfileWizardTemplateCatalog.CategoryVisa }, sort: 50) });

        Assert.Equal(new[] { "Borcnama", "Forma 16" }, merged.Select(r => r.Name).ToArray());
    }

    [Fact]
    public void MergeShared_OmitsGt15ContractLetters()
    {
        var merged = ApplicationProfileWizardTemplateCatalog.MergeShared(
            new[] { Row("Borcnama", Array.Empty<string>(), sort: 49) },
            new[]
            {
                Row("GT-15_Elyasow_ckl", new[] { ApplicationProfileWizardTemplateCatalog.CategoryInvitation }, sort: 58),
                Row("Forma 16", new[] { ApplicationProfileWizardTemplateCatalog.CategoryVisa }, sort: 50),
            });

        Assert.Equal(new[] { "Borcnama", "Forma 16" }, merged.Select(r => r.Name).ToArray());
        Assert.True(ApplicationProfileWizardTemplateCatalog.IsProfileSpecificUploadOnly("GT-15_MINSTROY_uzt"));
        Assert.False(ApplicationProfileWizardTemplateCatalog.IsProfileSpecificUploadOnly("Borcnama"));
    }

    [Fact]
    public void MatchesSharedSearch_FiltersByNameKindAndData()
    {
        var row = Row("Borcnama", Array.Empty<string>());
        Assert.True(ApplicationProfileWizardTemplateCatalog.MatchesSharedSearch(row, null));
        Assert.True(ApplicationProfileWizardTemplateCatalog.MatchesSharedSearch(row, "borc"));
        Assert.True(ApplicationProfileWizardTemplateCatalog.MatchesSharedSearch(row, "word"));
        Assert.False(ApplicationProfileWizardTemplateCatalog.MatchesSharedSearch(row, "Forma"));
    }

    private static ApplicationProfileWizardTemplateCatalog.CatalogRow Row(
        string name,
        IReadOnlyList<string> keys,
        int sort = 1) =>
        new()
        {
            UserReportTemplateId = Guid.NewGuid(),
            Name = name,
            Kind = ApplicationProfileTemplateKind.Word,
            SortOrder = sort,
            Scope = keys.Count == 0
                ? ApplicationProfileTemplateCatalogScope.Global
                : ApplicationProfileTemplateCatalogScope.Category,
            DataScope = ApplicationProfileTemplateDataScope.PeopleM2M,
            CategoryKeys = keys,
        };
}