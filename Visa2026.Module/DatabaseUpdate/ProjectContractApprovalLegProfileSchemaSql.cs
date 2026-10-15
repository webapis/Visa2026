using Npgsql;

namespace Visa2026.Module.DatabaseUpdate;

/// <summary>
/// Drops the retired link from project contracts to approval-leg profiles.
/// Approval legs are chosen on the application profile instance.
/// </summary>
public static class ProjectContractApprovalLegProfileSchemaSql
{
    internal const string DropPostgres = """
        ALTER TABLE IF EXISTS "ProjectContracts" DROP COLUMN IF EXISTS "ApprovalLegProfileId" CASCADE;
        ALTER TABLE IF EXISTS "ProjectContracts" DROP COLUMN IF EXISTS "ApprovalLegProfileID" CASCADE;
        DROP TABLE IF EXISTS "ProjectContractApprovalLegProfiles" CASCADE;
        """;

    public static void ApplyIfMissing(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            return;

        var cleaned = DatabaseProviderDetector.StripEfCoreProvider(connectionString);
        if (!DatabaseProviderDetector.IsPostgreSql(connectionString))
            return;

        using var connection = new NpgsqlConnection(cleaned);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = DropPostgres;
        command.ExecuteNonQuery();
    }
}
