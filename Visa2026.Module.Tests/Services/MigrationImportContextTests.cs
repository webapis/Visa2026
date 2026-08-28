using Visa2026.Module.Services.MigrationImport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Import/audit AsyncLocal scope — wrong nest/dispose leaks Path-A matchers and AppNumberFormat suppression.
/// </summary>
public sealed class MigrationImportContextTests
{
    [Fact]
    public void BeginDataImportScope_SetsFlagsAndSessionStart()
    {
        Assert.False(MigrationImportContext.IsDataImport);
        Assert.False(MigrationImportContext.IsAuditTrailSuppressed);
        Assert.Null(MigrationImportContext.ImportSessionStartedUtc);

        using (MigrationImportContext.BeginDataImportScope())
        {
            Assert.True(MigrationImportContext.IsDataImport);
            Assert.True(MigrationImportContext.IsAuditTrailSuppressed);
            Assert.NotNull(MigrationImportContext.ImportSessionStartedUtc);
            Assert.True(MigrationImportContext.ImportSessionStartedUtc <= DateTime.UtcNow.AddSeconds(1));
        }

        Assert.False(MigrationImportContext.IsDataImport);
        Assert.False(MigrationImportContext.IsAuditTrailSuppressed);
        Assert.Null(MigrationImportContext.ImportSessionStartedUtc);
    }

    [Fact]
    public void NestedScopes_KeepOuterFlagsUntilOuterDispose()
    {
        using (MigrationImportContext.BeginDataImportScope())
        {
            var outerStart = MigrationImportContext.ImportSessionStartedUtc;
            Assert.NotNull(outerStart);

            using (MigrationImportContext.BeginDataImportScope())
            {
                Assert.True(MigrationImportContext.IsDataImport);
                Assert.Equal(outerStart, MigrationImportContext.ImportSessionStartedUtc);
            }

            Assert.True(MigrationImportContext.IsDataImport);
            Assert.True(MigrationImportContext.IsAuditTrailSuppressed);
            Assert.Equal(outerStart, MigrationImportContext.ImportSessionStartedUtc);
        }

        Assert.False(MigrationImportContext.IsDataImport);
        Assert.Null(MigrationImportContext.ImportSessionStartedUtc);
    }

    [Fact]
    public void Dispose_RestoresPreviousNestedState()
    {
        Assert.False(MigrationImportContext.IsDataImport);

        using (MigrationImportContext.BeginDataImportScope())
        {
            Assert.True(MigrationImportContext.IsDataImport);
            using (MigrationImportContext.BeginDataImportScope())
            {
                Assert.True(MigrationImportContext.IsDataImport);
            }

            Assert.True(MigrationImportContext.IsDataImport);
        }

        Assert.False(MigrationImportContext.IsDataImport);
        Assert.False(MigrationImportContext.IsAuditTrailSuppressed);
    }
}
