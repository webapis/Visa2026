using System;

namespace Visa2026.E2E.Tests;

/// <summary>EasyTest isolation endpoints — must not collide with IDE (:5000) or removed UI-scenario (:5052) hosts.</summary>
internal static class EasyTestHostEnvironment
{
    public const int EasyTestPort = 5050;
    public const int LegacyUiScenarioPort = 5052;

    public const string BaseUrl = "http://localhost:5050";
    public const string DatabaseName = "visa2026_easytest";

    public static string PgHost =>
        Environment.GetEnvironmentVariable("VISA2026_EASYTEST_PG_HOST")
        ?? Environment.GetEnvironmentVariable("PG_HOST")
        ?? "localhost";

    public static string PgPort =>
        Environment.GetEnvironmentVariable("VISA2026_EASYTEST_PG_PORT")
        ?? Environment.GetEnvironmentVariable("PG_PORT")
        ?? "5432";

    public static string PgUser =>
        Environment.GetEnvironmentVariable("VISA2026_EASYTEST_PG_USER")
        ?? Environment.GetEnvironmentVariable("PG_USER")
        ?? "postgres";

    public static string PgPassword =>
        Environment.GetEnvironmentVariable("VISA2026_EASYTEST_PG_PASSWORD")
        ?? Environment.GetEnvironmentVariable("PG_PASSWORD")
        ?? "Visa2026Local";

    public static string MaintenanceConnectionString =>
        $"Host={PgHost};Port={PgPort};Database=postgres;Username={PgUser};Password={PgPassword}";

    public static string TestConnectionString =>
        $"Host={PgHost};Port={PgPort};Database={DatabaseName};Username={PgUser};Password={PgPassword};Persist Security Info=True;EFCoreProvider=Postgres";
}