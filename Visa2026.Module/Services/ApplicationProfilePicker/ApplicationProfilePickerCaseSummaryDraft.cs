using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Services.ApplicationProfilePicker;

/// <summary>
/// Create-picker Case summary step: same Use fields as Overview Edit,
/// minus Process number and auto Application number / date.
/// </summary>
public static class ApplicationProfilePickerCaseSummaryDraft
{
    public static bool IsHiddenOnCreate(string? key) =>
        string.Equals(key, ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber, StringComparison.Ordinal)
        || string.Equals(key, ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber, StringComparison.Ordinal)
        || string.Equals(key, ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceDate, StringComparison.Ordinal);

    public static IReadOnlyList<ApplicationWorkspaceCaseHeaderField> ForCreate(
        IEnumerable<ApplicationWorkspaceCaseHeaderField>? fields)
    {
        if (fields == null)
            return Array.Empty<ApplicationWorkspaceCaseHeaderField>();

        return fields.Where(field => !IsHiddenOnCreate(field.Key)).ToList();
    }

    public static bool CanCreate(IEnumerable<ApplicationWorkspaceCaseHeaderField>? fields) =>
        ApplicationWorkspaceCaseSummaryCompletenessGate.MissingRequiredFields(ForCreate(fields)).Count == 0;

    public static IReadOnlyList<ApplicationWorkspaceCaseHeaderFieldUpdate> Merge(
        IEnumerable<ApplicationWorkspaceCaseHeaderFieldUpdate>? current,
        ApplicationWorkspaceCaseHeaderFieldUpdate incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        var list = current?
            .Where(update => !string.Equals(update.Key, incoming.Key, StringComparison.Ordinal))
            .ToList()
            ?? new List<ApplicationWorkspaceCaseHeaderFieldUpdate>();
        list.Add(incoming);
        return list;
    }

    public static IReadOnlyList<ApplicationWorkspaceCaseHeaderField> Build(
        IObjectSpace objectSpace,
        Guid profileId,
        IReadOnlyList<ApplicationWorkspaceCaseHeaderFieldUpdate>? updates = null)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        if (profileId == Guid.Empty)
            return Array.Empty<ApplicationWorkspaceCaseHeaderField>();

        var profile = objectSpace.GetObjectByKey<ApplicationProfile>(profileId);
        if (profile == null)
            return Array.Empty<ApplicationWorkspaceCaseHeaderField>();

        var draft = objectSpace.CreateObject<ApplicationProfileInstance>();
        ApplicationProfilePickerApplyHelper.ApplyProfileToNewApplication(objectSpace, draft, profile);
        TryApplyUpdates(draft, objectSpace, updates, out _);
        return ForCreate(ApplicationWorkspaceCaseHeaderFieldsHelper.Build(
            draft,
            profile,
            objectSpace,
            loadLookupCatalogs: true));
    }

    public static bool TryApplyUpdates(
        ApplicationProfileInstance application,
        IObjectSpace objectSpace,
        IEnumerable<ApplicationWorkspaceCaseHeaderFieldUpdate>? updates,
        out string? error)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(objectSpace);
        error = null;
        if (updates == null)
            return true;

        foreach (var update in updates)
        {
            if (update == null || IsHiddenOnCreate(update.Key))
                continue;

            if (!ApplicationWorkspaceCaseHeaderFieldsHelper.TryApply(application, objectSpace, update, out error))
                return false;
        }

        return true;
    }
}