using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.MigrationImport;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class InvitationIssuingOriginPolicyGateTests
{
    [Fact]
    public void RequiresApplicationProfileInstanceOnSave_FalseForNull()
    {
        Assert.False(InvitationIssuingOriginPolicy.RequiresApplicationProfileInstanceOnSave(null));
        Assert.True(InvitationIssuingOriginPolicy.HasRequiredApplicationProfileInstance(null));
    }

    [Fact]
    public void RequiresApplicationProfileInstanceOnSave_FalseDuringDataImport()
    {
        var invitation = new Invitation();

        using (MigrationImportContext.BeginDataImportScope())
        {
            Assert.True(MigrationImportContext.IsDataImport);
            Assert.False(InvitationIssuingOriginPolicy.RequiresApplicationProfileInstanceOnSave(invitation));
            Assert.True(InvitationIssuingOriginPolicy.HasRequiredApplicationProfileInstance(invitation));
        }

        Assert.False(MigrationImportContext.IsDataImport);
    }

    [Fact]
    public void RequiresApplicationProfileInstanceOnSave_FalseWithoutObjectSpace()
    {
        // Detached Invitation has no ObjectSpace — gate must not treat it as a new officer save.
        var invitation = new Invitation();

        Assert.False(InvitationIssuingOriginPolicy.RequiresApplicationProfileInstanceOnSave(invitation));
        Assert.True(InvitationIssuingOriginPolicy.HasRequiredApplicationProfileInstance(invitation));
    }
}
