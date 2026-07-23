using System;
using System.Collections.Generic;
using System.Linq;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves migration-service process number (Işlenmäge başlanan belgi) from progress history
/// and formats Application lookup captions.
/// </summary>
public static class ApplicationProcessNumberHelper
{
    public const string CaptionSeparator = " · ";

    /// <summary>
    /// Prefers <c>PROCESS_STARTED.ProcessNumber</c>, then legacy Description on that step,
    /// then any other progress row with <see cref="ApplicationProgress.ProcessNumber"/>.
    /// </summary>
    public static string? ResolveFromHistory(IEnumerable<ApplicationProgress>? history)
    {
        if (history == null)
            return null;

        ApplicationProgress? started = null;
        string? anyProcessNumber = null;

        foreach (var progress in history)
        {
            if (progress == null)
                continue;

            var code = progress.State?.Code;
            if (started == null
                && string.Equals(code, ApplicationProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
            {
                started = progress;
            }

            if (anyProcessNumber == null && !string.IsNullOrWhiteSpace(progress.ProcessNumber))
                anyProcessNumber = progress.ProcessNumber.Trim();
        }

        if (started != null)
        {
            if (!string.IsNullOrWhiteSpace(started.ProcessNumber))
                return started.ProcessNumber.Trim();

            // Pre-field import: ProcessNumber lived in Description on PROCESS_STARTED.
            if (!string.IsNullOrWhiteSpace(started.Description))
                return started.Description.Trim();
        }

        return anyProcessNumber;
    }

    public static string FormatDisplayCaption(string? applicationNumber, string? processNumber)
    {
        var appNo = string.IsNullOrWhiteSpace(applicationNumber)
            ? string.Empty
            : applicationNumber.Trim();
        var procNo = string.IsNullOrWhiteSpace(processNumber)
            ? string.Empty
            : processNumber.Trim();

        if (appNo.Length == 0)
            return procNo;
        if (procNo.Length == 0)
            return appNo;
        return appNo + CaptionSeparator + procNo;
    }

    public static string FormatDisplayCaption(Application? application)
    {
        if (application == null)
            return string.Empty;

        var appNo = !string.IsNullOrWhiteSpace(application.FullApplicationNumber)
            ? application.FullApplicationNumber
            : application.ApplicationNumber;

        var processNumber = !string.IsNullOrWhiteSpace(application.ProcessNumber)
            ? application.ProcessNumber
            : ResolveFromHistory(application.ProgressHistory);

        return FormatDisplayCaption(appNo, processNumber);
    }
}