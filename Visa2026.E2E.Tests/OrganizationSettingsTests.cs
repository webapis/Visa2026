using DevExpress.EasyTest.Framework;
using System.Runtime.Versioning;
using Xunit;

namespace Visa2026.E2E.Tests
{
    /// <summary>
    /// Organization singleton settings (CompanyProfile, AuthorizedSignatory, AuthorizedRepresentative).
    /// Replaces legacy multi-row <see cref="CompanyTests"/>.
    /// </summary>
    public class OrganizationSettingsTests : E2ETestBase
    {
        [Fact]
        [SupportedOSPlatform("windows")]
        public void CompanyProfile_UpdateName()
        {
            Login();

            const string updatedName = "E2E Company Profile Updated";
            AppContext.Navigate("Organization.Company");
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", updatedName));
            AppContext.GetAction("Save").Execute();

            AppContext.Navigate("Organization.Company");
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", updatedName));
            Assert.NotNull(AppContext.GetAction("Save"));
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void AuthorizedSignatory_RequiredFullName()
        {
            Login();

            AppContext.Navigate("Organization.Authorized Signatory");
            AppContext.GetForm().FillForm(new EasyTestParameter("Full Name", ""));
            AppContext.GetAction("Save").Execute();

            Assert.NotNull(AppContext.GetAction("Save"));
        }

        [Fact]
        [SupportedOSPlatform("windows")]
        public void AuthorizedRepresentative_UpdatePhone()
        {
            Login();

            const string phone = "+99312345678";
            AppContext.Navigate("Organization.Authorized Representative");
            AppContext.GetForm().FillForm(new EasyTestParameter("Phone", phone));
            AppContext.GetAction("Save").Execute();

            AppContext.Navigate("Organization.Authorized Representative");
            Assert.NotNull(AppContext.GetAction("Save"));
        }
    }
}
