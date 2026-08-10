using System;

namespace Visa2026.Module.Services.OfficerShell;

public sealed class OfficerShellStartProcessResult
{
    public bool Success { get; init; }
    public Guid ApplicationId { get; init; }
    public string? ProcessNumber { get; init; }
    public int MergedCount { get; init; }
    public string? ErrorMessage { get; init; }

    public static OfficerShellStartProcessResult Succeeded(Guid applicationId, string processNumber, int mergedCount) =>
        new()
        {
            Success = true,
            ApplicationId = applicationId,
            ProcessNumber = processNumber,
            MergedCount = mergedCount,
        };

    public static OfficerShellStartProcessResult Failed(string message) =>
        new()
        {
            Success = false,
            ErrorMessage = message,
        };
}
