using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;

namespace Visa2026.Module.Services.ApprovalLegCatalog;

public sealed class ApprovalLegProfileSlotCatalogRow
{
    public Guid Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string MinistriesLabel { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public int UsedApplicationCount { get; init; }

    public bool IsInUse => UsedApplicationCount > 0;

    public int LegCount { get; init; }
}

public sealed class ApprovalLegProfileSlotMinistryOption
{
    public Guid Id { get; init; }

    public string Caption { get; init; } = string.Empty;
}

public sealed class ApprovalLegProfileSlotLegDraft
{
    public Guid MinistryId { get; init; }

    public string Caption { get; init; } = string.Empty;
}

public sealed class ApprovalLegProfileSlotDraft
{
    public Guid? Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public bool IsNew { get; set; }

    public bool IsInUse { get; set; }

    public int UsedApplicationCount { get; set; }

    public List<ApprovalLegProfileSlotLegDraft> Legs { get; set; } = [];
}

public static class ApprovalLegProfileSlotEditor
{
    public const int CodeMaxLength = 20;

    public const int ShortNameMaxLength = 40;

    public const int OfficialNameMaxLength = 200;

    public static IReadOnlyList<ApprovalLegProfileSlotCatalogRow> List(
        IObjectSpace objectSpace,
        string? search)
    {
        if (objectSpace == null)
            return [];

        var usedCounts = CountApplicationsByProfile(objectSpace);
        var query = objectSpace.GetObjectsQuery<ApprovalLegProfile>()
            .Include(p => p.MinistryLegs)
                .ThenInclude(l => l.ApprovingMinistry)
            .AsEnumerable();

        var rows = query
            .Select(p => new ApprovalLegProfileSlotCatalogRow
            {
                Id = p.ID,
                Code = p.Code ?? string.Empty,
                MinistriesLabel = p.MinistriesLabel ?? string.Empty,
                IsActive = p.IsActive,
                UsedApplicationCount = usedCounts.GetValueOrDefault(p.ID),
                LegCount = ApprovalLegProfileMinistryHelper.GetLegCount(p),
            })
            .Where(r => MatchesSearch(r, search))
            .OrderByDescending(r => r.IsActive)
            .ThenBy(r => r.Code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rows;
    }

    public static IReadOnlyList<ApprovalLegProfileSlotMinistryOption> ListMinistries(IObjectSpace objectSpace)
    {
        if (objectSpace == null)
            return [];

        return objectSpace.GetObjectsQuery<ApprovingMinistry>()
            .Where(m => m.IsActive)
            .AsEnumerable()
            .OrderBy(m => m.ShortNameTm, StringComparer.OrdinalIgnoreCase)
            .Select(m => new ApprovalLegProfileSlotMinistryOption
            {
                Id = m.ID,
                Caption = string.IsNullOrWhiteSpace(m.ShortNameTm) ? (m.NameTm ?? m.Code ?? m.ID.ToString()) : m.ShortNameTm,
            })
            .ToList();
    }

    public static ApprovalLegProfileSlotDraft NewDraft() =>
        new()
        {
            IsNew = true,
            IsActive = true,
            Legs = [],
        };

    public static ApprovalLegProfileSlotDraft? Load(IObjectSpace objectSpace, Guid id)
    {
        if (objectSpace == null || id == Guid.Empty)
            return null;

        var profile = objectSpace.GetObjectsQuery<ApprovalLegProfile>()
            .Include(p => p.MinistryLegs)
                .ThenInclude(l => l.ApprovingMinistry)
            .FirstOrDefault(p => p.ID == id);
        if (profile == null)
            return null;

        var used = CountApplicationsUsing(objectSpace, id);
        return new ApprovalLegProfileSlotDraft
        {
            Id = profile.ID,
            Code = profile.Code ?? string.Empty,
            IsActive = profile.IsActive,
            IsNew = false,
            IsInUse = used > 0,
            UsedApplicationCount = used,
            Legs = (profile.MinistryLegs ?? Array.Empty<ApprovalLegProfileMinistryLeg>())
                .Where(l => l.ApprovingMinistry != null)
                .OrderBy(l => l.Sequence ?? int.MaxValue)
                .Select(l => new ApprovalLegProfileSlotLegDraft
                {
                    MinistryId = l.ApprovingMinistry.ID,
                    Caption = l.ApprovingMinistry.ShortNameTm ?? l.ApprovingMinistry.NameTm ?? string.Empty,
                })
                .ToList(),
        };
    }

    public static bool TryCreate(
        IObjectSpace objectSpace,
        ApprovalLegProfileSlotDraft draft,
        out Guid id,
        out string? error)
    {
        id = Guid.Empty;
        error = null;
        if (objectSpace == null || draft == null)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.SaveFailed");
            return false;
        }

        if (!TryNormalizeDraft(objectSpace, draft, excludeId: null, out error))
            return false;

        var profile = objectSpace.CreateObject<ApprovalLegProfile>();
        ApplyScalars(profile, draft);
        if (!TryReplaceLegs(objectSpace, profile, draft.Legs, out error))
        {
            objectSpace.Rollback();
            return false;
        }

        ApprovalLegProfileMinistryHelper.WireMinistryLegs(profile);
        if (profile.IsActive
            && !ApprovalLegProfileMinistryHelper.TryValidateLegSla(objectSpace, profile, out var slaError))
        {
            error = slaError ?? VisaUiMessages.Get("MinistryReviewSlaSettings.NotConfigured");
            objectSpace.Rollback();
            return false;
        }

        try
        {
            objectSpace.CommitChanges();
        }
        catch (Exception)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.SaveFailed");
            return false;
        }

