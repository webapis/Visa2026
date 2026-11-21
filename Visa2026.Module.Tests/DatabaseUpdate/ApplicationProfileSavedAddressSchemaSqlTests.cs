using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileSavedAddressSchemaSqlTests
{
    [Fact]
    public void SavedAddressesTable_GcRecordMatchesSiblingApplicationProfileTables()
    {
        Assert.Contains(
            "\"GCRecord\" integer NOT NULL DEFAULT 0",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"OptimisticLockField\" integer NOT NULL DEFAULT 0",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "\"GCRecord\" integer NULL",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HostStart_HealsNullableGcRecordOnExistingSavedAddressesTable()
    {
        Assert.Contains(
            "ALTER COLUMN \"GCRecord\" SET NOT NULL",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "SET \"GCRecord\" = 0 WHERE \"GCRecord\" IS NULL",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "ALTER COLUMN \"OptimisticLockField\" SET NOT NULL",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "WHERE \"GCRecord\" = 0",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void UniqueIndex_IsFieldAndTextKeyOnly()
    {
        Assert.Contains(
            "IX_ApplicationProfileSavedAddresses_Field_Text",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "(\"Field\", \"TextKey\")",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "(\"ApplicationProfileId\", \"ProjectContractId\", \"Field\", \"TextKey\")",
            ApplicationProfileSavedAddressSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
    }
}