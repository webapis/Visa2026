using System;
using System.Runtime.Versioning;
using DevExpress.EasyTest.Framework;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Visa2026.E2E.Tests
{
    public class Visa2026Tests : IDisposable
    {
        private const string BlazorAppName = "Visa2026Blazor";
        private const string AppDBName     = "Visa2026EasyTest";

        private EasyTestFixtureContext FixtureContext { get; } = new EasyTestFixtureContext();

        [SupportedOSPlatform("windows")]
        public Visa2026Tests()
        {
            // Test edilecek Blazor uygulamasını ve DB'yi kaydet
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
        }

        public void Dispose()
        {
            FixtureContext.CloseRunningApplications();
        }

        // Basit duman testi: uygulama açılıyor mu?
        [Theory]
        [InlineData(BlazorAppName)]
        [SupportedOSPlatform("windows")]
        public void TestBlazorApp_Opens(string applicationName)
        {
            FixtureContext.DropDB(AppDBName);

            // v24.2: CreateApplicationContext => IApplicationContext döndürür
            var appContext = FixtureContext.CreateApplicationContext(applicationName);

            // v24.2: RunApplication parametresizdir
            appContext.RunApplication();

            // Buradan itibaren C# EasyTest API ile etkileşim:
            // Örnek: Logon -> Tasks'e Navigate
            appContext.GetForm().FillForm(("User Name", "Admin"));
            appContext.GetAction("Log In").Execute();
            appContext.Navigate("Tasks");
        }

        // (İSTEĞE BAĞLI) Başka akışlar burada C# API ile eklenebilir
        // Örn: kayıt ekleme, tablo kontrolü, validation, vs.
    }
}