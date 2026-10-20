#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

/// <summary>
/// File catch-up must skip non-Postgres targets and reject unsafe SQL identifiers
/// before opening a connection (skip-second-issued / existing blob index).
/// </summary>
public class Visa2014ExistingTargetBlobIndexGateTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Server=localhost;Database=Visa2026;Trusted_Connection=True")]
    [InlineData("Data Source=localhost\\SQLEXPRESS;Initial Catalog=Visa2026;Integrated Security=True")]
    public async Task LoadParentIdsWithDocumentAsync_returns_empty_when_connection_is_blank_or_not_postgres(
        string? connection)
    {
        var ids = await Visa2014ExistingTargetBlobIndex.LoadParentIdsWithDocumentAsync(
            connection,
            "VisaDocuments",
            "VisaID");

        Assert.Empty(ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Server=localhost;Database=Visa2026;Trusted_Connection=True")]
    public async Task SeedAsync_returns_zero_when_connection_is_blank_or_not_postgres(
        string? connection)
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var seeded = await Visa2014ExistingTargetBlobIndex.SeedAsync(
            connection,
            "VisaDocuments",
            "VisaID",
            keys);

        Assert.Equal(0, seeded);
        Assert.Empty(keys);
    }

    [Theory]
    [InlineData("Visa-Documents", "VisaID")]
    [InlineData("VisaDocuments", "Visa ID")]
    [InlineData("Visa Documents", "VisaID")]
    [InlineData("VisaDocuments;", "VisaID")]
    public async Task LoadParentIdsWithDocumentAsync_rejects_unsafe_sql_identifiers(
        string documentTable,
        string parentIdColumn)
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            Visa2014ExistingTargetBlobIndex.LoadParentIdsWithDocumentAsync(
                "Host=localhost;Database=visa2026;Username=u;Password=p;EFCoreProvider=Postgres",
                documentTable,
                parentIdColumn));

        Assert.Contains("Unsafe SQL identifier", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SeedAsync_rejects_unsafe_document_table_identifier()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            Visa2014ExistingTargetBlobIndex.SeedAsync(
                "Host=localhost;Database=visa2026;Username=u;Password=p",
                "VisaDocuments'",
                "VisaID",
                keys));

        Assert.Contains("Unsafe SQL identifier", ex.Message, StringComparison.Ordinal);
        Assert.Empty(keys);
    }
}
