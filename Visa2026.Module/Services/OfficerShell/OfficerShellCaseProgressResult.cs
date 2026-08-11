using System;

namespace Visa2026.Module.Services.OfficerShell;

public sealed class OfficerShellCaseProgressResult
{
    public bool Success { get; init; }

    public string? ErrorMessage { get; init; }

    public string? AdvancedStateCode { get; init; }

    public static OfficerShellCaseProgressResult Succeeded(string? advancedStateCode = null) =>
        new() { Success = true, AdvancedStateCode = advancedStateCode };

    public static OfficerShellCaseProgressResult Failed(string message) =>
        new() { Success = false, ErrorMessage = message };
}
