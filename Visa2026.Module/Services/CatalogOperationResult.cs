namespace Visa2026.Module.Services;

/// <summary>Result of rename/delete on a comma-separated multi-select catalog entry.</summary>
public sealed class CatalogOperationResult
{
    public bool Success { get; init; }

    public string? Message { get; init; }

    public int UsageCount { get; init; }

    public static CatalogOperationResult Ok(string? message = null, int usageCount = 0) =>
        new() { Success = true, Message = message, UsageCount = usageCount };

    public static CatalogOperationResult Fail(string message, int usageCount = 0) =>
        new() { Success = false, Message = message, UsageCount = usageCount };
}
