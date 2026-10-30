using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfilePickerShelfSchemaSqlTests
{
    [Fact]
    public void ShelvesTable_GcRecordMatchesSiblingApplicationProfileTables()
    {
        Assert.Contains(
            "\"GCRecord\" integer NOT NULL DEFAULT 0",
            ApplicationProfilePickerShelfSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"OptimisticLockField\" integer NOT NULL DEFAULT 0",
            ApplicationProfilePickerShelfSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "\"GCRecord\" integer NULL",
            ApplicationProfilePickerShelfSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void HostStart_HealsNullableGcRecordOnExistingShelvesTable()
    {
        Assert.Contains(
            "ALTER COLUMN \"GCRecord\" SET NOT NULL",
            ApplicationProfilePickerShelfSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "SET \"GCRecord\" = 0 WHERE \"GCRecord\" IS NULL",
            ApplicationProfilePickerShelfSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "ALTER COLUMN \"OptimisticLockField\" SET NOT NULL",
            ApplicationProfilePickerShelfSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
    }
}