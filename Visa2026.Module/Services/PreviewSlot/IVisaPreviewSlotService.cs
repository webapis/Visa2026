using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Services.PreviewSlot;

public enum VisaPreviewSlotMode
{
    Closed = 0,
    File = 1,
    Resminamalar = 2,
}

public sealed class ResminamalarSlotRequest
{
    public Guid ApplicationId { get; init; }

    public WordReportPackageScope Scope { get; init; } = WordReportPackageScope.Application;

    public IReadOnlyList<Guid> ApplicationItemIds { get; init; } = Array.Empty<Guid>();

    /// <summary>When set, catalog area shows this localized message instead of the report list.</summary>
    public string? EmptyCatalogMessage { get; init; }
}

public sealed class VisaPreviewSlotState
{
    public VisaPreviewSlotMode Mode { get; init; } = VisaPreviewSlotMode.Closed;

    /// <summary>Stable key for the current slot occupant (Resminamalar scope, file source, etc.).</summary>
    public string? OccupantKey { get; init; }

    /// <summary>XAF <see cref="View.Id"/> that opened the current occupant; used for owner-aware auto-close.</summary>
    public string? OwnerViewId { get; init; }

    public string? FileSourceType { get; init; }

    public Guid FileObjectId { get; init; }

    public ResminamalarSlotRequest? Resminamalar { get; init; }

    public int Version { get; init; }
}

/// <summary>
/// Global right-side preview slot orchestrator (file preview + inline Resminamalar).
/// Implemented in the Blazor host; callable from XAF Module controllers via DI.
/// </summary>
public interface IVisaPreviewSlotService
{
    VisaPreviewSlotState State { get; }

    event Action? StateChanged;

    Task OpenResminamalarAsync(ResminamalarSlotRequest request, string? ownerViewId = null);

    Task OpenFileAsync(string sourceType, Guid objectId, string? ownerViewId = null);

    Task CloseAsync();
}

public sealed class ReportPackagePreviewRequest
{
    public required Guid ApplicationId { get; init; }

    public required string EntryKey { get; init; }

    public required string DisplayName { get; init; }

    public IReadOnlyList<Guid>? ApplicationItemIds { get; init; }
}
