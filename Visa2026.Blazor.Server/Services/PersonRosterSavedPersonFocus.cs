namespace Visa2026.Blazor.Server.Services;

/// <summary>One-shot request so a newly opened roster list selects the person just saved.</summary>
public static class PersonRosterSavedPersonFocus
{
    public static PendingFocus? Pending { get; set; }

    public readonly record struct PendingFocus(string ListViewId, Guid PersonId, bool Incomplete);
}
