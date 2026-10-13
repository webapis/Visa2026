using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.MigrationImport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Gates for officer-created work permits: import and detached objects must not require
/// ApplicationProfileInstance; only new ObjectSpace-backed saves do.
/// </summary>
public class WorkPermitIssuingOriginPolicyGateTests
{
    [Fact]
    public void RequiresApplicationProfileInstanceOnSave_FalseForNull()
    {
        Assert.False(WorkPermitIssuingOriginPolicy.RequiresApplicationProfileInstanceOnSave(null));
        Assert.True(WorkPermitIssuingOriginPolicy.HasRequiredApplicationProfileInstance(null));
    }

    [Fact]
    public void RequiresApplicationProfileInstanceOnSave_FalseDuringDataImport()
    {
        var workPermit = new WorkPermit();

        using (MigrationImportContext.BeginDataImportScope())
        {
            Assert.True(MigrationImportContext.IsDataImport);
            Assert.False(WorkPermitIssuingOriginPolicy.RequiresApplicationProfileInstanceOnSave(workPermit));
            Assert.True(WorkPermitIssuingOriginPolicy.HasRequiredApplicationProfileInstance(workPermit));
        }

        Assert.False(MigrationImportContext.IsDataImport);
    }

    [Fact]
    public void RequiresApplicationProfileInstanceOnSave_FalseWithoutObjectSpace()
    {
        // Detached WorkPermit has no ObjectSpace — gate must not treat it as a new officer save.
        var workPermit = new WorkPermit();

        Assert.False(WorkPermitIssuingOriginPolicy.RequiresApplicationProfileInstanceOnSave(workPermit));
        Assert.True(WorkPermitIssuingOriginPolicy.HasRequiredApplicationProfileInstance(workPermit));
    }
}
