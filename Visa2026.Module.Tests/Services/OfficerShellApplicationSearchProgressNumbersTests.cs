using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Queue search haystack must index SubmissionDocumentNumber and ProcessNumber
/// (CO / AS style) so officers can find cases by those progress fields, not only ResultNumber.
/// </summary>
public class OfficerShellApplicationSearchProgressNumbersTests
{
    [Fact]
    public void BuildHaystack_IncludesSubmissionAndProcessNumbers()
    {
        var application = new ApplicationProfileInstance
        {
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>
            {
                new()
                {
                    SubmissionDocumentNumber = " SUB-9 ",
                    ProcessNumber = " CO-44 ",
                    ResultNumber = null,
                },
                new()
                {
                    SubmissionDocumentNumber = "   ",
                    ProcessNumber = null,
                },
            },
        };

        var haystack = OfficerShellApplicationSearch.BuildHaystack(application, "CASE-1");

        Assert.Contains("CASE-1", haystack, System.StringComparison.Ordinal);
        Assert.Contains("SUB-9", haystack, System.StringComparison.Ordinal);
        Assert.Contains("CO-44", haystack, System.StringComparison.Ordinal);
    }

    [Fact]
    public void Matches_FindsCaseBySubmissionOrProcessToken()
    {
        var application = new ApplicationProfileInstance
        {
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>
            {
                new()
                {
                    SubmissionDocumentNumber = "12/AS",
                    ProcessNumber = "88/CO",
                },
            },
        };
        var haystack = OfficerShellApplicationSearch.BuildHaystack(application);

        Assert.True(OfficerShellApplicationSearch.Matches("12/AS", haystack));
        Assert.True(OfficerShellApplicationSearch.Matches("88/CO", haystack));
        Assert.True(OfficerShellApplicationSearch.Matches("88", haystack));
        Assert.False(OfficerShellApplicationSearch.Matches("99/CO", haystack));
    }
}
