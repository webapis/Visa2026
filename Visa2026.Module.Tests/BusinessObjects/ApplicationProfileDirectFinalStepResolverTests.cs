using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

/// <summary>
/// Direct Issuance/Change finish with AS number received (PROCESS_ISSUED).
/// Other Direct ActionFamily values end at Migrasiýa gullugyna tabşyryldy.
/// </summary>
public class ApplicationProfileDirectFinalStepResolverTests
{
    [Theory]
    [InlineData(ApplicationProfileActionFamily.Issuance, true)]
    [InlineData(ApplicationProfileActionFamily.Change, true)]
    [InlineData(ApplicationProfileActionFamily.Registration, false)]
    [InlineData(ApplicationProfileActionFamily.Cancellation, false)]
    [InlineData(ApplicationProfileActionFamily.BusinessTrip, false)]
    public void DirectFinalStepRequiresAsNumber_OnlyIssuanceAndChange(
        ApplicationProfileActionFamily family,
        bool requiresAsNumber)
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            ActionFamily = family,
        };

        Assert.Equal(requiresAsNumber, ApplicationProfileConfigurationResolver.DirectFinalStepRequiresAsNumber(profile));
        Assert.Equal(
            !requiresAsNumber,
            ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(profile));
    }

    [Fact]
    public void DirectEndsAtMigrationSubmission_FalseForViaMinistries()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            ActionFamily = ApplicationProfileActionFamily.Registration,
        };

        Assert.False(ApplicationProfileConfigurationResolver.DirectFinalStepRequiresAsNumber(profile));
        Assert.False(ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(profile));
    }

    [Fact]
    public void DirectFinalTimelineLabel_IsAsNumberReceived()
    {
        Assert.Equal(
            "AS number received",
            ApplicationProfileConfigurationResolver.DirectFinalTimelineLabel(
                new ApplicationProfile { ActionFamily = ApplicationProfileActionFamily.Issuance }));
        Assert.Equal(
            "AS number received",
            ApplicationProfileConfigurationResolver.DirectFinalTimelineLabel((ApplicationProfile?)null));
    }

    [Fact]
    public void InstanceOverloads_UseResolvedProfileRoute()
    {
        var issuance = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
                ActionFamily = ApplicationProfileActionFamily.Issuance,
            },
        };
        var registration = new ApplicationProfileInstance
        {
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
                ActionFamily = ApplicationProfileActionFamily.Registration,
            },
        };

        Assert.True(ApplicationProfileConfigurationResolver.DirectFinalStepRequiresAsNumber(issuance));
        Assert.False(ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(issuance));
        Assert.False(ApplicationProfileConfigurationResolver.DirectFinalStepRequiresAsNumber(registration));
        Assert.True(ApplicationProfileConfigurationResolver.DirectEndsAtMigrationSubmission(registration));
    }
}
