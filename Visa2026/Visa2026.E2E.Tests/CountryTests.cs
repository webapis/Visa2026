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
    }
}