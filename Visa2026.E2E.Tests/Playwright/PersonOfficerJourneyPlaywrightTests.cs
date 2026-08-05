using System.Runtime.Versioning;
using System.Threading.Tasks;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests.Playwright;

/// <summary>
/// Officer person journey — Playwright E2E for Local (:5050) and Staging (live URL).
/// Filter: <c>Driver=Playwright</c>. Target: <c>VISA2026_E2E_TARGET=Local|Staging</c>.
/// </summary>
[Collection(PlaywrightE2eCollection.Name)]
[Trait("Driver", "Playwright")]
[Trait("Category", "UserManual")]
public sealed class PersonOfficerJourneyPlaywrightTests
{
    private readonly PlaywrightE2eFixture _fixture;

    public PersonOfficerJourneyPlaywrightTests(PlaywrightE2eFixture fixture) => _fixture = fixture;

    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("E2ETarget", "Local")]
    public async Task PersonOfficerJourney_LoginCreateEmployeeAddPassport_Local()
    {
        Assert.Equal(PlaywrightE2eTarget.Local, PlaywrightE2eEnvironment.Target);

        var journey = new PlaywrightPersonOfficerJourney(_fixture.Page);
        await journey.RunLoginCreateEmployeeAddPassportAsync(
            E2ETestPassportCreateOnlyJourneyValues.PersonalNumber,
            E2ETestPassportCreateOnlyJourneyValues.FirstName,
            E2ETestPassportCreateOnlyJourneyValues.LastName,
            E2ETestPassportCreateOnlyJourneyValues.FullName,
            E2ETestPassportCreateOnlyJourneyValues.PassportNumber);
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    [Trait("E2ETarget", "Staging")]
    public async Task PersonOfficerJourney_LoginCreateEmployeeAddPassport_Staging()
    {
        Assert.Equal(PlaywrightE2eTarget.Staging, PlaywrightE2eEnvironment.Target);

        var journey = new PlaywrightPersonOfficerJourney(_fixture.Page);
        await journey.RunLoginCreateEmployeeAddPassportAsync(
            E2ETestPassportCreateOnlyJourneyValues.PersonalNumber,
            E2ETestPassportCreateOnlyJourneyValues.FirstName,
            E2ETestPassportCreateOnlyJourneyValues.LastName,
            E2ETestPassportCreateOnlyJourneyValues.FullName,
            E2ETestPassportCreateOnlyJourneyValues.PassportNumber);
    }
}
