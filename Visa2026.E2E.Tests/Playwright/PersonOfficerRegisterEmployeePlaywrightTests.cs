using System.Runtime.Versioning;
using System.Threading.Tasks;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests.Playwright;

[Collection(PlaywrightE2eCollection.Name)]
[Trait("Driver", "Playwright")]
[Trait("Category", "UserManual")]
[Trait("EmployeeSlice", "Register")]
public sealed class PersonOfficerRegisterEmployeePlaywrightTests
{
    private readonly PlaywrightE2eFixture _fixture;

    public PersonOfficerRegisterEmployeePlaywrightTests(PlaywrightE2eFixture fixture) => _fixture = fixture;

    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("E2ETarget", "Local")]
    [Trait("GuideSlug", "employee/register")]
    public Task PersonOfficerJourney_RegisterEmployee_Local() =>
        PlaywrightE2eTestRunner.RunAsync(_fixture, nameof(PersonOfficerJourney_RegisterEmployee_Local), async () =>
        {
            Assert.Equal(PlaywrightE2eTarget.Local, PlaywrightE2eEnvironment.Target);
            var journey = new PlaywrightPersonOfficerJourney(_fixture.Page);
            await journey.RunRegisterEmployeeAsync(
                E2ETestRegisterEmployeeJourneyValues.PersonalNumber,
                E2ETestRegisterEmployeeJourneyValues.FirstName,
                E2ETestRegisterEmployeeJourneyValues.LastName);
        });
}
