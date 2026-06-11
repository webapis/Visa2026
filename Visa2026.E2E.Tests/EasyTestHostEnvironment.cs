namespace Visa2026.E2E.Tests;

/// <summary>EasyTest isolation endpoints — must not collide with IDE (:5000) or removed UI-scenario (:5052) hosts.</summary>
internal static class EasyTestHostEnvironment
{
    public const int EasyTestPort = 5050;
    public const int LegacyUiScenarioPort = 5052;

    public const string BaseUrl = "http://localhost:5050";
    public const string DatabaseName = "Visa2026EasyTest";
    public const string LocalDbServer = @"(localdb)\mssqllocaldb";

    public static string MasterConnectionString =>
        $"Server={LocalDbServer};Database=master;Trusted_Connection=True;TrustServerCertificate=True";

    public static string TestConnectionString =>
        $"Server={LocalDbServer};Database={DatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
}
