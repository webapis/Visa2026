using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Host-start heal SQL for AuthorizedSignatory.PassportExpirationDate —
/// letterhead signatory expiry field after deploy when ModuleUpdater is skipped.
/// </summary>
public class AuthorizedSignatoryPassportExpirationDateSchemaSqlTests
{
    [Fact]
    public void EnsureColumns_BothProviders_AddNullablePassportExpirationDate()
    {
        var pg = AuthorizedSignatoryPassportExpirationDateSchemaSql.EnsureColumnsPostgres;
        var ss = AuthorizedSignatoryPassportExpirationDateSchemaSql.EnsureColumnsSqlServer;

        Assert.Contains("AuthorizedSignatories", pg);
        Assert.Contains("AuthorizedSignatories", ss);
        Assert.Contains("PassportExpirationDate", pg);
        Assert.Contains("PassportExpirationDate", ss);
        Assert.Contains("timestamp without time zone NULL", pg);
        Assert.Contains("datetime2 NULL", ss);
        Assert.Contains("to_regclass('public.\"AuthorizedSignatories\"')", pg);
        Assert.Contains("COL_LENGTH(N'dbo.AuthorizedSignatories', N'PassportExpirationDate')", ss);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        AuthorizedSignatoryPassportExpirationDateSchemaSql.ApplyIfMissing(null!);
        AuthorizedSignatoryPassportExpirationDateSchemaSql.ApplyIfMissing("");
        AuthorizedSignatoryPassportExpirationDateSchemaSql.ApplyIfMissing("   ");
    }
}
