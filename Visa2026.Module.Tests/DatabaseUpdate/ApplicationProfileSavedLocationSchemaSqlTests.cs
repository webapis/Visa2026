using System;
using Visa2026.Module.DatabaseUpdate;
using Xunit;

namespace Visa2026.Module.Tests.DatabaseUpdate;

public class ApplicationProfileSavedLocationSchemaSqlTests
{
    [Fact]
    public void SavedLocationsTable_GcRecordNotNullDefault0()
    {
        Assert.Contains(
            "\"GCRecord\" integer NOT NULL DEFAULT 0",
            ApplicationProfileSavedLocationSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "\"OptimisticLockField\" integer NOT NULL DEFAULT 0",
            ApplicationProfileSavedLocationSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
    }

    [Fact]
    public void UniqueIndex_IsFieldAndTextKey_WithGcRecordFilter()
    {
        Assert.Contains(
            "IX_ApplicationProfileSavedLocations_Field_Text",
            ApplicationProfileSavedLocationSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "(\"Field\", \"TextKey\")",
            ApplicationProfileSavedLocationSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
        Assert.Contains(
            "WHERE \"GCRecord\" = 0",
            ApplicationProfileSavedLocationSchemaSql.EnsureSchemaPostgres,
            StringComparison.Ordinal);
    }
}
