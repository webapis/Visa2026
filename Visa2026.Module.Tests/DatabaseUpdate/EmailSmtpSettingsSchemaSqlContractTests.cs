using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Mail-server singleton heal added after the shared-mailbox password work.
/// Keeps <see cref="EmailDocumentCopiesSchemaSql"/> EmailSmtpSettings shape from drifting.
/// </summary>
public class EmailSmtpSettingsSchemaSqlContractTests
{
    [Fact]
    public void EnsureTablesPostgres_CreatesEmailSmtpSettingsWithProtectedPassword()
    {
        var sql = EmailDocumentCopiesSchemaSql.EnsureTablesPostgres;

        Assert.Contains("CREATE TABLE \"EmailSmtpSettings\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"ProtectedPassword\" text NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"Host\" character varying(256) NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"UserName\" character varying(320) NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"FromAddress\" character varying(320) NULL", sql, StringComparison.Ordinal);
        Assert.Contains("\"Port\" integer NOT NULL DEFAULT 587", sql, StringComparison.Ordinal);
        Assert.Contains("\"Security\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("\"BccOfficer\" boolean NOT NULL DEFAULT TRUE", sql, StringComparison.Ordinal);
        Assert.Contains("\"MaxTotalAttachmentBytes\" bigint NOT NULL DEFAULT 20971520", sql, StringComparison.Ordinal);
        Assert.Contains("\"TimeoutSeconds\" integer NOT NULL DEFAULT 120", sql, StringComparison.Ordinal);
        Assert.Contains("\"GCRecord\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        Assert.Contains("\"OptimisticLockField\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Server=localhost;Database=Visa2026;Trusted_Connection=True;")]
    public void ApplyIfMissing_NoOpsOnBlankOrNonPostgres(string? connectionString)
    {
        EmailDocumentCopiesSchemaSql.ApplyIfMissing(connectionString!);
    }
}
