using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.PreviewSlot;

public static partial class IssueIssuedHeaderComposeService
{
    public static string FormatSourceVisaCaption(Visa? visa)
    {
        if (visa == null)
            return string.Empty;

        var number = visa.VisaNumber?.Trim() ?? string.Empty;
        var end = visa.ExpirationDate?.Date;
        if (string.IsNullOrWhiteSpace(number) && end == null)
            return string.Empty;
        if (end == null)
            return number;
        if (string.IsNullOrWhiteSpace(number))
            return $"ends {end:dd.MM.yyyy}";
        return $"{number} (ends {end:dd.MM.yyyy})";
    }

    public static bool BorderZoneEndDateDiffersFromVisa(IssueIssuedHeaderComposeDraft? draft)
    {
        if (draft == null || draft.Kind != IssueIssuedHeaderKind.BorderZone)
            return false;
        if (draft.SourceVisaEndDate is not DateTime visaEnd || draft.ExpirationDate is not DateTime end)
            return false;
        return visaEnd.Date != end.Date;
    }

    /// <summary>
    /// Next linked visa on the case that does not yet have a BorderZone with that SourceVisa.
    /// </summary>
    public static Guid? TryResolveNextUngrantedSourceVisaId(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId)
    {
        if (objectSpace == null || applicationProfileInstanceId == Guid.Empty)
            return null;

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationProfileInstanceId);
        if (instance == null)
            return null;

        var grantedVisaIds = objectSpace.GetObjectsQuery<BorderZone>()
            .Where(z => z.ApplicationProfileInstance != null
                && z.ApplicationProfileInstance.ID == applicationProfileInstanceId
                && z.SourceVisa != null)
            .Select(z => z.SourceVisa!.ID)
            .Distinct()
            .ToHashSet();

        var candidates = new List<Visa>();
        foreach (var person in ApplicationRosterHelper.GetRosterPeople(instance).Where(p => p != null))
        {
            var visa = IssuedExtensionDefaultsHelper.TryResolveLinkedVisa(objectSpace, instance, person);
            if (visa == null || visa.ID == Guid.Empty)
                continue;
            if (grantedVisaIds.Contains(visa.ID))
                continue;
            if (candidates.Any(v => v.ID == visa.ID))
                continue;
            candidates.Add(visa);
        }

        return candidates
            .OrderBy(v => v.VisaNumber)
            .ThenBy(v => v.ID)
            .Select(v => (Guid?)v.ID)
            .FirstOrDefault();
    }

    internal static void ApplyBorderZoneSourceVisaToDraft(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedHeaderComposeDraft draft,
        Guid sourceVisaId)
    {
        var visa = objectSpace.GetObjectByKey<Visa>(sourceVisaId);
        draft.SourceVisaId = sourceVisaId;
        draft.SourceVisaCaption = FormatSourceVisaCaption(visa);
        draft.SourceVisaEndDate = visa?.ExpirationDate?.Date;
        if (draft.ExpirationDate == null && draft.SourceVisaEndDate is DateTime end)
            draft.ExpirationDate = end;

        var scoped = new List<IssueIssuedHeaderPersonLineDraft>();
        foreach (var line in draft.People)
        {
            var person = objectSpace.GetObjectByKey<Person>(line.PersonId);
            var linked = IssuedExtensionDefaultsHelper.TryResolveLinkedVisa(objectSpace, instance, person);
            if (linked == null || linked.ID != sourceVisaId)
                continue;

            line.SourceVisaId = sourceVisaId;
            line.SourceVisaCaption = draft.SourceVisaCaption;
            line.Include = line.IsReady;
            scoped.Add(line);
        }

        draft.People.Clear();
        draft.People.AddRange(scoped);
    }

    internal static void ApplyBorderZoneVisaCaptionsToPeople(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedHeaderComposeDraft draft)
    {
        if (draft.SourceVisaId is not Guid sourceVisaId || sourceVisaId == Guid.Empty)
            return;

        var caption = draft.SourceVisaCaption;
        if (string.IsNullOrWhiteSpace(caption))
        {
            var visa = objectSpace.GetObjectByKey<Visa>(sourceVisaId);
            caption = FormatSourceVisaCaption(visa);
            draft.SourceVisaCaption = caption;
            draft.SourceVisaEndDate = visa?.ExpirationDate?.Date;
        }

        foreach (var line in draft.People)
        {
            line.SourceVisaId = sourceVisaId;
            line.SourceVisaCaption = caption;
        }
    }
}