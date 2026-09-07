using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationTypeProfileLockTests
{
    [Theory]
    [InlineData("App_Inv", "get_invitation")]
    [InlineData("App_Inv_FM", "get_invitation_fm")]
    [InlineData("App_Inv_And_WP", "get_invitation_wp")]
    [InlineData("App_Inv_According_to_WP", "get_invitation_according_to_wp")]
    [InlineData("App_Sevice_Passport", "get_invitation_service_passport")]
    public void ResolveProfileCode_InvitationTypes_UseUniqueTemplateCodes(string typeName, string expectedCode)
    {
        Assert.Equal(expectedCode, Visa2014ApplicationTypeProfileLock.ResolveProfileCode(typeName));
        Assert.Equal(expectedCode, Visa2014ApplicationProfileResolver.ResolveProfileCodeByTypeName(typeName));
    }

    [Fact]
    public void TryGetBySourceComposite_FamilyInvitation_MapsToAppInvFm()
    {
        Assert.True(Visa2014ApplicationTypeProfileLock.TryGetBySourceComposite("F:0:na:na:na", out var row));
        Assert.Equal("App_Inv_FM", row.TargetApplicationTypeName);
        Assert.Equal("get_invitation_fm", row.ApplicationProfileCode);
        Assert.Equal("Çakylyk Almak FM", row.ApplicationProfileName);
    }

    [Fact]
    public void LookupTranslationsApplicationTypeValues_MatchLockTargets()
    {
        var solutionRoot = Visa2014ContentRoot.FindSolutionRoot();
        Assert.NotNull(solutionRoot);
        var yamlPath = Visa2014ContentRoot.LookupTranslationsPath(solutionRoot);
        Assert.NotNull(yamlPath);
        var catalogs = Visa2014LookupTranslator.Load(yamlPath!);
        Assert.True(catalogs.TryGetValue("ApplicationType", out var catalog));

        foreach (var (composite, targetType) in catalog.LegacyToTarget)
        {
            Assert.True(
                Visa2014ApplicationTypeProfileLock.TryGetBySourceComposite(composite, out var row),
                $"Lock missing sourceComposite '{composite}' (lookup target {targetType}).");
            Assert.Equal(targetType, row.TargetApplicationTypeName);
        }
    }
}