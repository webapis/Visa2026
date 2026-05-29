using System;

namespace Visa2026.Module.Services;

/// <summary>Relationship option for the visa family members text editor combo box.</summary>
public sealed class RelationshipLookupItem
{
    public Guid Oid { get; init; }

    public string NameTm { get; init; } = string.Empty;
}