        id = profile.ID;
        return true;
    }

    public static bool TrySave(
        IObjectSpace objectSpace,
        ApprovalLegProfileSlotDraft draft,
        out string? error)
    {
        error = null;
        if (objectSpace == null || draft?.Id is not Guid id || id == Guid.Empty)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.SaveFailed");
            return false;
        }

        var profile = objectSpace.GetObjectByKey<ApprovalLegProfile>(id);
        if (profile == null)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.NotFound");
            return false;
        }

        if (!TryNormalizeDraft(objectSpace, draft, excludeId: id, out error))
            return false;

        var inUse = ApprovalLegProfileMinistryHelper.IsProfileReferencedByApplications(profile, objectSpace);
        if (inUse && HasStructuralLegChanges(profile, draft.Legs))
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.MinistryLegsStructuralEditBlocked");
            return false;
        }

        ApplyScalars(profile, draft);
        if (!inUse && !TryReplaceLegs(objectSpace, profile, draft.Legs, out error))
            return false;

        ApprovalLegProfileMinistryHelper.WireMinistryLegs(profile);
        if (profile.IsActive
            && !ApprovalLegProfileMinistryHelper.TryValidateLegSla(objectSpace, profile, out var slaError))
        {
            error = slaError ?? VisaUiMessages.Get("MinistryReviewSlaSettings.NotConfigured");
            return false;
        }

        try
        {
            objectSpace.CommitChanges();
        }
        catch (Exception)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.SaveFailed");
            return false;
        }

        return true;
    }

    public static bool TryDelete(IObjectSpace objectSpace, Guid id, out string? error)
    {
        error = null;
        if (objectSpace == null || id == Guid.Empty)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.SaveFailed");
            return false;
        }

        var profile = objectSpace.GetObjectByKey<ApprovalLegProfile>(id);
        if (profile == null)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.NotFound");
            return false;
        }

        if (ApprovalLegProfileMinistryHelper.IsProfileReferencedByApplications(profile, objectSpace))
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.DeleteInUseBlocked");
            return false;
        }

        objectSpace.Delete(profile);
        try
        {
            objectSpace.CommitChanges();
        }
        catch (Exception)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.SaveFailed");
            return false;
        }

        return true;
    }

    public static bool TryCreateMinistry(
        IObjectSpace objectSpace,
        string? shortNameTm,
        string? nameTm,
        out ApprovalLegProfileSlotMinistryOption? created,
        out string? error)
    {
        created = null;
        error = null;
        if (objectSpace == null)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.CreateMinistryFailed");
            return false;
        }

        if (!TryNormalizeNewMinistry(shortNameTm, nameTm, out var shortName, out var officialName, out error))
            return false;

        var existingNames = objectSpace.GetObjectsQuery<ApprovingMinistry>()
            .AsEnumerable()
            .Select(m => m.ShortNameTm);
        if (IsShortNameTaken(existingNames, shortName))
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.MinistryNameTaken");
            return false;
        }

        var ministry = objectSpace.CreateObject<ApprovingMinistry>();
        ministry.ShortNameTm = shortName;
        ministry.NameTm = officialName;
        ministry.IsActive = true;
