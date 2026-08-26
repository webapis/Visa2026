using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

public sealed class VisaPreviewSlotService : IVisaPreviewSlotService
{
    private VisaPreviewSlotState _state = new();

    public VisaPreviewSlotState State => _state;

    public event Action? StateChanged;

    public Task OpenResminamalarAsync(ResminamalarSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.Resminamalar,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForResminamalar(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            Resminamalar = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenDocumentCopiesAsync(DocumentCopiesSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.DocumentCopies,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForDocumentCopies(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            DocumentCopies = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenProgressLettersAsync(ProgressLettersSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.ProgressLetters,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForProgressLetters(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            ProgressLetters = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenPersonDocumentCopiesAsync(PersonDocumentCopiesSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.PersonDocumentCopies,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForPersonDocumentCopies(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            PersonDocumentCopies = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenHeaderDocumentCopiesAsync(HeaderDocumentCopiesSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.HeaderDocumentCopies,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            HeaderDocumentCopies = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenPlaceholderManualAsync(PlaceholderManualSlotRequest? request = null, string? ownerViewId = null)
    {
        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.PlaceholderManual,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForPlaceholderManual(request?.FilterRootBoType),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            PlaceholderManual = request ?? new PlaceholderManualSlotRequest(),
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenIssueIssuedHeaderAsync(IssueIssuedHeaderSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.IssueIssuedHeader,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForIssueIssuedHeader(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            IssueIssuedHeader = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenIssueIssuedVisaAsync(IssueIssuedVisaSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.IssueIssuedVisa,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForIssueIssuedVisa(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            IssueIssuedVisa = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenFileAsync(string sourceType, Guid objectId, string? ownerViewId = null)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || objectId == Guid.Empty)
            return Task.CompletedTask;

        var normalizedSource = sourceType.Trim();
        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.File,
            OccupantKey = VisaPreviewSlotOccupantKeys.ForFile(normalizedSource, objectId),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            FileSourceType = normalizedSource,
            FileObjectId = objectId,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task CloseAsync()
    {
        if (_state.Mode == VisaPreviewSlotMode.Closed)
            return Task.CompletedTask;

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.Closed,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    private static string? NormalizeOwnerViewId(string? ownerViewId) =>
        string.IsNullOrWhiteSpace(ownerViewId) ? null : ownerViewId.Trim();
}
