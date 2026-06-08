using System;
using System.Text.RegularExpressions;

namespace Visa2026.Module.Services.RuntimeLogging;

public static partial class ApplicationRuntimeLogTextHelper
{
    private static readonly Regex BatchIdRegex = BatchIdPattern();

    public static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || maxLength <= 0)
            return value;

        return value.Length <= maxLength ? value : value[..maxLength];
    }

    public static string? ScrubSecrets(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value
            .Replace("Password=", "Password=***", StringComparison.OrdinalIgnoreCase)
            .Replace("pwd=", "pwd=***", StringComparison.OrdinalIgnoreCase);
    }

    public static Guid? TryExtractBatchId(string? message, IReadOnlyList<KeyValuePair<string, object?>>? state)
    {
        if (state != null)
        {
            foreach (var pair in state)
            {
                if (string.Equals(pair.Key, "BatchId", StringComparison.OrdinalIgnoreCase)
                    && pair.Value is Guid guid && guid != Guid.Empty)
                    return guid;
            }
        }

        if (string.IsNullOrWhiteSpace(message))
            return null;

        var match = BatchIdRegex.Match(message);
        return match.Success && Guid.TryParse(match.Groups[1].Value, out var fromMessage)
            ? fromMessage
            : null;
    }

    public static string? ResolveErrorCode(
        IReadOnlyList<KeyValuePair<string, object?>>? state,
        string category,
        string? message)
    {
        if (state != null)
        {
            foreach (var pair in state)
            {
                if (!string.Equals(pair.Key, ApplicationRuntimeLogErrorCodes.PropertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (pair.Value is string code && !string.IsNullOrWhiteSpace(code))
                    return Truncate(code.Trim(), 64);
            }
        }

        return TryInferErrorCodeFromMessage(category, message);
    }

    public static string? TryExtractErrorCode(string category, string? message) =>
        TryInferErrorCodeFromMessage(category, message);

    private static string? TryInferErrorCodeFromMessage(string category, string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return null;

        if (message.Contains("PDF batch failed", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.PdfBatchFailed;
        if (message.Contains("Resminamalar batch failed", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.WordBatchFailed;
        if (message.Contains("PdfGenerationBatchWorkerService loop error", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.PdfWorkerLoop;
        if (message.Contains("WordReportGenerationBatchWorkerService loop error", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.WordWorkerLoop;
        if (message.Contains("User report template seed failed", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.InfraTemplateSeed;
        if (message.Contains("Batch schema column ensure failed", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.InfraBatchSchema;
        if (message.Contains("Error occurred while cleaning temporary files", StringComparison.OrdinalIgnoreCase))
            return ApplicationRuntimeLogErrorCodes.TempCleanup;
        if (category.Contains("ExceptionHandlerMiddleware", StringComparison.Ordinal))
            return ApplicationRuntimeLogErrorCodes.HttpUnhandled;
        if (category.Contains("Circuits", StringComparison.Ordinal))
            return ApplicationRuntimeLogErrorCodes.BlazorCircuit;

        return null;
    }

    [GeneratedRegex(@"BatchId=([0-9a-fA-F-]{36})", RegexOptions.CultureInvariant)]
    private static partial Regex BatchIdPattern();
}
