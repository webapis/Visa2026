using System;
using System.Collections.ObjectModel;
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