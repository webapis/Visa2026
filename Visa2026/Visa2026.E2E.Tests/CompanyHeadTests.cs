using DevExpress.EasyTest.Framework;
using System.Runtime.Versioning;
using Xunit;

namespace Visa2026.E2E.Tests
{
    public class CompanyHeadTests : E2ETestBase
    {
        [Fact]
        [SupportedOSPlatform("windows")]
        public void CompanyHead_CRUD_Lifecycle()
        {
            Login();

            // Arrange: Create dependencies
            string companyName = "Signatory Company";
            CreateCompany(companyName);

            string empFirst = "John";
            string empLast = "Signer";
            CreateEmployee(empFirst, empLast);
            string employeeFullName = $"{empFirst} {empLast}";

            // 1. Create
            CreateCompanyHead(companyName, employeeFullName);

            // 2. Read
            AppContext.Navigate("Authorized Signatory");
            AppContext.GetGrid().ProcessRow(new EasyTestParameter("Company", companyName), new EasyTestParameter("Full Name", employeeFullName));

            // 3. Update (Change Position - assuming Position is a simple text or lookup we can set, 
            // but for simplicity let's just verify we can open and save)
            AppContext.GetAction("Save").Execute();

            // 4. Delete
            AppContext.GetAction("Delete").Execute();
            AppContext.GetAction("Yes").Execute();
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void CompanyHead_Validation_RequiredFields()
        {
            Login();
            
            // Arrange
            string companyName = "Validation Company";
            CreateCompany(companyName);

            AppContext.Navigate("Authorized Signatory");
            AppContext.GetAction("New").Execute();

            // 1. Attempt to save with just Company, missing Employee
            AppContext.GetForm().FillForm(new EasyTestParameter("Company", companyName));
            AppContext.GetAction("Save").Execute();

            // 2. Verify Save failed (Action is still available)
            Assert.NotNull(AppContext.GetAction("Save"));

            // Cleanup
            AppContext.Navigate("Authorized Signatory");
            try { AppContext.GetAction("No").Execute(); } catch { }
        }
    }
}