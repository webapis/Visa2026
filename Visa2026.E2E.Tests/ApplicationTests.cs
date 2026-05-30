using System.Runtime.Versioning;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests
{
    /// <summary>
    /// E2E smoke for <see cref="Visa2026.Module.BusinessObjects.Application"/> creation.
    /// Uses ministry quick code <c>101</c> → <c>App_Inv</c> (invitation), seeded on DB update.
    /// </summary>
    public class ApplicationTests : E2ETestBase
    {
        /// <summary>Ministry <see cref="Visa2026.Module.BusinessObjects.ApplicationType.SelectionCode"/> for App_Inv.</summary>
        public const string AppInvSelectionCode = "101";

        /// <summary>
        /// Creates invitation application (101), adds one line, selects seeded person from Person dropdown.
        /// Flow: Login → Application New → type 101 → Save → New Application Item → Person lookup → Save.
        /// </summary>
        [Fact]
        [SupportedOSPlatform("windows")]
        public void Application_Create_AppInv_SavesWithNumber()
        {
            Login();

            CreateApplicationWithTypeCode(AppInvSelectionCode);

            var fullNumber = AppContext.GetForm().GetPropertyValue("Full Application Number");
            Assert.False(string.IsNullOrWhiteSpace(fullNumber),
                "Expected Full Application Number to be assigned after save.");

            AddApplicationItemWithPerson(E2ETestDataSeed.PersonFullName);

            var passportNumber = AppContext.GetForm().GetPropertyValue("Current Passport");
            Assert.False(string.IsNullOrWhiteSpace(passportNumber),
                "Expected Current Passport to be filled when Person is selected from the dropdown.");
        }
    }
}
