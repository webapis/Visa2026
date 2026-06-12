using System.Runtime.Versioning;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;
using Xunit.Abstractions;

namespace Visa2026.E2E.Tests;

/// <summary>
/// Single officer journey — scenario <c>person-officer-journey</c> (E2E-001).
/// Log on → employees list → create employee → add passport on same employee.
/// </summary>
public class PersonOfficerJourneyTests : E2ETestBase
{
    public PersonOfficerJourneyTests(EasyTestSessionFixture session, ITestOutputHelper output)
        : base(session, output)
    {
    }

    [Fact]
    [SupportedOSPlatform("windows")]
    public void PersonOfficerJourney_LoginCreateEmployeeAddPassport()
    {
        RunScenario(() =>
        {
            Login(E2ETestLoginValues.StandardUserName, E2ETestLoginValues.StandardUserPassword);
            AssertAuthenticatedAppShell();

            NavigateEmployeesList();
            Assert.NotNull(AppContext.GetAction("New"));

            CreateEmployeeWithRequiredFields();

            OpenEmployeeInListByPersonalNumber(E2ETestEmployeeCreateValues.PersonalNumber);
            Assert.Equal(E2ETestEmployeeCreateValues.FirstName, AppContext.GetForm().GetPropertyValue("First Name"));
            Assert.Equal(E2ETestEmployeeCreateValues.LastName, AppContext.GetForm().GetPropertyValue("Last Name"));
            Assert.Equal(
                E2ETestEmployeeCreateValues.PersonalNumber,
                AppContext.GetForm().GetPropertyValue("Personal Number"));

            ExecutePersonPassportsNestedNew();
            FillPassportRequiredFields();
            SavePassportDetail();
            AssertPassportDetailShowsNumber(E2ETestPassportCreateValues.PassportNumber);
        });
    }
}
