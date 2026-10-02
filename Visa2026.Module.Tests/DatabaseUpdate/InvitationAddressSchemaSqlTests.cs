#nullable enable

using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

/// <summary>
/// Host-start heal SQL for invitation stay tables (ff2819af). Drift here causes 42P01 when
/// ModuleInfo is already current and officers open Çakylyk Almak create.
/// </summary>
public class InvitationAddressSchemaSqlTests
{
    [Fact]
    public void Postgres_creates_alternative_catalog_and_invitation_address()
    {
        var sql = InvitationAddressSchemaSql.EnsureTablesPostgres;
        Assert.Contains("CREATE TABLE IF NOT EXISTS \"AlternativeAddressesForInvitation\"", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE \"InvitationAddress\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"AddressLine\" character varying(2000)", sql, StringComparison.Ordinal);
        Assert.Contains("\"ApplicationProfileInstanceId\"", sql, StringComparison.Ordinal);
        Assert.Contains("\"AlternativeAddressId\"", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Postgres_GcRecord_is_not_null_with_default_zero()
    {
        var sql = InvitationAddressSchemaSql.EnsureTablesPostgres;
        Assert.Contains(
            "\"GCRecord\" integer NOT NULL DEFAULT 0",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "ALTER TABLE \"InvitationAddress\" ALTER COLUMN \"GCRecord\" SET NOT NULL",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "ALTER TABLE \"AlternativeAddressesForInvitation\" ALTER COLUMN \"GCRecord\" SET NOT NULL",
            sql,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "\"GCRecord\" integer NULL",
            sql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Postgres_cascades_instance_delete_and_nulls_alternative_on_catalog_delete()
    {
        var sql = InvitationAddressSchemaSql.EnsureTablesPostgres;
        Assert.Contains(
            "REFERENCES \"ApplicationProfileInstances\" (\"ID\") ON DELETE CASCADE",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "REFERENCES \"AlternativeAddressesForInvitation\" (\"ID\") ON DELETE SET NULL",
            sql,
            StringComparison.Ordinal);
        Assert.Contains(
            "CREATE UNIQUE INDEX \"IX_InvitationAddress_ApplicationProfileInstanceId\"",
            sql,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SqlServer_mirrors_postgres_table_and_fk_shape()
    {
        var sql = InvitationAddressSchemaSql.EnsureTablesSqlServer;
        Assert.Contains("CREATE TABLE dbo.AlternativeAddressesForInvitation", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE dbo.InvitationAddress", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE CASCADE", sql, StringComparison.Ordinal);
        Assert.Contains("ON DELETE SET NULL", sql, StringComparison.Ordinal);
        Assert.Contains("AddressLine nvarchar(2000)", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void ApplyIfMissing_noops_on_blank_connection_string()
    {
        InvitationAddressSchemaSql.ApplyIfMissing(null!);
        InvitationAddressSchemaSql.ApplyIfMissing("");
        InvitationAddressSchemaSql.ApplyIfMissing("   ");
    }
}
