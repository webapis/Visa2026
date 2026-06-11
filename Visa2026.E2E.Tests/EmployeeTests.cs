using System.Runtime.Versioning;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests
{
    /// <summary>
    /// E2E-010 / scenario <c>person-employee-create</c> — see <c>scenarios/ready/person-employee-create.yaml</c>.
    /// Creates employee with required Person fields on <c>Person_ListView_Employees</c>.
    /// </summary>
    public class EmployeeTests : E2ETestBase
    {
        public EmployeeTests(EasyTestSessionFixture session) : base(session) { }

        /// <summary>
        /// Steps: login → New employee → fill required fields → Save → open list row → assert properties.
        /// </summary>
        [Fact]
        [SupportedOSPlatform("windows")]
        public void Employee_Create_RequiredFields_SavesAndAppearsInList()
        {
            Login(E2ETestLoginValues.StandardUserName, E2ETestLoginValues.StandardUserPassword);

            CreateEmployeeWithRequiredFields();

            OpenEmployeeInListByPersonalNumber(E2ETestEmployeeCreateValues.PersonalNumber);

            string firstName = AppContext.GetForm().GetPropertyValue("First Name");
            string lastName = AppContext.GetForm().GetPropertyValue("Last Name");
            Assert.Equal(E2ETestEmployeeCreateValues.FirstName, firstName);
            Assert.Equal(E2ETestEmployeeCreateValues.LastName, lastName);

            string personalNumber = AppContext.GetForm().GetPropertyValue("Personal Number");
            Assert.Equal(E2ETestEmployeeCreateValues.PersonalNumber, personalNumber);
        }
    }
}
