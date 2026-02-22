using DevExpress.EasyTest.Framework;
using System.Runtime.Versioning;
using Xunit;

namespace Visa2026.E2E.Tests
{
    public class CompanyTests : E2ETestBase
    {
        [Fact]
        [SupportedOSPlatform("windows")]
        public void Company_CRUD_Lifecycle()
        {
            Login();

            // 1. Create Company

            string testName = "Test Company";
            AppContext.Navigate("Default.Company");
            AppContext.GetAction("New").Execute();
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", testName));
            AppContext.GetAction("Save").Execute();
            

            // 2. Read (Verify in ListView and Open)
            AppContext.Navigate("Company");
            AppContext.GetGrid().ProcessRow(new EasyTestParameter("Name", testName));
            
            AppContext.GetAction("Representatives").Execute();
            AppContext.GetAction("Representatives.New").Execute();
            AppContext.GetForm().FillForm(new EasyTestParameter("Is Local Employee", "True"));

            // Use the property name "LocalEmployee" (no space) to target the lookup editor action
            AppContext.GetAction("Local Employee.New").Execute();
            AppContext.GetForm().FillForm(new EasyTestParameter("First Name", "John"), new EasyTestParameter("Last Name", "Doe"));
            AppContext.GetAction("Save").Execute(); // Save Local Employee
            AppContext.GetAction("Save").Execute(); // Save Representative

         


            // 3. Update
            string updatedName = "Test Company Updated";
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", updatedName));
            AppContext.GetAction("Save").Execute();

            // 4. Delete
            AppContext.GetAction("Delete").Execute();

            // Confirm deletion in the dialog
            AppContext.GetAction("Yes").Execute();
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void Company_Validation_RequiredFields()
        {
            Login();
            AppContext.Navigate("Company");
            AppContext.GetAction("New").Execute();

            // 1. Attempt to save without filling mandatory fields (Name)
            AppContext.GetAction("Save").Execute();

            // 2. Verify we are still on the Detail View (Save failed) by checking if 'Save' is still actionable
            Assert.NotNull(AppContext.GetAction("Save"));

            // 3. Cleanup: Navigate away to trigger potential confirmation dialog
            AppContext.Navigate("Company");
            
            // Handle "Do you want to save changes?" dialog if it appears
            try { AppContext.GetAction("No").Execute(); } catch { /* Dialog might not appear */ }
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void Company_Delete_Cancellation()
        {
            Login();
            // Arrange: Create a record
            string name = "DeleteCancelCompany";
            CreateCompany(name);

            // Act 1: Try Delete -> No
            AppContext.Navigate("Company");
            AppContext.GetGrid().ProcessRow(new EasyTestParameter("Name", name));
            AppContext.GetAction("Delete").Execute();
            AppContext.GetAction("No").Execute();

            // Act 2: Delete -> Yes (Cleanup)
            AppContext.GetAction("Delete").Execute();
            AppContext.GetAction("Yes").Execute();
        }
    }
}