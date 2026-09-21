using System.Runtime.Versioning;
using System.Threading.Tasks;
using Xunit;

namespace Visa2026.E2E.Tests.Playwright;

[Collection(PlaywrightE2eCollection.Name)]
[Trait("Driver", "Playwright")]
[Trait("Category", "UserManual")]
[Trait("EmployeeSlice", "Find")]
public sealed class PersonOfficerFindEmployeePlaywrightTests
{
    private readonly PlaywrightE2eFixture _fixture;

    public PersonOfficerFindEmployeePlaywrightTests(PlaywrightE2eFixture fixture) => _fixture = fixture;

    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("E2ETarget", "Local")]
    [Trait("GuideSlug", "person/open-and-search")]
    public Task PersonOfficerJourney_FindEmployee_Local() =>
        PlaywrightE2eTestRunner.RunAsync(_fixture, nameof(PersonOfficerJourney_FindEmployee_Local), async () =>
        {
            Assert.Equal(PlaywrightE2eTarget.Local, PlaywrightE2eEnvironment.Target);
            var journey = new PlaywrightPersonOfficerJourney(_fixture.Page);
            await journey.RunFindEmployeeAsync();
        });
}
