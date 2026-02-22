using System;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using DevExpress.EasyTest.Framework;
using Xunit;

namespace Visa2026.E2E.Tests
{
    [SupportedOSPlatform("windows")]
    public abstract class E2ETestBase : IDisposable, IAsyncLifetime
    {
        protected const string BlazorAppName = "Visa2026Blazor";
        protected const string AppDBName = "Visa2026EasyTest";
        private EasyTestFixtureContext FixtureContext { get; }

        protected IApplicationContext AppContext { get; }

        protected E2ETestBase()
        {
            FixtureContext = new EasyTestFixtureContext();

            FixtureContext.RegisterApplications(
                new BlazorApplicationOptions(
                    BlazorAppName,
                    string.Format(@"{0}\..\..\..\..\Visa2026.Blazor.Server", Environment.CurrentDirectory)
                )
            );

            FixtureContext.RegisterDatabases(
                new DatabaseOptions(
                    AppDBName,
                    "Visa2026EasyTest",
                    server: @"(localdb)\mssqllocaldb"
                )
            );

            AppContext = FixtureContext.CreateApplicationContext(BlazorAppName);
        }

        public Task InitializeAsync()
        {
            // 1. Clean Database (Clean on Start)
            FixtureContext.DropDB(AppDBName);

            // 2. Start Application
            AppContext.RunApplication();
            return Task.CompletedTask;
        }

        protected void Login(string userName = "Admin")
        {
            AppContext.GetForm().FillForm(new EasyTestParameter("User Name", userName));
            // In this project, password is empty for test users.
            AppContext.GetAction("Log In").Execute();
        }

        protected void CreateCountry(string name, string code)
        {
            AppContext.Navigate("Lookup/Geography.Country");
            AppContext.GetAction("New").Execute();
            AppContext.GetForm().FillForm(new EasyTestParameter("Name", name), new EasyTestParameter("Code", code));
            AppContext.GetAction("Save").Execute();
        }

        public void Dispose()
        {
            // 3. Cleanup
            FixtureContext.CloseRunningApplications();
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}