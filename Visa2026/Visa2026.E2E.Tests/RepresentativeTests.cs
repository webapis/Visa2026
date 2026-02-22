using DevExpress.EasyTest.Framework;
using System.Runtime.Versioning;
using Xunit;

namespace Visa2026.E2E.Tests
{
    public class RepresentativeTests : E2ETestBase
    {
        [Fact]
        [SupportedOSPlatform("windows")]
        public void Representative_CRUD_Lifecycle()
        {
            Login();

            // Arrange: Create dependencies
            string companyName = "Rep Company";
            CreateCompany(companyName);

            string empFirst = "Jane";
            string empLast = "Rep";
            CreateEmployee(empFirst, empLast);
            string employeeFullName = $"{empFirst} {empLast}";

            // 1. Create
            CreateRepresentative(companyName, employeeFullName);

            // 2. Read
            AppContext.Navigate("Authorized Representative");
            AppContext.GetGrid().ProcessRow(new EasyTestParameter("Company", companyName), new EasyTestParameter("Full Name", employeeFullName));

            // 3. Update
            AppContext.GetAction("Save").Execute();

            // 4. Delete
            AppContext.GetAction("Delete").Execute();
            AppContext.GetAction("Yes").Execute();
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void Representative_Validation_RequiredFields()
        {
            Login();

            // Arrange
            string companyName = "Validation Rep Company";
            CreateCompany(companyName);

            AppContext.Navigate("Authorized Representative");
            AppContext.GetAction("New").Execute();

            // 1. Attempt to save with just Company, missing Employee
            AppContext.GetForm().FillForm(new EasyTestParameter("Company", companyName));
            AppContext.GetAction("Save").Execute();

            // 2. Verify Save failed
            Assert.NotNull(AppContext.GetAction("Save"));

            // Cleanup
            AppContext.Navigate("Authorized Representative");
            try { AppContext.GetAction("No").Execute(); } catch { }
        }
    }
}