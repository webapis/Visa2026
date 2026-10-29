using System;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Aktiv personeller must hide visas linked to cancellation or check-out cases,
/// and keep RegistrationKind in the definition so host-start heal recreates stale views.
/// </summary>
public class VisaAktivPersonellerViewSqlContractTests
{
    private static string LoadSql() =>
        ReportDashboardSqlViewResource.Load("vw_rd_visa_aktiv_personeller.postgres.sql");

    [Fact]
    public void Sql_ExcludesCancellationAndCheckOutLinkedVisas()
    {
        var sql = LoadSql();

        Assert.Contains("open_visas", sql, StringComparison.Ordinal);
        Assert.Contains("ApplicationProfileInstanceVisas", sql, StringComparison.Ordinal);
        // Cancellation family (enum value 1) — link alone closes the visa.
        Assert.Contains(
            $"ap.\"ActionFamily\" = {(int)ApplicationProfileActionFamily.Cancellation}",
            sql,
            StringComparison.Ordinal);
        // Registration + CheckOut (enum values 2 / 2), including check_out_internal.
        Assert.Contains(
            $"ap.\"ActionFamily\" = {(int)ApplicationProfileActionFamily.Registration}",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            $"COALESCE(ap.\"RegistrationKind\", 0) = {(int)ApplicationProfileRegistrationKind.CheckOut}",
            sql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Sql_ProjectsVisaStartDate_AndRegistrationKindForHeal()
    {
        var sql = LoadSql();

        Assert.Contains("VisaStartDate", sql, StringComparison.Ordinal);
        // Heal gate: ViewDefinitionContains(..., "RegistrationKind")
        Assert.Contains("RegistrationKind", sql, StringComparison.Ordinal);
        Assert.Contains("vw_rd_visa_aktiv_personeller", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Sql_CancellationCheckOutExclusion_DoesNotRequireIssuedState()
    {
        var sql = LoadSql();
        var openVisasStart = sql.IndexOf("open_visas AS", StringComparison.Ordinal);
        Assert.True(openVisasStart >= 0);
        var openRankedStart = sql.IndexOf("open_ranked AS", StringComparison.Ordinal);
        Assert.True(openRankedStart > openVisasStart);
        var openVisasCte = sql.Substring(openVisasStart, openRankedStart - openVisasStart);

        Assert.Contains("ActionFamily", openVisasCte, StringComparison.Ordinal);
        Assert.Contains("RegistrationKind", openVisasCte, StringComparison.Ordinal);
        // Link is enough; do not require LatestPrimaryStateCode / PROCESS_ISSUED on the case.
        Assert.DoesNotContain("PROCESS_ISSUED", openVisasCte, StringComparison.Ordinal);
        Assert.DoesNotContain("LatestPrimaryStateCode", openVisasCte, StringComparison.Ordinal);
    }
}
