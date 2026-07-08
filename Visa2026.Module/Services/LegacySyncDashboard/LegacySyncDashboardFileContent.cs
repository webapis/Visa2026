namespace Visa2026.Module.Services.LegacySyncDashboard;

public sealed class LegacySyncDashboardFileContent
{
    public bool Success { get; init; }

    public string? Content { get; init; }

    public string ContentType { get; init; } = "text/plain";

    public int? StatusCode { get; init; }

    public string? ErrorMessage { get; init; }

    public static LegacySyncDashboardFileContent Disabled(string message) =>
        new() { Success = false, StatusCode = 404, ErrorMessage = message };

    public static LegacySyncDashboardFileContent NotFound(string message) =>
        new() { Success = false, StatusCode = 404, ErrorMessage = message };

    public static LegacySyncDashboardFileContent Ok(string content, string contentType) =>
        new() { Success = true, Content = content, ContentType = contentType, StatusCode = 200 };
}
