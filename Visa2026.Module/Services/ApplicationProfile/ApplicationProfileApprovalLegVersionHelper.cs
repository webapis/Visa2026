using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;

namespace Visa2026.Module.BusinessObjects;

/// <summary>
/// Per-profile named approval-leg versions and instance ministry snapshots (slice 8l).
/// Versions are not shared across profiles. After create, instances keep a snapshot.
/// </summary>
public static class ApplicationProfileApprovalLegVersionHelper
{
    public const string DefaultVersionName = "Version 1";

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
    /// Template legs for fallback when an instance has no snapshot yet (tests / pre-cutover rows).
    /// Prefers the default version; otherwise orphan <see cref="ApplicationProfile.ApprovalLegs"/>.
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
        var fromVersions = GetOrderedVersions(profile)
            .SelectMany(GetOrderedLegs)
            .Count();
        if (fromVersions > 0)
            return fromVersions;

        return profile?.ApprovalLegs?.Count(l => l.ApprovingMinistry != null) ?? 0;
    }

    public static bool RequiresVersionPick(ApplicationProfile? profile) =>
        profile != null
        && profile.ProgressRoute == ApplicationProfileInstanceProgressRouteKind.ViaMinistries
        && GetOrderedVersions(profile).Count > 0;

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
            if (GetConfiguredLegCount(profile) > 0)
            {
                errorMessage = "This profile still has unversioned approval legs. Open Configure profile, save, and try again.";
                return false;
            }

            errorMessage = "This profile has no approval-leg versions. Configure them before creating an application.";
            return false;
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
            objectSpace.Delete(existing);

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