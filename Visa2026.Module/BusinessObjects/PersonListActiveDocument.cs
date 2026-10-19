namespace Visa2026.Module.BusinessObjects;

/// <summary>One in-force visa, work permit, or invitation line on a Person list.</summary>
public sealed class PersonListActiveDocument
{
    public string Number { get; init; } = string.Empty;

    /// <summary>Visa type. Empty for work permits and invitations.</summary>
    public string Detail { get; init; } = string.Empty;

    public string Dates { get; init; } = string.Empty;

    /// <summary>Person document-copies record key, for example <c>Passport:{id}/Visa:{id}</c>.</summary>
    public string RecordKey { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;
}
