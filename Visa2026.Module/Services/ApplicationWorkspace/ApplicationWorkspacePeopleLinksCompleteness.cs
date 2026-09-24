using System;
using System.Linq;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// People and links nav + tile completeness from profile-configured person tiles.
/// A tile is short when Count is below ExpectedCount. ExpectedCount 0 means no gap
/// (nothing to link). Visa / Work permit / Invitation expected counts are
/// min(Last-N, active rows), not a fixed Last-N quota.
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

    public static bool LinksLocked(ApplicationWorkspaceCaseView? view) =>
        view?.Chrome.ResolvedLinksLocked == true;

    public static bool IsCountShort(int count, int expectedCount, bool linksLocked = false)
    {
        _ = linksLocked;
        if (expectedCount <= 0)
            return false;
        return count < expectedCount;
    }

    public static bool IsRecordShort(ApplicationWorkspaceCasePersonRecord? record, bool linksLocked = false) =>
        record != null && IsCountShort(record.Count, record.ExpectedCount, linksLocked);

    /// <summary>Excluded (Seretmezlik) people never count as a gap.</summary>
    public static bool PersonHasGap(ApplicationWorkspaceCasePerson? person, bool linksLocked = false) =>
        person?.Records != null && !person.IsExcluded && person.Records.Any(r => IsRecordShort(r, linksLocked));

    public static bool IsKindShort(ApplicationWorkspaceCasePerson? person, string recordKey, bool linksLocked = false)
    {
        if (person?.Records == null || string.IsNullOrWhiteSpace(recordKey))
            return false;

        var record = person.Records.FirstOrDefault(r =>
            string.Equals(r.Key, recordKey, StringComparison.OrdinalIgnoreCase));
        return IsRecordShort(record, linksLocked);
    }

    public static int PeopleWithGaps(ApplicationWorkspaceCaseView? view)
    {
        var locked = LinksLocked(view);
        return view?.People?.Count(p => PersonHasGap(p, locked)) ?? 0;
    }

    public static NavStatus Resolve(ApplicationWorkspaceCaseView? view)
    {
        var people = view?.People;
        if (people == null || people.Count == 0)
            return NavStatus.EmptyRoster;

        var locked = LinksLocked(view);
        return people.Any(p => PersonHasGap(p, locked)) ? NavStatus.Incomplete : NavStatus.Complete;
    }
}