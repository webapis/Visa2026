using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public class ApplicationMigrationSlaProfileTypeLinkResolverTests
{
    [Theory]
    [InlineData("UP-TO-TWO-WEEKS", "UP TO TWO WEEKS")]
    [InlineData("UP-TO-3-DAYS", "UP-TO-3-DAYS")]
    [InlineData("up-to-one-month", "UP-TO-ONE-MONTH")]
    public void NormalizeProfileCode_IgnoresHyphensAndSpaces(string left, string right)
    {
        Assert.Equal(
            ApplicationMigrationSlaProfileTypeLinkResolver.NormalizeProfileCode(left),
            ApplicationMigrationSlaProfileTypeLinkResolver.NormalizeProfileCode(right));
    }

    [Fact]
    public void TryResolveProfile_MatchesNormalizedCode()
    {
        var profile = new ApplicationMigrationSlaProfile { Code = "UP TO TWO WEEKS" };
        var index = ApplicationMigrationSlaProfileTypeLinkResolver.BuildProfileIndex([profile]);

        var resolved = ApplicationMigrationSlaProfileTypeLinkResolver.TryResolveProfile(index, "UP-TO-TWO-WEEKS");

        Assert.Same(profile, resolved);
    }

    [Fact]
    public void TryResolveApplicationType_MatchesLocalizationKeyWhenNameDiffers()
    {
        var row = new ApplicationTypeConfigurationRow
        {
            Name = "App_Cancel_BZ",
            Code = "cancel_borderzone",
        };
        var applicationType = new ApplicationType
        {
            Name = "legacy-name",
            LocalizationKey = "App_Cancel_BZ",
        };

        var resolved = ApplicationMigrationSlaProfileTypeLinkResolver.TryResolveApplicationType(
            [applicationType],
            row);

        Assert.Same(applicationType, resolved);
    }
}
