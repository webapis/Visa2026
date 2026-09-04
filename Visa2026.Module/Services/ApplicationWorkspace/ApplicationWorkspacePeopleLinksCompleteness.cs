using System;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// People and links nav + tile completeness from profile-configured person tiles.
/// A tile is short when Count is below ExpectedCount.
/// </summary>
public static class ApplicationWorkspacePeopleLinksCompleteness
{
    public const string PassportKey = "passport";
    public const string VisaKey = "visa";

    public enum NavStatus
    {
        EmptyRoster = 0,
        Incomplete = 1,
        Complete = 2,
    }

    public static bool IsCountShort(int count, int expectedCount) =>
        count < Math.Max(expectedCount, 1);

    public static bool IsRecordShort(ApplicationWorkspaceCasePersonRecord? record) =>
        record != null && IsCountShort(record.Count, record.ExpectedCount);

    public static bool PersonHasGap(ApplicationWorkspaceCasePerson? person) =>
        person?.Records != null && person.Records.Any(IsRecordShort);

    public static bool IsKindShort(ApplicationWorkspaceCasePerson? person, string recordKey)
    {
        if (person?.Records == null || string.IsNullOrWhiteSpace(recordKey))
            return false;

        var record = person.Records.FirstOrDefault(r =>
            string.Equals(r.Key, recordKey, StringComparison.OrdinalIgnoreCase));
        return IsRecordShort(record);
    }

    public static int PeopleWithGaps(ApplicationWorkspaceCaseView? view) =>
        view?.People?.Count(PersonHasGap) ?? 0;

    public static NavStatus Resolve(ApplicationWorkspaceCaseView? view)
    {
        var people = view?.People;
        if (people == null || people.Count == 0)
            return NavStatus.EmptyRoster;

        return people.Any(PersonHasGap) ? NavStatus.Incomplete : NavStatus.Complete;
    }
}