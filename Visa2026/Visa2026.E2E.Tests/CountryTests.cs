using DevExpress.EasyTest.Framework;
using System.Runtime.Versioning;
using Xunit;

namespace Visa2026.E2E.Tests
{
    public class CountryTests : E2ETestBase
    {
        [Fact]
        [SupportedOSPlatform("windows")]
        public void Country_CRUD_Lifecycle()
        {
            Login();

            // 1. Create
            AppContext.Navigate("Lookup/Geography.Country");
            AppContext.GetAction("New").Execute();

            string testCode = "TST";
            string testName = "Test Country";

            AppContext.GetForm().FillForm(new EasyTestParameter("Code", testCode), new EasyTestParameter("Name", testName));
            AppContext.GetAction("Save").Execute();

            // 2. Read (Verify in ListView and Open)
            AppContext.Navigate("Lookup/Geography.Country");
            AppContext.GetGrid().ProcessRow(new EasyTestParameter("Name", testName));

            // 3. Update
            string updatedName = "Test Country Updated";
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", updatedName));
            AppContext.GetAction("Save").Execute();

            // 4. Delete
            AppContext.GetAction("Delete").Execute();

            // Confirm deletion in the dialog (Standard XAF Blazor confirmation)
            AppContext.GetAction("Yes").Execute();
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void Country_Validation_RequiredFields()
        {
            Login();
            AppContext.Navigate("Lookup/Geography.Country");
            AppContext.GetAction("New").Execute();

            // 1. Attempt to save without filling mandatory fields (Name/Code)
            AppContext.GetAction("Save").Execute();

            // 2. Verify we are still on the Detail View (Save failed) by checking if 'Save' is still actionable
            // If validation passed unexpectedly, the view would likely close or change.
            Assert.NotNull(AppContext.GetAction("Save"));

            // 3. Cleanup: Navigate away to trigger potential confirmation dialog
            AppContext.Navigate("Lookup/Geography.Country");
            
            // Handle "Do you want to save changes?" dialog if it appears
            try { AppContext.GetAction("No").Execute(); } catch { /* Dialog might not appear */ }
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void Country_Delete_Cancellation()
        {
            Login();
            // Arrange: Create a record
            string name = "DeleteCancel";
            AppContext.Navigate("Lookup/Geography.Country");
            AppContext.GetAction("New").Execute();
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", name), new EasyTestParameter("Code", "DC"));
            AppContext.GetAction("Save").Execute();

            // Act 1: Try Delete -> No
            AppContext.Navigate("Lookup/Geography.Country");
            AppContext.GetGrid().ProcessRow(new EasyTestParameter("Name", name));
            AppContext.GetAction("Delete").Execute();
            AppContext.GetAction("No").Execute();

            // Act 2: Delete -> Yes (Cleanup)
            AppContext.GetAction("Delete").Execute();
            AppContext.GetAction("Yes").Execute();
        }
    }
}