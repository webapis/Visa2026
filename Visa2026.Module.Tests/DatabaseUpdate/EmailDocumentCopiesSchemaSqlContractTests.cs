using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class EmailDocumentCopiesSchemaSqlContractTests
{
    [Fact]
    public void EnsureTablesPostgres_CreatesCoreTablesWithSoftDeleteColumns()
    {
        var sql = EmailDocumentCopiesSchemaSql.EnsureTablesPostgres;

        foreach (var table in new[]
                 {
                     "EmailContacts",
                     "EmailTemplates",
                     "SentEmails",
                     "SentEmailPersons",
                     "OfficerMailConnections"
                 })
        {
            Assert.Contains($"CREATE TABLE \"{table}\"", sql, StringComparison.Ordinal);
            Assert.Contains("\"GCRecord\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
            Assert.Contains("\"OptimisticLockField\" integer NOT NULL DEFAULT 0", sql, StringComparison.Ordinal);
        }

        Assert.Contains("FK_SentEmailPersons_SentEmails_SentEmailID", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"BatchId\"", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"PartNumber\"", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"PartCount\"", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"MaxEmailSizeMb\"", sql, StringComparison.Ordinal);
        Assert.Contains("PermissionPolicyUser", sql, StringComparison.Ordinal);
        Assert.Contains("ADD COLUMN IF NOT EXISTS \"Email\" character varying(320)", sql, StringComparison.Ordinal);
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
