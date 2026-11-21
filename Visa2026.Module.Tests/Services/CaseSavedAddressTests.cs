using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class CaseSavedAddressTests
{
    [Fact]
    public void Cap_IsPositiveTenantWideLimit()
    {
        Assert.True(CaseSavedAddress.Cap >= 1);
    }
}