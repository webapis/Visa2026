namespace Visa2026.Module.Services.PreviewSlot;

public sealed class ApplicationProgressLetterCatalogEntry
{
    public Guid ProgressId { get; init; }

    public string StatusLabel { get; init; } = string.Empty;

    public DateTime Date { get; init; }

    public string FileName { get; init; } = string.Empty;
}
