using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class ReportDashboardCatalogBuildListCriteriaTests
{
    [Theory]
    [InlineData(PersonIncompleteDataLabels.PersonalData, "[MissingPersonalData] = True")]
    [InlineData(PersonIncompleteDataLabels.Passport, "[MissingPassport] = True")]
    [InlineData(PersonIncompleteDataLabels.Cv, "[MissingCv] = True")]
    [InlineData(PersonIncompleteDataLabels.Photo, "[MissingPhoto] = True")]
    [InlineData(PersonIncompleteDataLabels.Education, "[MissingEducation] = True")]
    [InlineData(PersonIncompleteDataLabels.Medical, "[MissingMedical] = True")]
    [InlineData(PersonIncompleteDataLabels.Address, "[MissingAddress] = True")]
    [InlineData(PersonIncompleteDataLabels.FamilyDocs, "[MissingFamilyDocs] = True")]
    [InlineData(PersonIncompleteDataLabels.Other, "[MissingOther] = True")]
    public void BuildListCriteria_IncompletePersons_MapsMissingAreaFlags(string statusLabel, string expectedFlag)
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.IncompletePersons,
            projectKey: null,
            statusLabel: statusLabel);

        Assert.Contains(expectedFlag, criteria);
        Assert.StartsWith("(True) And (", criteria);
    }

    [Fact]
    public void BuildListCriteria_IncompletePersons_UnknownStatus_KeepsTrueFlag()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.IncompletePersons,
            projectKey: null,
            statusLabel: "Not a real area");

        Assert.Contains("(True) And (True)", criteria);
    }

    [Fact]
    public void BuildListCriteria_IncompletePersons_EscapesQuotesInProjectKey()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.Employees,
            ReportDashboardCategory.IncompletePersons,
            projectKey: "O'Hara",
            statusLabel: PersonIncompleteDataLabels.Passport);

        Assert.Contains("[PersonRoleCode] =", criteria);
        Assert.Contains("[ProjectName] = 'O''Hara'", criteria);
        Assert.Contains("[MissingPassport] = True", criteria);
    }

    [Fact]
    public void BuildListCriteria_ApplicationViaMinistry_IncludesRouteCriteria()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.ApplicationViaMinistry,
            projectKey: null,
            statusLabel: null);

        Assert.Contains(ApplicationProgressRouteNavigation.CriteriaViaMinistries, criteria);
        Assert.DoesNotContain(ApplicationProgressRouteNavigation.CriteriaDirectMigration, criteria);
    }

    [Fact]
    public void BuildListCriteria_ApplicationDirectMigration_IncludesRouteCriteria()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.ApplicationDirectMigration,
            projectKey: null,
            statusLabel: null);

        Assert.Contains(ApplicationProgressRouteNavigation.CriteriaDirectMigration, criteria);
        Assert.DoesNotContain(ApplicationProgressRouteNavigation.CriteriaViaMinistries, criteria);
    }

    [Fact]
    public void BuildListCriteria_PersonSearch_AndsFoldedTokensAndEscapesQuotes()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.PersonSearch,
            projectKey: null,
            statusLabel: null,
            searchTerm: "O'Brien  Gül");

        Assert.Contains("Contains([SearchText], 'o''brien')", criteria);
        Assert.Contains("Contains([SearchText], 'gul')", criteria);
        Assert.Contains(" And ", criteria);
    }

    [Fact]
    public void BuildListCriteria_PersonSearch_NoVisaStatus_AllowsBlankOrLabel()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.PersonSearch,
            projectKey: null,
            statusLabel: ReportDashboardCatalog.PersonSearchNoVisaLabel);

        Assert.Contains("[StatusLabel] = '' Or [StatusLabel] Is Null Or [StatusLabel] = 'No visa'", criteria);
    }

    [Fact]
    public void BuildListCriteria_WorkPermit_ExcludesArchivedByDefault()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.WorkPermit,
            projectKey: null,
            statusLabel: null,
            includeArchivedPersons: false);

        Assert.Contains("[Person.IsArchived] = False", criteria);
    }

    [Fact]
    public void ApplicationProgressRouteFor_MapsMinistryAndDirectCategories()
    {
        Assert.Equal(
            ApplicationProgressRouteKind.ViaMinistries,
            ReportDashboardCatalog.ApplicationProgressRouteFor(ReportDashboardCategory.ApplicationViaMinistry));
        Assert.Equal(
            ApplicationProgressRouteKind.DirectToMigrationService,
            ReportDashboardCatalog.ApplicationProgressRouteFor(ReportDashboardCategory.ApplicationDirectMigration));
        Assert.Null(ReportDashboardCatalog.ApplicationProgressRouteFor(ReportDashboardCategory.VisaExtension));
    }
}
