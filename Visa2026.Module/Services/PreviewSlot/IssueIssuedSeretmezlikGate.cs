using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Seretmezlik-excluded people stay off Application Result create pickers
/// (Invitation / Work permit / Visa / Border zone / Rejection) and cannot be issued via compose Create.
/// </summary>
public static class IssueIssuedSeretmezlikGate
{
    public const string BlockedCreateMessage =
        "Cannot create for people excluded by Seretmezlik.";

    public static HashSet<Guid> ExcludedPersonIds(
        IObjectSpace? objectSpace,
        ApplicationProfileInstance? instance) =>
        ApplicationProfileInstanceExclusionQueries.GetExcludedPersonIds(instance, objectSpace);

    public static bool IsExcluded(Guid personId, IReadOnlySet<Guid> excluded) =>
        personId != Guid.Empty && excluded != null && excluded.Contains(personId);

    /// <summary>
    /// Blocks Create (and Update when adding people) if any selected person is Seretmezlik-excluded
    /// and not already on the letter (<paramref name="existingLineId"/> empty).
    /// </summary>
    public static string? RejectIfSelectingExcluded(
        IObjectSpace? objectSpace,
        ApplicationProfileInstance? instance,
        IEnumerable<(Guid PersonId, Guid? ExistingLineId, string? PersonName)> selected)
    {
        if (instance == null || selected == null)
            return null;

        var excluded = ExcludedPersonIds(objectSpace, instance);
        if (excluded.Count == 0)
            return null;

        var offenders = selected
            .Where(s => IsExcluded(s.PersonId, excluded)
                && (s.ExistingLineId is not Guid line || line == Guid.Empty))
            .Select(s => string.IsNullOrWhiteSpace(s.PersonName) ? s.PersonId.ToString("N")[..8] : s.PersonName!.Trim())
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        if (offenders.Count == 0)
            return null;

        return BlockedCreateMessage + " " + string.Join(", ", offenders);
    }
}