using DevExpress.EasyTest.Framework;
using Xunit;
using System.Runtime.Versioning;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

// To run functional tests for ASP.NET Core Blazor XAF Applications,
// install browser drivers: https://www.selenium.dev/documentation/getting_started/installing_browser_drivers/.
//
// Refer to the following article for more information: https://docs.devexpress.com/eXpressAppFramework/403852/

namespace Visa2026.E2E.Tests
{
    public class Visa2026Tests : IDisposable
    {
        const string BlazorAppName = "Visa2026Blazor";
        const string AppDBName = "Visa2026EasyTest";
        EasyTestFixtureContext FixtureContext { get; } = new EasyTestFixtureContext();

        [SupportedOSPlatform("windows")]
        public Visa2026Tests()
        {
            FixtureContext.RegisterApplications(
                new BlazorApplicationOptions(BlazorAppName, string.Format(@"{0}\..\..\..\..\Visa2026.Blazor.Server", Environment.CurrentDirectory))
            );
            FixtureContext.RegisterDatabases(new DatabaseOptions(AppDBName, "Visa2026EasyTest", server: @"(localdb)\mssqllocaldb"));
        }
        public void Dispose()
        {
            FixtureContext.CloseRunningApplications();
        }
        [Theory]
        [InlineData(BlazorAppName)]
        [SupportedOSPlatform("windows")]
        public void TestBlazorApp(string applicationName)
        {
            FixtureContext.DropDB(AppDBName);
            var appContext = FixtureContext.CreateApplicationContext(applicationName);
            appContext.RunApplication();
        }

        [Theory]
        [InlineData(BlazorAppName, "sample.ets")]
        [SupportedOSPlatform("windows")]
        public void TestBlazorAppWithEts(string applicationName, string etsFileName)
        {
            FixtureContext.DropDB(AppDBName);
            var appContext = FixtureContext.CreateApplicationContext(applicationName);
            appContext.RunApplication();
            // Use the correct API: IApplicationContext.Application.ExecuteTestScript
            appContext.Application.ExecuteTestScript(etsFileName);
        }
    }
}
