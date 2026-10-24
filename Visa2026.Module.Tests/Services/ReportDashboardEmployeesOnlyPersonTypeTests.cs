using Visa2026.Module.Services.ReportDashboard;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Invitation / Work Permit / Registration / Visa Extension stay Employees-only until
/// Family / Temporary Visitor sub-reports exist. Wrong mapping empties Overview or leaks rows.
/// </summary>
public sealed class ReportDashboardEmployeesOnlyPersonTypeTests
{
    [Theory]
    [InlineData(ReportDashboardCategory.Invitation)]
    [InlineData(ReportDashboardCategory.WorkPermit)]
    [InlineData(ReportDashboardCategory.Registration)]
    [InlineData(ReportDashboardCategory.VisaExtension)]
    public void RestrictsToEmployeesOnly_true_for_officer_person_document_categories(
        ReportDashboardCategory category) =>
        Assert.True(ReportDashboardCatalog.RestrictsToEmployeesOnly(category));

    [Theory]
    [InlineData(ReportDashboardCategory.Passport)]
    [InlineData(ReportDashboardCategory.Education)]
    [InlineData(ReportDashboardCategory.Travel)]
    [InlineData(ReportDashboardCategory.IncompletePersons)]
    public void RestrictsToEmployeesOnly_false_for_other_categories(
        ReportDashboardCategory category) =>
        Assert.False(ReportDashboardCatalog.RestrictsToEmployeesOnly(category));

    [Theory]
    [InlineData(ReportDashboardCategory.Invitation, ReportDashboardPersonType.FamilyMembers)]
    [InlineData(ReportDashboardCategory.WorkPermit, ReportDashboardPersonType.TemporaryVisitors)]
    [InlineData(ReportDashboardCategory.Registration, ReportDashboardPersonType.FamilyMembers)]
    [InlineData(ReportDashboardCategory.VisaExtension, ReportDashboardPersonType.TemporaryVisitors)]
    public void SuppressesSubReportsForPersonType_hides_employee_only_categories(
        ReportDashboardCategory category,
        ReportDashboardPersonType personType) =>
        Assert.True(ReportDashboardCatalog.SuppressesSubReportsForPersonType(category, personType));

    [Fact]
    public void SuppressesSubReportsForPersonType_false_for_employees_and_all()
    {
        Assert.False(ReportDashboardCatalog.SuppressesSubReportsForPersonType(
            ReportDashboardCategory.Invitation, ReportDashboardPersonType.Employees));
        Assert.False(ReportDashboardCatalog.SuppressesSubReportsForPersonType(
            ReportDashboardCategory.Invitation, ReportDashboardPersonType.All));
        Assert.False(ReportDashboardCatalog.SuppressesSubReportsForPersonType(
            ReportDashboardCategory.Passport, ReportDashboardPersonType.FamilyMembers));
    }

    [Fact]
    public void ResolveEffectivePersonType_maps_all_to_employees_for_restricted_categories()
    {
        Assert.Equal(
            ReportDashboardPersonType.Employees,
            ReportDashboardCatalog.ResolveEffectivePersonType(
                ReportDashboardCategory.Invitation, ReportDashboardPersonType.All));
        Assert.Equal(
            ReportDashboardPersonType.Employees,
            ReportDashboardCatalog.ResolveEffectivePersonType(
                ReportDashboardCategory.WorkPermit, ReportDashboardPersonType.Employees));
        Assert.Equal(
            ReportDashboardPersonType.FamilyMembers,
            ReportDashboardCatalog.ResolveEffectivePersonType(
                ReportDashboardCategory.Invitation, ReportDashboardPersonType.FamilyMembers));
        Assert.Equal(
            ReportDashboardPersonType.All,
            ReportDashboardCatalog.ResolveEffectivePersonType(
                ReportDashboardCategory.Passport, ReportDashboardPersonType.All));
    }

    [Fact]
    public void BuildListCriteria_returns_false_when_person_type_suppresses_category()
    {
        Assert.Equal(
            "False",
            ReportDashboardCatalog.BuildListCriteria(
                ReportDashboardPersonType.FamilyMembers,
                ReportDashboardCategory.Invitation,
                projectKey: null,
                statusLabel: null));
        Assert.Equal(
            "False",
            ReportDashboardCatalog.BuildListCriteria(
                ReportDashboardPersonType.TemporaryVisitors,
                ReportDashboardCategory.WorkPermit,
                projectKey: null,
                statusLabel: null,
                subReport: "active-by-project"));
    }

    [Fact]
    public void BuildListCriteria_all_on_invitation_does_not_suppress()
    {
        var criteria = ReportDashboardCatalog.BuildListCriteria(
            ReportDashboardPersonType.All,
            ReportDashboardCategory.Invitation,
            projectKey: null,
            statusLabel: null,
            subReport: "ready-by-project");

        Assert.NotEqual("False", criteria);
        Assert.False(string.IsNullOrWhiteSpace(criteria));
    }
}
