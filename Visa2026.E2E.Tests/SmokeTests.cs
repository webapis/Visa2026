using System.Runtime.Versioning;
using DevExpress.EasyTest.Framework;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.E2E.Tests
{
    /// <summary>
    /// Tier 0 CI gate smokes — scenario specs in <c>scenarios/ready/*.yaml</c> (Option A: yaml is metadata, C# runs steps).
    /// </summary>
    public class SmokeTests : E2ETestBase
    {
        public SmokeTests(EasyTestSessionFixture session) : base(session) { }

        /// <summary>
        /// E2E-001 / scenario <c>login-smoke</c> — see <c>scenarios/ready/login-smoke.yaml</c>.
        /// </summary>
        [Fact]
        [SupportedOSPlatform("windows")]
        public void LoginSmoke_AuthenticatedShellLoads()
        {
            Login(E2ETestLoginValues.StandardUserName, E2ETestLoginValues.StandardUserPassword);
            AssertAuthenticatedAppShell();
        }

        /// <summary>
        /// E2E-001-nav / scenario <c>login-nav-employees</c> — see <c>scenarios/ready/login-nav-employees.yaml</c>.
        /// </summary>
        [Fact]
        [SupportedOSPlatform("windows")]
        public void LoginNavEmployees_ListOpensWithNewAction()
        {
            Login(E2ETestLoginValues.StandardUserName, E2ETestLoginValues.StandardUserPassword);
            NavigateEmployeesList();
            Assert.NotNull(AppContext.GetAction("New"));
        }
    }
}
