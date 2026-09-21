using System.Runtime.Versioning;
using System.Threading.Tasks;
using Xunit;

namespace Visa2026.E2E.Tests.Playwright;

[Collection(PlaywrightE2eCollection.Name)]
[Trait("Driver", "Playwright")]
[Trait("Category", "UserManual")]
[Trait("EmployeeSlice", "SignIn")]
public sealed class PersonOfficerSignInPlaywrightTests
{
    private readonly PlaywrightE2eFixture _fixture;

    public PersonOfficerSignInPlaywrightTests(PlaywrightE2eFixture fixture) => _fixture = fixture;

    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("E2ETarget", "Local")]
    [Trait("GuideSlug", "getting-started/login")]
    public Task PersonOfficerJourney_SignIn_Local() =>
        PlaywrightE2eTestRunner.RunAsync(_fixture, nameof(PersonOfficerJourney_SignIn_Local), async () =>
        {
            Assert.Equal(PlaywrightE2eTarget.Local, PlaywrightE2eEnvironment.Target);
            var journey = new PlaywrightPersonOfficerJourney(_fixture.Page);
            await journey.RunSignInToReportDashboardAsync();
        });
}
