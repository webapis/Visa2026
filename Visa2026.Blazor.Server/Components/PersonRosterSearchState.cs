namespace Visa2026.Blazor.Server.Components;

/// <summary>
/// Shared roster-search presentation. The grid keeps the empty-area component mounted
/// when a search also returns no rows, so this state notifies that component directly.
/// </summary>
public sealed class PersonRosterSearchState
{
    public bool Awaiting { get; private set; } = true;

    public string Query { get; private set; } = string.Empty;

    /// <summary>Null until the filtered total is known. Zero is a finished search with no rows.</summary>
    public int? ResultCount { get; private set; }

    public event Action? Changed;

    public void Publish(bool awaiting, string? query)
    {
        var next = query?.Trim() ?? string.Empty;
        if (awaiting || !string.Equals(Query, next, StringComparison.Ordinal))
            ResultCount = null;

        Awaiting = awaiting;
        Query = next;
        Changed?.Invoke();
    }

    public void SetResultCount(int? count)
    {
        if (ResultCount == count)
            return;

        ResultCount = count;
        Changed?.Invoke();
    }
}
