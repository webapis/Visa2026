using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Host-start heal SQL for CompanyProfile.RegistrationDate — drift breaks
/// organization letterhead after deploy when ModuleUpdater is skipped.
/// </summary>
public class CompanyProfileRegistrationDateSchemaSqlTests
{
    [Fact]
    public void EnsureColumns_BothProviders_AddNullableRegistrationDate()
    {
        var pg = CompanyProfileRegistrationDateSchemaSql.EnsureColumnsPostgres;
        var ss = CompanyProfileRegistrationDateSchemaSql.EnsureColumnsSqlServer;

        Assert.Contains("CompanyProfiles", pg);
        Assert.Contains("CompanyProfiles", ss);
        Assert.Contains("RegistrationDate", pg);
        Assert.Contains("RegistrationDate", ss);
        Assert.Contains("timestamp without time zone NULL", pg);
        Assert.Contains("datetime2 NULL", ss);
        Assert.Contains("to_regclass('public.\"CompanyProfiles\"')", pg);
        Assert.Contains("COL_LENGTH(N'dbo.CompanyProfiles', N'RegistrationDate')", ss);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        CompanyProfileRegistrationDateSchemaSql.ApplyIfMissing(null!);
        CompanyProfileRegistrationDateSchemaSql.ApplyIfMissing("");
        CompanyProfileRegistrationDateSchemaSql.ApplyIfMissing("   ");
    }
}