#pragma warning disable CS0618
        ministry.Name = shortName;
#pragma warning restore CS0618

        try
        {
            objectSpace.CommitChanges();
        }
        catch (Exception)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.CreateMinistryFailed");
            return false;
        }

        created = new ApprovalLegProfileSlotMinistryOption
        {
            Id = ministry.ID,
            Caption = shortName,
        };
        return true;
    }

    public static bool TryNormalizeNewMinistry(
        string? shortNameTm,
        string? nameTm,
        out string shortName,
        out string officialName,
        out string? error)
    {
        shortName = Truncate(shortNameTm, ShortNameMaxLength);
        officialName = Truncate(nameTm, OfficialNameMaxLength);
        error = null;

        if (string.IsNullOrWhiteSpace(shortName))
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.ShortNameRequired");
            return false;
        }

        if (string.IsNullOrWhiteSpace(officialName))
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.OfficialNameRequired");
            return false;
        }

        return true;
    }

    public static bool IsShortNameTaken(IEnumerable<string?> existingShortNames, string shortName)
    {
        if (string.IsNullOrWhiteSpace(shortName) || existingShortNames == null)
            return false;

        foreach (var existing in existingShortNames)
        {
            if (!string.IsNullOrWhiteSpace(existing)
                && string.Equals(existing.Trim(), shortName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public static bool MatchesSearch(ApprovalLegProfileSlotCatalogRow row, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        var needle = search.Trim();
        return Contains(row.Code, needle) || Contains(row.MinistriesLabel, needle);
    }

    public static string AllocateUniqueCode(string? preferred, IReadOnlySet<string> existingCodes)
    {
        var baseCode = TruncateCode(string.IsNullOrWhiteSpace(preferred) ? "CHAIN" : preferred.Trim());
        if (!CodeTaken(baseCode, existingCodes))
            return baseCode;

        for (var i = 2; i < 100; i++)
        {
            var suffix = $"-{i}";
            var stemLen = Math.Max(1, CodeMaxLength - suffix.Length);
            var candidate = TruncateCode(baseCode, stemLen) + suffix;
            if (!CodeTaken(candidate, existingCodes))
                return candidate;
        }

        return TruncateCode(baseCode + Guid.NewGuid().ToString("N"));
    }

    private static bool TryNormalizeDraft(
        IObjectSpace objectSpace,
        ApprovalLegProfileSlotDraft draft,
        Guid? excludeId,
        out string? error)
    {
        error = null;
        draft.Legs ??= [];
        if (draft.Legs.Count == 0)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.MinistryLegsRequired");
            return false;
        }

        if (draft.Legs.Select(l => l.MinistryId).Distinct().Count() != draft.Legs.Count)
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.DuplicateMinistry");
            return false;
        }

        var shorts = draft.Legs.Select(l => l.Caption).ToList();
        var autoCode = ApprovalLegProfileCodeHelper.BuildProfileCode(shorts);
        var code = string.IsNullOrWhiteSpace(draft.Code) ? autoCode : draft.Code.Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.CodeRequired");
            return false;
        }

        code = TruncateCode(code);
        var existing = LoadExistingCodes(objectSpace, excludeId);
        if (CodeTaken(code, existing))
        {
            error = VisaUiMessages.Get("ApprovalLegProfile.Slot.CodeTaken");
            return false;
        }

        draft.Code = code;
        return true;
    }

    private static void ApplyScalars(ApprovalLegProfile profile, ApprovalLegProfileSlotDraft draft)
    {
        profile.Code = draft.Code;
        profile.IsActive = draft.IsActive;
        var shorts = draft.Legs.Select(l => l.Caption).ToList();
        var nameTm = ApprovalLegProfileCodeHelper.BuildProfileNameTm(shorts);
        profile.NameTm = string.IsNullOrWhiteSpace(nameTm) ? draft.Code : nameTm;
#pragma warning disable CS0618
        profile.Name = draft.Code;
#pragma warning restore CS0618
        profile.LocalizationKey = draft.Code;
    }

    private static bool TryReplaceLegs(
        IObjectSpace objectSpace,
        ApprovalLegProfile profile,
        IReadOnlyList<ApprovalLegProfileSlotLegDraft> legs,
        out string? error)
    {
        error = null;
        if (profile.MinistryLegs != null)
        {
            foreach (var existing in profile.MinistryLegs.ToList())
                objectSpace.Delete(existing);
        }

        var sequence = 1;
        foreach (var row in legs)
        {
            var ministry = objectSpace.GetObjectByKey<ApprovingMinistry>(row.MinistryId);
            if (ministry == null)
            {
                error = VisaUiMessages.Get("ApprovalLegProfile.Slot.MinistryMissing");
                return false;
            }

            var leg = objectSpace.CreateObject<ApprovalLegProfileMinistryLeg>();
            leg.Sequence = sequence++;
            leg.ApprovingMinistry = ministry;
            ApprovalLegProfileMinistryHelper.AttachLegToProfile(profile, leg, objectSpace);
        }

        return true;
    }

    private static bool HasStructuralLegChanges(
        ApprovalLegProfile profile,
        IReadOnlyList<ApprovalLegProfileSlotLegDraft> desired)
    {
        var current = (profile.MinistryLegs ?? Array.Empty<ApprovalLegProfileMinistryLeg>())
            .Where(l => l.ApprovingMinistry != null)
            .OrderBy(l => l.Sequence ?? int.MaxValue)
            .Select(l => l.ApprovingMinistry.ID)
            .ToList();
        if (current.Count != desired.Count)
            return true;

        for (var i = 0; i < current.Count; i++)
        {
            if (current[i] != desired[i].MinistryId)
                return true;
        }

        return false;
    }

    private static Dictionary<Guid, int> CountApplicationsByProfile(IObjectSpace objectSpace)
    {
        var ids = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ApprovalLegProfile != null)
            .Select(a => a.ApprovalLegProfile!.ID)
            .ToList();

        return ids
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private static int CountApplicationsUsing(IObjectSpace objectSpace, Guid profileId) =>
        objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Count(a => a.ApprovalLegProfile != null && a.ApprovalLegProfile.ID == profileId);

    private static HashSet<string> LoadExistingCodes(IObjectSpace objectSpace, Guid? excludeId)
    {
        var query = objectSpace.GetObjectsQuery<ApprovalLegProfile>().AsEnumerable();
        if (excludeId is Guid skip && skip != Guid.Empty)
            query = query.Where(p => p.ID != skip);

        return query
            .Select(p => p.Code)
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static bool CodeTaken(string code, IReadOnlySet<string> existing) =>
        existing.Contains(code);

    private static string TruncateCode(string value, int maxLength = CodeMaxLength) =>
        Truncate(value, maxLength);

    private static string Truncate(string? value, int maxLength)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static bool Contains(string? haystack, string needle) =>
        !string.IsNullOrEmpty(haystack)
        && haystack.Contains(needle, StringComparison.OrdinalIgnoreCase);
}
