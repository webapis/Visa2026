using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.MigrationImport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Gates for officer-created visas: import and detached objects must not require
/// IssuingApplicationProfileInstance; only new ObjectSpace-backed saves do.
/// </summary>
public class VisaIssuingOriginPolicyGateTests
{
    [Fact]
    public void RequiresIssuingApplicationProfileInstanceOnSave_FalseForNull()
    {
        Assert.False(VisaIssuingOriginPolicy.RequiresIssuingApplicationProfileInstanceOnSave(null));
        Assert.True(VisaIssuingOriginPolicy.HasRequiredIssuingApplicationProfileInstance(null));
    }

    [Fact]
    public void RequiresIssuingApplicationProfileInstanceOnSave_FalseDuringDataImport()
    {
        var visa = new Visa();

        using (MigrationImportContext.BeginDataImportScope())
        {
            Assert.True(MigrationImportContext.IsDataImport);
            Assert.False(VisaIssuingOriginPolicy.RequiresIssuingApplicationProfileInstanceOnSave(visa));
            Assert.True(VisaIssuingOriginPolicy.HasRequiredIssuingApplicationProfileInstance(visa));
        }

        Assert.False(MigrationImportContext.IsDataImport);
    }

    [Fact]
    public void RequiresIssuingApplicationProfileInstanceOnSave_FalseWithoutObjectSpace()
    {
        // Detached Visa has no ObjectSpace — gate must not treat it as a new officer save.
        var visa = new Visa();

        Assert.False(VisaIssuingOriginPolicy.RequiresIssuingApplicationProfileInstanceOnSave(visa));
        Assert.True(VisaIssuingOriginPolicy.HasRequiredIssuingApplicationProfileInstance(visa));
    }
}
