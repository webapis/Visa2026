using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Contract guards for dropping the retired ProjectContract ↔ ApprovalLegProfile link
/// (approval legs are chosen on the application profile instance).
/// </summary>
public class ProjectContractApprovalLegProfileDropSchemaSqlTests
{
    [Fact]
    public void DropPostgres_DropsFkColumnsAndJoinTable()
    {
        var sql = ProjectContractApprovalLegProfileSchemaSql.DropPostgres;

        Assert.Contains(
            "ALTER TABLE IF EXISTS \"ProjectContracts\" DROP COLUMN IF EXISTS \"ApprovalLegProfileId\" CASCADE",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "ALTER TABLE IF EXISTS \"ProjectContracts\" DROP COLUMN IF EXISTS \"ApprovalLegProfileID\" CASCADE",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "DROP TABLE IF EXISTS \"ProjectContractApprovalLegProfiles\" CASCADE",
            sql,
            StringComparison.Ordinal);
        Assert.DoesNotContain("ADD COLUMN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FOREIGN KEY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ApplyIfMissing_BlankOrNull_IsNoOp()
    {
        ProjectContractApprovalLegProfileSchemaSql.ApplyIfMissing(null!);
        ProjectContractApprovalLegProfileSchemaSql.ApplyIfMissing("");
        ProjectContractApprovalLegProfileSchemaSql.ApplyIfMissing("   ");
    }

    [Fact]
    public void ApplyIfMissing_NonPostgres_IsNoOp()
    {
        // SQL Server-style strings must not open an Npgsql connection.
        ProjectContractApprovalLegProfileSchemaSql.ApplyIfMissing(
            "Server=localhost;Database=Visa2026;Trusted_Connection=True;EFCoreProvider=SqlServer");
    }
}
