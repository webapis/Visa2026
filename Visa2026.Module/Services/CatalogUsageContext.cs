using System;

namespace Visa2026.Module.Services;

/// <summary>
/// Optional context when counting or removing catalog label usage from the multi-select popup
/// (uses draft selection on the record being edited).
/// </summary>
public sealed class CatalogUsageContext
{
    public Guid? EditingObjectId { get; init; }

    public string? EditingEffectiveStored { get; init; }
}
