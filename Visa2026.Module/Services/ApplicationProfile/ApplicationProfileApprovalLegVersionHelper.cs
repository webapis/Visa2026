using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Approval-leg versions for via-ministry profiles.
/// Shared source of truth: tenant <see cref="ApprovalLegProfile"/> (Configuration).
/// Per profile: optional <see cref="ApplicationProfile.DefaultApprovalLegProfile"/>.
/// At create, ministries are snapshotted onto the instance.
/// Nested <see cref="ApplicationProfileApprovalLegVersion"/> copies are legacy (slice 8l) and no longer seeded.
/// </summary>
public static class ApplicationProfileApprovalLegVersionHelper
{
    public const string DefaultVersionName = "Version 1";

    public static IReadOnlyList<ApprovalLegProfile> GetSharedActiveProfiles(IObjectSpace? objectSpace)
    {
        if (objectSpace == null)
            return [];

        return objectSpace.GetObjectsQuery<ApprovalLegProfile>()
            .Include(p => p.MinistryLegs)
                .ThenInclude(l => l.ApprovingMinistry)
            .Where(p => p.IsActive)
            .AsEnumerable()
            .Where(p => ApprovalLegProfileMinistryHelper.GetLegCount(p) > 0)
            .OrderBy(p => p.Code, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.NameTm, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<ApplicationProfileApprovalLegVersion> GetOrderedVersions(ApplicationProfile? profile)
    {
        if (profile?.ApprovalLegVersions == null)
            return [];

        return profile.ApprovalLegVersions
            .OrderBy(v => v.Sequence)
            .ThenBy(v => v.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static ApplicationProfileApprovalLegVersion? GetDefaultVersion(ApplicationProfile? profile)
    {
        var versions = GetOrderedVersions(profile);
        return versions.FirstOrDefault(v => v.IsDefault) ?? versions.FirstOrDefault();
    }

    public static ApplicationProfileApprovalLegVersion? FindVersion(ApplicationProfile? profile, Guid versionId)
    {
        if (profile == null || versionId == Guid.Empty)
            return null;

        return GetOrderedVersions(profile).FirstOrDefault(v => v.ID == versionId);
    }

    public static IReadOnlyList<ApplicationProfileApprovalLeg> GetOrderedLegs(ApplicationProfileApprovalLegVersion? version)
    {
        if (version?.Legs == null)
            return [];

        return version.Legs
            .Where(l => l.ApprovingMinistry != null)
            .OrderBy(l => l.Sequence ?? int.MaxValue)
            .ToList();
    }

    /// <summary>
    /// Legacy nested template legs when an instance has no snapshot and no shared default.
    /// Prefer <see cref="ResolveLegsForInstance"/> / <see cref="GetConfiguredLegCount"/> for the shared catalog.
    /// </summary>
    public static IReadOnlyList<ApplicationProfileApprovalLeg> GetTemplateLegs(ApplicationProfile? profile)
    {
        var version = GetDefaultVersion(profile);
        var fromVersion = GetOrderedLegs(version);
        if (fromVersion.Count > 0)
            return fromVersion;

        if (profile?.ApprovalLegs == null)
            return [];

        return profile.ApprovalLegs
            .Where(l => l.ApprovingMinistry != null && l.ApprovalLegVersion == null)
            .OrderBy(l => l.Sequence ?? int.MaxValue)
            .ToList();
    }

    public static int GetConfiguredLegCount(ApplicationProfile? profile)
    {
        var sharedDefault = profile?.DefaultApprovalLegProfile;
        var fromShared = ApprovalLegProfileMinistryHelper.GetLegCount(sharedDefault);
        if (fromShared > 0)
            return fromShared;

        var fromVersions = GetOrderedVersions(profile)
            .SelectMany(GetOrderedLegs)
            .Count();
        if (fromVersions > 0)
            return fromVersions;

        return profile?.ApprovalLegs?.Count(l => l.ApprovingMinistry != null) ?? 0;
    }

    public static bool RequiresVersionPick(ApplicationProfile? profile, IObjectSpace? objectSpace = null) =>
        profile != null
        && profile.ProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
        && (GetSharedActiveProfiles(objectSpace).Count > 0 || GetOrderedVersions(profile).Count > 0);

    /// <summary>Resolve shared <see cref="ApprovalLegProfile"/> for instance create (preferred path).</summary>
    public static bool TryResolveSharedProfileForCreate(
        ApplicationProfile profile,
        Guid? requestedApprovalLegProfileId,
        IObjectSpace objectSpace,
        out ApprovalLegProfile? sharedProfile,
        out string? errorMessage)
    {
        sharedProfile = null;
        errorMessage = null;

        if (profile.ProgressRoute != ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
            return true;

        // Prefer a single keyed load for the write ObjectSpace — do not pull the entire
        // shared catalog (MinistryLegs) into the commit graph.
        if (requestedApprovalLegProfileId is Guid requestedId && requestedId != Guid.Empty)
        {
            sharedProfile = LoadSharedProfileWithLegs(objectSpace, requestedId);
            if (sharedProfile == null || !sharedProfile.IsActive
                || ApprovalLegProfileMinistryHelper.GetLegCount(sharedProfile) <= 0)
            {
                errorMessage = "Select a valid approval-leg version for this application.";
                return false;
            }

            return true;
        }

        var shared = GetSharedActiveProfiles(objectSpace);
        var nested = GetOrderedVersions(profile);

        if (shared.Count == 0 && nested.Count == 0)
        {
            if (GetConfiguredLegCount(profile) > 0)
            {
                errorMessage = "This profile still has unversioned approval legs. Open Configure profile, save, and try again.";
                return false;
            }

            errorMessage = "No shared approval-leg versions in Configuration. Add Approval leg profiles before creating an application.";
            return false;
        }

        if (profile.DefaultApprovalLegProfileId is Guid defaultId && defaultId != Guid.Empty)
        {
            sharedProfile = LoadSharedProfileWithLegs(objectSpace, defaultId)
                ?? shared.FirstOrDefault(p => p.ID == defaultId);
            if (sharedProfile != null)
                return true;
        }

        if (shared.Count == 1)
        {
            sharedProfile = LoadSharedProfileWithLegs(objectSpace, shared[0].ID) ?? shared[0];
            return true;
        }

        if (shared.Count == 0 && nested.Count > 0)
        {
            // Caller falls back to nested TryResolveVersionForCreate.
            return true;
        }

        errorMessage = "Choose which approval-leg version this application will follow.";
        return false;
    }

    public static ApprovalLegProfile? LoadSharedProfileWithLegs(IObjectSpace objectSpace, Guid approvalLegProfileId)
    {
        if (objectSpace == null || approvalLegProfileId == Guid.Empty)
            return null;

        return objectSpace.GetObjectsQuery<ApprovalLegProfile>()
            .Include(p => p.MinistryLegs)
                .ThenInclude(l => l.ApprovingMinistry)
            .FirstOrDefault(p => p.ID == approvalLegProfileId);
    }

    public static bool TryResolveVersionForCreate(
        ApplicationProfile profile,
        Guid? requestedVersionId,
        out ApplicationProfileApprovalLegVersion? version,
        out string? errorMessage)
    {
        version = null;
        errorMessage = null;

        if (profile.ProgressRoute != ApplicationProfileInstanceProgressRouteKind.ViaMinistries)
            return true;

        var versions = GetOrderedVersions(profile);
        if (versions.Count == 0)
        {
            // Shared catalog path — nested versions optional
            return true;
        }

        if (requestedVersionId is Guid id && id != Guid.Empty)
        {
            version = FindVersion(profile, id);
            if (version == null)
            {
                errorMessage = "Select a valid approval-leg version for this application.";
                return false;
            }

            return true;
        }

        if (versions.Count == 1)
        {
            version = versions[0];
            return true;
        }

        errorMessage = "Choose which approval-leg version this application will follow.";
        return false;
    }

    public static void ApplySharedSnapshot(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApprovalLegProfile? sharedProfile)
    {
        if (objectSpace == null || application == null)
            return;

        application.ApprovalLegProfile = sharedProfile == null
            ? null
            : objectSpace.GetObject(sharedProfile);
        application.ApprovalLegVersionId = null;
        application.ApprovalLegVersionName = sharedProfile == null
            ? null
            : (sharedProfile.NameTm ?? sharedProfile.Code);

        ApprovalLegProfileMinistryHelper.ApplySnapshot(objectSpace, application, application.ApprovalLegProfile);
    }

    public static void ApplySnapshot(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        ApplicationProfileApprovalLegVersion? version)
    {
        if (objectSpace == null || application == null)
            return;

        if (application.ApprovalLegSnapshots == null)
            application.ApprovalLegSnapshots = new System.Collections.ObjectModel.ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>();

        foreach (var existing in application.ApprovalLegSnapshots.ToList())
        {
            application.ApprovalLegSnapshots.Remove(existing);
            if (objectSpace.IsNewObject(existing))
            {
                objectSpace.RemoveFromModifiedObjects(existing);
                continue;
            }

            objectSpace.Delete(existing);
        }

        application.ApprovalLegVersionId = version?.ID;
        application.ApprovalLegVersionName = version?.Name;

        if (version == null)
            return;

        var seq = 1;
        foreach (var leg in GetOrderedLegs(version))
        {
            var ministry = leg.ApprovingMinistry!;
            var snapshot = objectSpace.CreateObject<ApplicationProfileInstanceApprovalLegSnapshot>();
            snapshot.ApplicationProfileInstance = application;
            snapshot.Sequence = seq++;
            snapshot.ApprovingMinistryId = ministry.ID;
            snapshot.MinistryShortName = ministry.ShortNameTm ?? ministry.NameTm ?? string.Empty;
            snapshot.MinistryNameTm = ministry.NameTm ?? string.Empty;
            if (MinistryReviewSlaHelper.TryGetEffectiveSla(objectSpace, out var maxDays, out var warningDays))
            {
                snapshot.MaxDaysInReview = maxDays;
                snapshot.WarningDaysBeforeMax = warningDays;
            }

            application.ApprovalLegSnapshots.Add(snapshot);
        }
    }

    public static IReadOnlyList<(int Sequence, string Name)> ResolveLegsForInstance(
        ApplicationProfileInstance application,
        ApplicationProfile? profile)
    {
        var route = ApplicationProfileConfigurationResolver.GetProgressRoute(application);
        if (route == ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService)
            return [];

        var fromSnapshot = application.ApprovalLegSnapshots?
            .Where(s => !string.IsNullOrWhiteSpace(s.MinistryShortName))
            .OrderBy(s => s.Sequence ?? int.MaxValue)
            .Select((s, i) => (
                Sequence: i + 1,
                Name: s.MinistryShortName.Trim()))
            .ToList() ?? [];
        if (fromSnapshot.Count > 0)
            return fromSnapshot;

        var liveProfile = profile ?? application.ApplicationProfile;
        var shared = application.ApprovalLegProfile ?? liveProfile?.DefaultApprovalLegProfile;
        if (shared?.MinistryLegs != null)
        {
            var fromShared = shared.MinistryLegs
                .Where(l => l.ApprovingMinistry != null)
                .OrderBy(l => l.Sequence ?? int.MaxValue)
                .Select((l, i) => (
                    Sequence: i + 1,
                    Name: l.ApprovingMinistry!.ShortNameTm
                        ?? l.ApprovingMinistry.NameTm
                        ?? $"Ministry {i + 1}"))
                .ToList();
            if (fromShared.Count > 0)
                return fromShared;
        }

        return GetTemplateLegs(liveProfile)
            .Select((l, i) => (
                Sequence: i + 1,
                Name: l.ApprovingMinistry!.ShortNameTm
                    ?? l.ApprovingMinistry.NameTm
                    ?? $"Ministry {i + 1}"))
            .ToList();
    }

    public static void EnsureSingleDefault(ApplicationProfile profile, ApplicationProfileApprovalLegVersion? preferred = null)
    {
        var versions = GetOrderedVersions(profile);
        if (versions.Count == 0)
            return;

        var keep = preferred != null && versions.Contains(preferred)
            ? preferred
            : versions.FirstOrDefault(v => v.IsDefault) ?? versions[0];

        foreach (var version in versions)
            version.IsDefault = ReferenceEquals(version, keep);
    }

    public static void RenumberVersions(ApplicationProfile profile)
    {
        var seq = 1;
        foreach (var version in GetOrderedVersions(profile))
            version.Sequence = seq++;
    }

    public static void RenumberLegs(ApplicationProfileApprovalLegVersion version)
    {
        var seq = 1;
        foreach (var leg in (version.Legs ?? []).OrderBy(l => l.Sequence ?? 0))
            leg.Sequence = seq++;
    }
}