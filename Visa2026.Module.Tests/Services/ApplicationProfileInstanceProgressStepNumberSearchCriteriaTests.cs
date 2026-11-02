using System;
using System.Linq;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Guards the progress-step number search that matches ResultNumber,
/// SubmissionDocumentNumber, and ProcessNumber with three operands per token.
/// </summary>
public sealed class ApplicationProfileInstanceProgressStepNumberSearchCriteriaTests
{
    [Fact]
    public void BuildProgressResultNumberCriteria_returns_null_for_blank_input()
    {
        Assert.Null(ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildProgressResultNumberCriteria("   "));
        Assert.Null(ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildProgressResultNumberCriteria(string.Empty));
    }

    [Fact]
    public void BuildProgressResultNumberCriteria_repeats_each_token_three_times_for_the_or_fields()
    {
        // Avoid spaces around '/' so PersonSearchTokens keeps "4/4114" as one token
        // (typed "4 / 4114" splits into "4", "/", "4114").
        var criteria = ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder
            .BuildProgressResultNumberCriteria("4/4114 CO0236909");

        Assert.NotNull(criteria);
        var text = criteria!.ToString();

        Assert.Contains("ResultNumber", text, StringComparison.Ordinal);
        Assert.Contains("SubmissionDocumentNumber", text, StringComparison.Ordinal);
        Assert.Contains("ProcessNumber", text, StringComparison.Ordinal);
        Assert.Contains("4/4114", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("co0236909", text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(" And ", text, StringComparison.Ordinal);

        // Two tokens × three OR fields = six field mentions.
        var fieldMentions =
            CountOccurrences(text, "ResultNumber")
            + CountOccurrences(text, "SubmissionDocumentNumber")
            + CountOccurrences(text, "ProcessNumber");
        Assert.Equal(6, fieldMentions);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        for (var index = 0; ; index += needle.Length)
        {
            index = haystack.IndexOf(needle, index, StringComparison.Ordinal);
            if (index < 0)
                return count;
            count++;
        }
    }

    [Fact]
    public void CompactResultNumberSearchKey_keeps_letters_and_digits_without_spaces()
    {
        Assert.Equal(
            "CO0236909",
            ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder.CompactResultNumberSearchKey(" CO 0236909 "));
        Assert.Equal(
            string.Empty,
            ApplicationProfileInstanceListViewFullTextSearchCriteriaBuilder.CompactResultNumberSearchKey(" \t "));
    }
}
