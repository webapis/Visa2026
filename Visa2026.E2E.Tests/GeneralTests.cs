using DevExpress.EasyTest.Framework;
using System.Runtime.Versioning;
using Xunit;

namespace Visa2026.E2E.Tests
{
    public class GeneralTests : E2ETestBase
    {
        [Fact]
        [SupportedOSPlatform("windows")]
        public void TestBlazorApp_Opens()
        {
            // The base class constructor has already started the application.
            // We can now interact with it.
            
            Login(); // Logs in as Admin
            NavigateEmployeesList();
        }
    }
}