using System;

namespace Visa2026.Module.Services;

/// <summary>Country option for the visa family members text editor combo box.</summary>
public sealed class CountryLookupItem
{
    public Guid Oid { get; init; }

    public string Code { get; init; } = string.Empty;

    public string NameTm { get; init; } = string.Empty;

    public string DisplayText =>
        string.IsNullOrWhiteSpace(Code)
            ? NameTm
            : string.IsNullOrWhiteSpace(NameTm)
                ? Code
                : $"{Code} — {NameTm}";
}
