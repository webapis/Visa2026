using System;
using System.Runtime.Versioning;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Visa2026.E2E.Tests
{
    public class Visa2026Tests : E2ETestBase
    {
        // Basit duman testi: uygulama açılıyor mu?
        [Fact]
        [SupportedOSPlatform("windows")]
        public void TestBlazorApp_Opens()
        {
            // The base class constructor (VisaTestBase) has already:
            // 1. Dropped the DB (FixtureContext.DropDB(AppDBName))
            // 2. Started the application (AppContext = FixtureContext.CreateApplicationContext(BlazorAppName); AppContext.RunApplication();)
            // AppContext is available for use here from the base class.

            // Buradan itibaren C# EasyTest API ile etkileşim:
            // Örnek: Logon -> Tasks'e Navigate
            Login();
            AppContext.Navigate("Employees");
        }

        // (İSTEĞE BAĞLI) Başka akışlar burada C# API ile eklenebilir
        // Örn: kayıt ekleme, tablo kontrolü, validation, vs.

      
    }
}