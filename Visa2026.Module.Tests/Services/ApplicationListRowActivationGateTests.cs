using System.Threading;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Regression for list-row vs progress-step file-link clicks: SuppressBriefly must
/// cancel the next workspace navigation once, then clear.
/// </summary>
public sealed class ApplicationListRowActivationGateTests
{
    [Fact]
    public void ConsumeIfPending_is_false_when_nothing_was_suppressed()
    {
        // Clear any leftover suppress from another test in this process.
        while (ApplicationListRowActivationGate.ConsumeIfPending())
        {
        }

        Assert.False(ApplicationListRowActivationGate.ConsumeIfPending());
    }

    [Fact]
    public void SuppressBriefly_makes_the_next_consume_true_then_false()
    {
        while (ApplicationListRowActivationGate.ConsumeIfPending())
        {
        }

        ApplicationListRowActivationGate.SuppressBriefly();

        Assert.True(ApplicationListRowActivationGate.ConsumeIfPending());
        Assert.False(ApplicationListRowActivationGate.ConsumeIfPending());
    }

    [Fact]
    public void ConsumeIfPending_is_false_after_the_brief_window_expires()
    {
        while (ApplicationListRowActivationGate.ConsumeIfPending())
        {
        }

        ApplicationListRowActivationGate.SuppressBriefly();
        // Gate window is 700ms; wait past it with buffer so this stays deterministic.
        Thread.Sleep(900);

        Assert.False(ApplicationListRowActivationGate.ConsumeIfPending());
    }
}
