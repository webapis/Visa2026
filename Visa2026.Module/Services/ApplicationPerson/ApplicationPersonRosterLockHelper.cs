using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Locks ApplicationProfileInstance roster + sticky <see cref="ApplicationProfileInstancePersonResolvedLink"/> rows when the instance
/// process is terminal (plan §10.1 #13, §10.5).
/// </summary>
public static class ApplicationProfileInstancePersonRosterLockHelper
{
    /// <summary>
    /// Same trigger as <see cref="Application.IsWorkflowTerminal"/>:
    /// latest progress <c>PROCESS_ISSUED</c>, <c>PROCESS_REJECTED</c>, or <c>PROCESS_CANCELLED</c>.
    /// </summary>
    public static bool AreResolvedLinksLocked(ApplicationProfileInstance? application) =>
        !MigrationImportContext.IsDataImport
        && ApplicationProfileInstanceProgressProfileResolver.IsWorkflowTerminal(application);

    public static bool TryValidateRosterEditableWhenWorkflowTerminal(
        IObjectSpace objectSpace,
        out string? errorMessage)
    {
        errorMessage = null;
        if (objectSpace == null)
            return true;

        foreach (var link in objectSpace.GetObjectsToSave(false).OfType<ApplicationProfileInstancePersonResolvedLink>())
        {
            if (!TryValidateResolvedLinkMutation(objectSpace, link, out errorMessage))
                return false;
        }

        foreach (var obj in objectSpace.GetObjectsToDelete(false))
        {
            if (obj is ApplicationProfileInstancePersonResolvedLink resolvedLink
                && !TryValidateResolvedLinkMutation(objectSpace, resolvedLink, out errorMessage))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryValidateResolvedLinkMutation(
        IObjectSpace objectSpace,
        ApplicationProfileInstancePersonResolvedLink resolvedLink,
        out string? errorMessage)
    {
        errorMessage = null;
        var application = resolvedLink.ApplicationProfileInstance
            ?? (resolvedLink.ApplicationProfileInstanceId != Guid.Empty
                ? objectSpace.GetObjectByKey<ApplicationProfileInstance>(resolvedLink.ApplicationProfileInstanceId)
                : null);
        if (application == null || !AreResolvedLinksLocked(application))
            return true;

        errorMessage = VisaUiMessages.Get("ApplicationProfileInstancePerson.RosterLockedWhenWorkflowTerminal");
        return false;
    }
}
