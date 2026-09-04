using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileWizardTemplateScopeHelperTests
{
    [Fact]
    public void CloneToThisProfile_DoesNotRequireSharedConfirm()
    {
        Assert.True(ApplicationProfileWizardTemplateScopeHelper.IsCloneToThisProfile(
            ApplicationProfileTemplateCatalogScope.Global,
            ApplicationProfileTemplateCatalogScope.ProfileSpecific));
        Assert.False(ApplicationProfileWizardTemplateScopeHelper.RequiresSharedVisibilityConfirm(
            ApplicationProfileTemplateCatalogScope.Global,
            ApplicationProfileTemplateCatalogScope.ProfileSpecific));
    }

    [Fact]
    public void PromoteOrRecategorize_RequiresSharedConfirm()
    {
        Assert.True(ApplicationProfileWizardTemplateScopeHelper.RequiresSharedVisibilityConfirm(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific,
            ApplicationProfileTemplateCatalogScope.Global));
        Assert.True(ApplicationProfileWizardTemplateScopeHelper.RequiresSharedVisibilityConfirm(
            ApplicationProfileTemplateCatalogScope.Global,
            ApplicationProfileTemplateCatalogScope.Category));
        Assert.True(ApplicationProfileWizardTemplateScopeHelper.IsShared(
            ApplicationProfileTemplateCatalogScope.Global));
        Assert.True(ApplicationProfileWizardTemplateScopeHelper.IsShared(
            ApplicationProfileTemplateCatalogScope.Category));
        Assert.False(ApplicationProfileWizardTemplateScopeHelper.IsShared(
            ApplicationProfileTemplateCatalogScope.ProfileSpecific));
        Assert.Equal(
            ApplicationProfileTemplateCatalogScope.Global,
            ApplicationProfileWizardTemplateScopeHelper.SharedTarget(
                ApplicationProfileTemplateCatalogScope.ProfileSpecific));
        Assert.Equal(
            ApplicationProfileTemplateCatalogScope.Category,
            ApplicationProfileWizardTemplateScopeHelper.SharedTarget(
                ApplicationProfileTemplateCatalogScope.Category));
    }

    [Fact]
    public void BuildProfileCopyName_AvoidsTakenNames()
    {
        var taken = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Borcnama (Calik)",
        };

        var name = ApplicationProfileWizardTemplateScopeHelper.BuildProfileCopyName(
            "Borcnama",
            "Calik",
            taken.Contains);

        Assert.Equal("Borcnama (Calik) 2", name);
    }

    [Fact]
    public void TypeMatchesCategory_UsesIssueFlags()
    {
        var type = new ApplicationType { CanIssueInvitation = true, ShowInvitations = true };
        Assert.True(ApplicationProfileWizardTemplateScopeHelper.TypeMatchesCategory(
            type, ApplicationProfileWizardTemplateCatalog.CategoryInvitation));
        Assert.False(ApplicationProfileWizardTemplateScopeHelper.TypeMatchesCategory(
            type, ApplicationProfileWizardTemplateCatalog.CategoryRegistration));
    }
}
