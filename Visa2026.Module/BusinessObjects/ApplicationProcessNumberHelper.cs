using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Resolves migration-service process number (Işlenmäge başlanan belgi) from progress history
/// and formats ApplicationProfileInstance lookup captions.
/// </summary>
public static class ApplicationProcessNumberHelper
{
    public const string CaptionSeparator = " · ";

    /// <summary>
    /// Prefers <c>PROCESS_STARTED.ProcessNumber</c>, then legacy Description on that step,
    /// then any other progress row with <see cref="ApplicationProfileInstanceProgress.ProcessNumber"/>.
    /// </summary>
    public static string? ResolveFromHistory(IEnumerable<ApplicationProfileInstanceProgress>? history)
    {
        if (history == null)
            return null;

        ApplicationProfileInstanceProgress? started = null;
        string? anyProcessNumber = null;

        foreach (var progress in history)
        {
            if (progress == null)
                continue;

            var code = progress.State?.Code;
            if (started == null
                && string.Equals(code, ApplicationProfileInstanceProgressStateCodes.ProcessStarted, StringComparison.OrdinalIgnoreCase))
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

    public static string FormatDisplayCaption(ApplicationProfileInstance? application)
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

    public const int MaxLength = 100;

    public static bool HasProcessStartedStep(ApplicationProfileInstance? application)
    {
        var history = application?.ProgressHistory;
        if (history == null)
            return false;

        return history.Any(p =>
            ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(p.State?.Code));
    }

    public static string? Normalize(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return null;

        var trimmed = stored.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    /// <summary>
    /// Writes the migration-service process number onto the instance and any
    /// <c>PROCESS_STARTED</c> progress row. Empty is allowed only before Submitted.
    /// </summary>
    public static bool TryAssign(
        IObjectSpace? objectSpace,
        ApplicationProfileInstance application,
        string? value,
        bool requireWhenVisible,
        out string? error)
    {
        error = null;
        ArgumentNullException.ThrowIfNull(application);

        var trimmed = Normalize(value);
        if (trimmed != null && trimmed.Length > MaxLength)
        {
            error = $"Process number cannot exceed {MaxLength} characters.";
            return false;
        }

        if (trimmed == null)
        {
            if (requireWhenVisible && HasProcessStartedStep(application))
            {
                error = "Process number is required after submitting to Migration Service.";
                return false;
            }

            application.ProcessNumber = null;
            return true;
        }

        if (IsTaken(objectSpace, application.ID, trimmed))
        {
            error = $"Process number '{trimmed}' is already used on another Application Profile Instance.";
            return false;
        }

        application.ProcessNumber = trimmed;
        if (application.ProgressHistory == null)
            return true;

        foreach (var progress in application.ProgressHistory)
        {
            if (ApplicationMigrationSlaHelper.IsMigrationServiceProcessStartedStep(progress.State?.Code))
                progress.ProcessNumber = trimmed;
        }

        return true;
    }

    public static bool IsTaken(IObjectSpace? objectSpace, Guid excludeInstanceId, string processNumber)
    {
        if (objectSpace == null || string.IsNullOrWhiteSpace(processNumber))
            return false;

        return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Any(a => a.ID != excludeInstanceId && a.ProcessNumber == processNumber);
    }

    public static string CopyForIssuedDocument(ApplicationProfileInstance? instance)
    {
        var fromInstance = Normalize(instance?.ProcessNumber);
        if (fromInstance != null)
            return fromInstance;

        return Normalize(ResolveFromHistory(instance?.ProgressHistory)) ?? string.Empty;
    }

    public static bool TryRequireForIssued(ApplicationProfileInstance? instance, out string? error)
    {
        error = null;
        if (instance == null || !ApplicationProfileConfigurationResolver.ShowProcessNumber(instance))
            return true;

        if (!string.IsNullOrWhiteSpace(CopyForIssuedDocument(instance)))
            return true;

        error = "Process number is required on this case before issuing.";
        return false;
    }

    public static void ApplyToVisa(Visa visa, ApplicationProfileInstance? instance)
    {
        ArgumentNullException.ThrowIfNull(visa);
        var copied = CopyForIssuedDocument(instance);
        if (!string.IsNullOrWhiteSpace(copied))
            visa.ProcessNumber = copied;
        else if (string.IsNullOrWhiteSpace(visa.ProcessNumber))
            visa.ProcessNumber = visa.VisaNumber;
    }
}