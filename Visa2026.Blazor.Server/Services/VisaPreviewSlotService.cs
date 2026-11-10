using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

public sealed class VisaPreviewSlotService : IVisaPreviewSlotService
{
    private readonly List<OccupantEntry> _stack = new();
    private VisaPreviewSlotState _state = new();

    public VisaPreviewSlotState State => _state;

    public event Action? StateChanged;

    public Task OpenResminamalarAsync(ResminamalarSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ReplaceStack(new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.Resminamalar,
            Key = VisaPreviewSlotOccupantKeys.ForResminamalar(request),
            Title = "Resminamalar",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            Resminamalar = request,
        });
        return Task.CompletedTask;
    }

    public Task OpenDocumentCopiesAsync(DocumentCopiesSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ReplaceStack(new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.DocumentCopies,
            Key = VisaPreviewSlotOccupantKeys.ForDocumentCopies(request),
            Title = "Document copies",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            DocumentCopies = request,
        });
        return Task.CompletedTask;
    }

    public Task OpenProgressLettersAsync(ProgressLettersSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ReplaceStack(new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.ProgressLetters,
            Key = VisaPreviewSlotOccupantKeys.ForProgressLetters(request),
            Title = "Progress letters",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            ProgressLetters = request,
        });
        return Task.CompletedTask;
    }

    public Task OpenPersonDocumentCopiesAsync(
        PersonDocumentCopiesSlotRequest request,
        string? ownerViewId = null,
        bool stackWithCurrent = false)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var entry = new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.PersonDocumentCopies,
            Key = VisaPreviewSlotOccupantKeys.ForPersonDocumentCopies(request),
            Title = request.OpenUpload
                ? (string.IsNullOrWhiteSpace(request.FocusDisplayName) ? "Upload" : request.FocusDisplayName.Trim())
                : "Person copies",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            PersonDocumentCopies = request,
        };

        var shouldStack = stackWithCurrent
            || _state.Mode is VisaPreviewSlotMode.IssueIssuedHeader or VisaPreviewSlotMode.IssueIssuedVisa;
        if (shouldStack && _stack.Count > 0)
            PushOrActivate(entry);
        else
            ReplaceStack(entry);

        return Task.CompletedTask;
    }

    public Task OpenHeaderDocumentCopiesAsync(
        HeaderDocumentCopiesSlotRequest request,
        string? ownerViewId = null,
        bool stackWithCurrent = false)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var entry = new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.HeaderDocumentCopies,
            Key = VisaPreviewSlotOccupantKeys.ForHeaderDocumentCopies(request),
            Title = TitleForHeaderCopies(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            HeaderDocumentCopies = request,
        };

        var shouldStack = stackWithCurrent
            || _state.Mode is VisaPreviewSlotMode.IssueIssuedHeader or VisaPreviewSlotMode.IssueIssuedVisa;

        if (shouldStack && _stack.Count > 0)
            PushOrActivate(entry);
        else
            ReplaceStack(entry);

        return Task.CompletedTask;
    }

    public Task OpenPlaceholderManualAsync(PlaceholderManualSlotRequest? request = null, string? ownerViewId = null)
    {
        ReplaceStack(new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.PlaceholderManual,
            Key = VisaPreviewSlotOccupantKeys.ForPlaceholderManual(request?.FilterRootBoType),
            Title = "Placeholders",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            PlaceholderManual = request ?? new PlaceholderManualSlotRequest(),
        });
        return Task.CompletedTask;
    }

    public Task OpenIssueIssuedHeaderAsync(IssueIssuedHeaderSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ReplaceStack(new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.IssueIssuedHeader,
            Key = VisaPreviewSlotOccupantKeys.ForIssueIssuedHeader(request),
            Title = TitleForIssueHeader(request),
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            IssueIssuedHeader = request,
        });
        return Task.CompletedTask;
    }

    public Task OpenIssueIssuedVisaAsync(IssueIssuedVisaSlotRequest request, string? ownerViewId = null)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        ReplaceStack(new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.IssueIssuedVisa,
            Key = VisaPreviewSlotOccupantKeys.ForIssueIssuedVisa(request),
            Title = "Visa",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            IssueIssuedVisa = request,
        });
        return Task.CompletedTask;
    }

    public Task OpenApprovalLegCatalogAsync(ApprovalLegCatalogSlotRequest? request = null, string? ownerViewId = null)
    {
        ReplaceStack(new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.ApprovalLegCatalog,
            Key = VisaPreviewSlotOccupantKeys.ForApprovalLegCatalog(),
            Title = "Approval legs",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            ApprovalLegCatalog = request ?? new ApprovalLegCatalogSlotRequest(),
        });
        return Task.CompletedTask;
    }

    public Task OpenFileAsync(string sourceType, Guid objectId, string? ownerViewId = null, bool stackWithCurrent = false)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || objectId == Guid.Empty)
            return Task.CompletedTask;

        var normalizedSource = sourceType.Trim();
        var entry = new OccupantEntry
        {
            Mode = VisaPreviewSlotMode.File,
            Key = VisaPreviewSlotOccupantKeys.ForFile(normalizedSource, objectId),
            Title = "File",
            OwnerViewId = NormalizeOwnerViewId(ownerViewId),
            FileSourceType = normalizedSource,
            FileObjectId = objectId,
        };

        if (stackWithCurrent && _stack.Count > 0)
            PushOrActivate(entry);
        else
            ReplaceStack(entry);

        return Task.CompletedTask;
    }

    public Task CloseAsync()
    {
        if (_state.Mode == VisaPreviewSlotMode.Closed && _stack.Count == 0)
            return Task.CompletedTask;

        _stack.Clear();
        PublishClosed();
        return Task.CompletedTask;
    }

    public Task ActivateTabAsync(string occupantKey)
    {
        if (string.IsNullOrWhiteSpace(occupantKey))
            return Task.CompletedTask;

        var index = _stack.FindIndex(e => string.Equals(e.Key, occupantKey.Trim(), StringComparison.Ordinal));
        if (index < 0)
            return Task.CompletedTask;

        PublishActive(index, bumpVersion: true);
        return Task.CompletedTask;
    }

    public Task CloseTabAsync(string occupantKey)
    {
        if (string.IsNullOrWhiteSpace(occupantKey))
            return Task.CompletedTask;

        var key = occupantKey.Trim();
        var index = _stack.FindIndex(e => string.Equals(e.Key, key, StringComparison.Ordinal));
        if (index < 0)
            return Task.CompletedTask;

        var wasActive = string.Equals(_state.OccupantKey, key, StringComparison.Ordinal);
        _stack.RemoveAt(index);

        if (_stack.Count == 0)
        {
            PublishClosed();
            return Task.CompletedTask;
        }

        var nextIndex = wasActive
            ? Math.Min(index, _stack.Count - 1)
            : _stack.FindIndex(e => string.Equals(e.Key, _state.OccupantKey, StringComparison.Ordinal));
        if (nextIndex < 0)
            nextIndex = _stack.Count - 1;

        PublishActive(nextIndex, bumpVersion: true);
        return Task.CompletedTask;
    }

    private void ReplaceStack(OccupantEntry entry)
    {
        _stack.Clear();
        _stack.Add(entry);
        PublishActive(0, bumpVersion: true);
    }

    private void PushOrActivate(OccupantEntry entry)
    {
        var existing = _stack.FindIndex(e => string.Equals(e.Key, entry.Key, StringComparison.Ordinal));
        if (existing >= 0)
        {
            _stack[existing] = entry;
            PublishActive(existing, bumpVersion: true);
            return;
        }

        _stack.Add(entry);
        PublishActive(_stack.Count - 1, bumpVersion: true);
    }

    private void PublishClosed()
    {
        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.Closed,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
    }

    private void PublishActive(int activeIndex, bool bumpVersion)
    {
        if (activeIndex < 0 || activeIndex >= _stack.Count)
        {
            PublishClosed();
            return;
        }

        var active = _stack[activeIndex];
        var tabs = _stack.Count > 1
            ? _stack.Select((e, i) => new VisaPreviewSlotTab
            {
                Key = e.Key,
                Title = e.Title,
                IsActive = i == activeIndex,
            }).ToArray()
            : Array.Empty<VisaPreviewSlotTab>();

        var occupants = _stack.Select((e, i) => e.ToSnapshot(isActive: i == activeIndex)).ToArray();

        _state = new VisaPreviewSlotState
        {
            Mode = active.Mode,
            Tabs = tabs,
            Occupants = occupants,
            OccupantKey = active.Key,
            OwnerViewId = active.OwnerViewId,
            FileSourceType = active.FileSourceType,
            FileObjectId = active.FileObjectId,
            Resminamalar = active.Resminamalar,
            DocumentCopies = active.DocumentCopies,
            ProgressLetters = active.ProgressLetters,
            PersonDocumentCopies = active.PersonDocumentCopies,
            HeaderDocumentCopies = active.HeaderDocumentCopies,
            PlaceholderManual = active.PlaceholderManual,
            IssueIssuedHeader = active.IssueIssuedHeader,
            IssueIssuedVisa = active.IssueIssuedVisa,
            ApprovalLegCatalog = active.ApprovalLegCatalog,
            Version = bumpVersion ? _state.Version + 1 : _state.Version,
        };
        StateChanged?.Invoke();
    }

    private static string TitleForHeaderCopies(HeaderDocumentCopiesSlotRequest request)
    {
        var family = request.Family switch
        {
            HeaderDocumentCopiesFamily.WorkPermit => "Work permit",
            HeaderDocumentCopiesFamily.Invitation => "Invitation",
            HeaderDocumentCopiesFamily.Rejection => "Rejection",
            HeaderDocumentCopiesFamily.BorderZone => "Border zone",
            HeaderDocumentCopiesFamily.Visa => "Visa copies",
            HeaderDocumentCopiesFamily.WorkPermitLocationGrant => "Location permission",
            _ => "Document copies",
        };
        return request.PreviewAllCopies || request.OpenPreviewOnly ? family : family + " catalog";
    }

    private static string TitleForIssueHeader(IssueIssuedHeaderSlotRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.CatalogKey))
        {
            var key = request.CatalogKey.Trim();
            if (key.Contains("location", StringComparison.OrdinalIgnoreCase)
                || key.Contains("gosmaca", StringComparison.OrdinalIgnoreCase)
                || key.Contains("goşmaça", StringComparison.OrdinalIgnoreCase))
                return "Location grant";
            if (key.Contains("workpermit", StringComparison.OrdinalIgnoreCase)
                || key.Contains("work-permit", StringComparison.OrdinalIgnoreCase))
                return "Work permit";
            if (key.Contains("invitation", StringComparison.OrdinalIgnoreCase))
                return "Invitation";
            if (key.Contains("border", StringComparison.OrdinalIgnoreCase))
                return "Border zone";
            if (key.Contains("rejection", StringComparison.OrdinalIgnoreCase))
                return "Rejection";
        }

        return request.Kind switch
        {
            IssueIssuedHeaderKind.WorkPermitLocationGrant => "Location grant",
            IssueIssuedHeaderKind.WorkPermit => "Work permit",
            IssueIssuedHeaderKind.Invitation => "Invitation",
            IssueIssuedHeaderKind.BorderZone => "Border zone",
            IssueIssuedHeaderKind.Rejection => "Rejection",
            _ => "Issued",
        };
    }

    private static string? NormalizeOwnerViewId(string? ownerViewId) =>
        string.IsNullOrWhiteSpace(ownerViewId) ? null : ownerViewId.Trim();

    private sealed class OccupantEntry
    {
        public VisaPreviewSlotMode Mode { get; init; }
        public string Key { get; init; } = string.Empty;
        public string Title { get; init; } = string.Empty;
        public string? OwnerViewId { get; init; }
        public string? FileSourceType { get; init; }
        public Guid FileObjectId { get; init; }
        public ResminamalarSlotRequest? Resminamalar { get; init; }
        public DocumentCopiesSlotRequest? DocumentCopies { get; init; }
        public ProgressLettersSlotRequest? ProgressLetters { get; init; }
        public PersonDocumentCopiesSlotRequest? PersonDocumentCopies { get; init; }
        public HeaderDocumentCopiesSlotRequest? HeaderDocumentCopies { get; init; }
        public PlaceholderManualSlotRequest? PlaceholderManual { get; init; }
        public IssueIssuedHeaderSlotRequest? IssueIssuedHeader { get; init; }
        public IssueIssuedVisaSlotRequest? IssueIssuedVisa { get; init; }
        public ApprovalLegCatalogSlotRequest? ApprovalLegCatalog { get; init; }

        public VisaPreviewSlotOccupantSnapshot ToSnapshot(bool isActive) => new()
        {
            Key = Key,
            Title = Title,
            IsActive = isActive,
            Mode = Mode,
            OwnerViewId = OwnerViewId,
            FileSourceType = FileSourceType,
            FileObjectId = FileObjectId,
            Resminamalar = Resminamalar,
            DocumentCopies = DocumentCopies,
            ProgressLetters = ProgressLetters,
            PersonDocumentCopies = PersonDocumentCopies,
            HeaderDocumentCopies = HeaderDocumentCopies,
            PlaceholderManual = PlaceholderManual,
            IssueIssuedHeader = IssueIssuedHeader,
            IssueIssuedVisa = IssueIssuedVisa,
            ApprovalLegCatalog = ApprovalLegCatalog,
        };
    }
}