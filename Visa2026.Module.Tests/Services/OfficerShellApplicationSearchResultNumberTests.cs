using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Staged/in-process queue search must include progress Result Number (Netije belgisi)
/// so officers can find cases the same way FullTextSearch does on the ListView.
/// </summary>
public class OfficerShellApplicationSearchResultNumberTests
{
    [Fact]
    public void BuildHaystack_IncludesProgressResultNumbers()
    {
        var application = new ApplicationProfileInstance();
        application.ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>
        {
            new() { ResultNumber = " 12/2026 " },
            new() { ResultNumber = "4/4114" },
            new() { ResultNumber = "   " },
            new() { ResultNumber = null },
        };

        var haystack = OfficerShellApplicationSearch.BuildHaystack(application, "AS-1");

        Assert.Contains("AS-1", haystack, System.StringComparison.Ordinal);
        Assert.Contains("12/2026", haystack, System.StringComparison.Ordinal);
        Assert.Contains("4/4114", haystack, System.StringComparison.Ordinal);
        Assert.DoesNotContain("\n\n\n", haystack, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Matches_FindsCaseByResultNumberToken()
    {
        var application = new ApplicationProfileInstance();
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress { ResultNumber = "4/4114" });
        var haystack = OfficerShellApplicationSearch.BuildHaystack(application);

        Assert.True(OfficerShellApplicationSearch.Matches("4/4114", haystack));
        Assert.True(OfficerShellApplicationSearch.Matches("4114", haystack));
        Assert.False(OfficerShellApplicationSearch.Matches("5/4114", haystack));
    }

    [Fact]
    public void BuildHaystack_NullApplication_UsesExtrasOnly()
    {
        var haystack = OfficerShellApplicationSearch.BuildHaystack(null, "extra", "  ", null);
        Assert.Equal("extra", haystack);
    }
}
