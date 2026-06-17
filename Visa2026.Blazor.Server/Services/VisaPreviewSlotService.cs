using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Blazor.Server.Services;

public sealed class VisaPreviewSlotService : IVisaPreviewSlotService
{
    private VisaPreviewSlotState _state = new();

    public VisaPreviewSlotState State => _state;

    public event Action? StateChanged;

    public Task OpenResminamalarAsync(ResminamalarSlotRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.Resminamalar,
            Resminamalar = request,
            Version = _state.Version + 1,
        };
        StateChanged?.Invoke();
        return Task.CompletedTask;
    }

    public Task OpenFileAsync(string sourceType, Guid objectId)
    {
        if (string.IsNullOrWhiteSpace(sourceType) || objectId == Guid.Empty)
            return Task.CompletedTask;

        _state = new VisaPreviewSlotState
        {
            Mode = VisaPreviewSlotMode.File,
            FileSourceType = sourceType.Trim(),
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
}
