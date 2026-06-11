using System.Runtime.Versioning;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests;

/// <summary>
/// E2E-020 / scenario <c>person-passport-add-seeded-employee</c> — see <c>scenarios/ready/person-passport-add-seeded-employee.yaml</c>.
/// Adds a second passport on the seeded employee (<see cref="E2ETestDataSeed.PersonPersonalNumber"/>).
/// </summary>
public class PassportTests : E2ETestBase
{
    public PassportTests(EasyTestSessionFixture session) : base(session) { }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void Passport_AddOnSeededEmployee_SavesAndShowsPassportNumber()
    {
        Login(E2ETestLoginValues.StandardUserName, E2ETestLoginValues.StandardUserPassword);

        EnsureEmployeeParentForChildBoTest();
        ExecutePersonPassportsNestedNew();
        FillPassportRequiredFields();
        SavePassportDetail();

        AssertPassportDetailShowsNumber(E2ETestPassportCreateValues.PassportNumber);
    }
}
