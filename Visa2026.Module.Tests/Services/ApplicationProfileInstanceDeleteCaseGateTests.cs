using System;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceDeleteCaseGateTests
{
    [Fact]
    public void CountIssuedRecords_NullObjectSpaceOrEmptyId_ReturnsZero()
    {
        Assert.Equal(0, ApplicationProfileInstanceDeleteHelper.CountIssuedRecords(null!, Guid.Empty));
        Assert.Equal(0, ApplicationProfileInstanceDeleteHelper.CountIssuedRecords(null!, Guid.NewGuid()));
    }

    [Fact]
    public void TryDelete_NullObjectSpaceOrEmptyId_ReturnsNotFound()
    {
        Assert.False(ApplicationProfileInstanceDeleteHelper.TryDelete(null!, Guid.Empty, out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));

        Assert.False(ApplicationProfileInstanceDeleteHelper.TryDelete(null!, Guid.NewGuid(), out error));
        Assert.False(string.IsNullOrWhiteSpace(error));
    }
}
