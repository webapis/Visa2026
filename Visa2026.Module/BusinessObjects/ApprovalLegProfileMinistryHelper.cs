using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

public static class ApprovalLegProfileMinistryHelper
{
    /// <summary>True while a leg popup commit is being redirected to a parent object-space commit.</summary>
    internal static bool IsLegCommitRedirectInProgress => LegCommitRedirectScope.IsActive;

    /// <summary>Skip nested popup sessions during redirect — the parent session prepares legs once.</summary>
    internal static bool ShouldPrepareLegsOnCommit(IObjectSpace objectSpace) =>
        !(IsLegCommitRedirectInProgress && ObjectSpaceHelper.IsNestedObjectSpace(objectSpace));

    /// <summary>
    /// True when a new parent profile would not be inserted in the same commit batch as its leg.
    /// </summary>
    internal static bool WouldOrphanLegForeignKey(
        bool isParentNewObject,
        Guid parentId,
        IEnumerable<Guid> profileIdsInCommitBatch) =>
        isParentNewObject
        && parentId != Guid.Empty
        && !profileIdsInCommitBatch.Contains(parentId);

    /// <summary>
    /// Ensures aggregated legs reference the parent profile before EF commit.
    /// Nested Blazor list rows often populate <see cref="ApprovalLegProfile.MinistryLegs"/> without back-references.
    /// </summary>
    public static void PrepareLegsForCommit(IObjectSpace objectSpace)
    {
        if (!ShouldPrepareLegsOnCommit(objectSpace))
            return;

        using var preparing = PrepareLegsForCommitScope.TryEnter(objectSpace);
        if (preparing == null)
            return;

        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        var profilesToSave = CollectProfilesInCommitBatch(objectSpace, rootObjectSpace);
        var legsToSave = CollectLegsInCommitBatch(objectSpace, rootObjectSpace, profilesToSave);

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
            WireOrphanLegsInRoot(objectSpace, rootObjectSpace, legsToSave);

        foreach (var profile in profilesToSave)
        {
            var targetSpace = ResolveObjectSpaceFor(profile, objectSpace, rootObjectSpace);
            var profileInTarget = ResolveProfileInObjectSpace(targetSpace, profile) ?? profile;
            RehomeProfileLegsInObjectSpace(targetSpace, profileInTarget);
            WireMinistryLegs(profileInTarget);
            targetSpace.SetModified(profileInTarget);
        }

        foreach (var leg in legsToSave)
        {
            PrepareLegForSave(objectSpace, rootObjectSpace, leg);
            EnsureLegHasParentNavigation(objectSpace, rootObjectSpace, leg);
        }

        foreach (var profile in CollectParentProfiles(objectSpace, rootObjectSpace, legsToSave))
            ResolveObjectSpaceFor(profile, objectSpace, rootObjectSpace).SetModified(profile);
    }

    /// <summary>
    /// When a leg popup commits, ensure the unsaved parent profile is in the same batch.
    /// Redirects to the parent session when the popup uses a different object space.
    /// </summary>
    public static bool TryFinalizeLegCommit(IObjectSpace objectSpace, CancelEventArgs e)
    {
        if (IsLegCommitRedirectInProgress)
            return true;

        PrepareLegsForCommit(objectSpace);

        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        var profilesToSave = CollectProfilesInCommitBatch(objectSpace, rootObjectSpace);
        var legsToSave = CollectLegsInCommitBatch(objectSpace, rootObjectSpace, profilesToSave);
        if (legsToSave.Count == 0)
            return true;

        foreach (var leg in legsToSave)
            EnsureLegParentInCommitBatch(objectSpace, leg);

        if (legsToSave.Any(leg => IsLegForeignKeyOrphaned(objectSpace, leg))
            || legsToSave.Any(leg => leg.ApprovalLegProfile == null))
        {
            e.Cancel = true;
            throw new UserFriendlyException(VisaUiMessages.Get("ApprovalLegProfile.SaveBeforeMinistryLeg"));
        }

        return true;
    }

    /// <summary>
    /// Leg popup Save on a new contract: commit the root session (parent + leg) instead of the popup session.
    /// </summary>
    public static bool TryCommitParentWithLeg(
        IObjectSpace legObjectSpace,
        ApprovalLegProfileMinistryLeg leg,
        CancelEventArgs e)
    {
        PrepareLegsForCommit(legObjectSpace);
        TryAttachLegToParent(legObjectSpace, leg);

        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(legObjectSpace) ?? legObjectSpace;
        var parent = leg.ApprovalLegProfile
            ?? FindParentProfile(legObjectSpace, rootObjectSpace, leg);
        if (parent == null)
            return false;

        var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(legObjectSpace, parent);
        var parentInSpace = ResolveProfileInObjectSpace(parentSpace, parent);
        if (parentInSpace == null || !parentSpace.IsNewObject(parentInSpace))
            return false;

        var legInSpace = EnsureLegInObjectSpace(parentSpace, parentInSpace, leg);
        PrepareLegsForCommit(parentSpace);
        parentSpace.SetModified(parentInSpace);

        if (TryRedirectCommitToParentSpace(parentSpace, legObjectSpace, e))
            return true;

        return legInSpace.ApprovalLegProfile != null && !IsLegForeignKeyOrphaned(parentSpace, legInSpace);
    }

    /// <summary>After frame-based parent resolution, verify the leg can commit without FK orphan.</summary>
    public static bool CanCommitLeg(IObjectSpace committingObjectSpace, ApprovalLegProfileMinistryLeg leg)
    {
        PrepareLegsForCommit(committingObjectSpace);
        EnsureLegParentInCommitBatch(committingObjectSpace, leg);
        return leg.ApprovalLegProfile != null && !IsLegForeignKeyOrphaned(committingObjectSpace, leg);
    }

    private static void EnsureLegParentInCommitBatch(
        IObjectSpace committingObjectSpace,
        ApprovalLegProfileMinistryLeg leg)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(committingObjectSpace) ?? committingObjectSpace;
        TryAttachLegToParent(committingObjectSpace, leg);

        if (IsLegForeignKeyOrphaned(committingObjectSpace, leg)
            && !ReferenceEquals(committingObjectSpace, rootObjectSpace))
        {
            WireOrphanLegsInRoot(committingObjectSpace, rootObjectSpace, [leg]);
        }
    }

    private static bool IsLegForeignKeyOrphaned(IObjectSpace committingObjectSpace, ApprovalLegProfileMinistryLeg leg)
    {
        if (NeedsParentInCommitBatch(committingObjectSpace, leg, out _))
            return true;

        var legSpace = ObjectSpaceHelper.Get(leg);
        if (legSpace != null
            && !ReferenceEquals(legSpace, committingObjectSpace)
            && NeedsParentInCommitBatch(legSpace, leg, out _))
        {
            return true;
        }

        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(committingObjectSpace) ?? committingObjectSpace;
        if (!ReferenceEquals(rootObjectSpace, committingObjectSpace)
            && NeedsParentInCommitBatch(rootObjectSpace, leg, out _))
        {
            return true;
        }

        return false;
    }

    private static bool TryRedirectCommitToParentSpace(
        IObjectSpace parentSpace,
        IObjectSpace legObjectSpace,
        CancelEventArgs e)
    {
        if (ReferenceEquals(parentSpace, legObjectSpace))
            return false;

        using var scope = LegCommitRedirectScope.TryEnter();
        if (scope == null)
        {
            e.Cancel = true;
            return true;
        }

        e.Cancel = true;
        parentSpace.CommitChanges();
        return true;
    }

    public static void WireMinistryLegs(ApprovalLegProfile profile)
    {
        if (profile.MinistryLegs == null)
            return;

        // Snapshot: assigning ApprovalLegProfile / SyncForeignKeys mutates the same
        // InverseProperty collection, which throws if we enumerate the live list.
        foreach (var leg in profile.MinistryLegs.ToList())
        {
            leg.ApprovalLegProfile = profile;
            leg.SyncForeignKeys();
        }
    }

    public static void PrepareLegForSave(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        ApprovalLegProfileMinistryLeg leg)
    {
        if (leg.ApprovalLegProfile == null)
        {
            EnsureLegParentProfile(
                objectSpace,
                rootObjectSpace,
                leg,
                objectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfile>().ToList());
        }

        leg.SyncForeignKeys();
    }

    /// <summary>Links a leg to its parent profile in the given object space (popup save / nested list).</summary>
    public static bool TryAttachLegToParent(IObjectSpace objectSpace, ApprovalLegProfileMinistryLeg leg)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;

        if (leg.ApprovalLegProfile != null)
        {
            var parentSpace = ResolveObjectSpaceFor(leg.ApprovalLegProfile, objectSpace, rootObjectSpace);
            var parentInSpace = ResolveProfileInObjectSpace(parentSpace, leg.ApprovalLegProfile) ?? leg.ApprovalLegProfile;
            EnsureLegInObjectSpace(parentSpace, parentInSpace, leg);
            parentSpace.SetModified(parentInSpace);
            return true;
        }

        var parent = FindParentProfile(objectSpace, rootObjectSpace, leg);
        if (parent == null)
            return false;

        var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
        var legInTarget = EnsureLegInObjectSpace(targetSpace, parent, leg);
        return true;
    }

    public static void AttachLegToProfile(
        ApprovalLegProfile profile,
        ApprovalLegProfileMinistryLeg leg,
        IObjectSpace objectSpace)
    {
        leg.ApprovalLegProfile = profile;
        if (profile.MinistryLegs != null && !profile.MinistryLegs.Contains(leg))
            profile.MinistryLegs.Add(leg);

        leg.SyncForeignKeys();
        objectSpace.SetModified(profile);
    }

    internal static bool NeedsParentInCommitBatch(
        IObjectSpace objectSpace,
        ApprovalLegProfileMinistryLeg leg,
        out ApprovalLegProfile? parent)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        parent = leg.ApprovalLegProfile ?? FindParentProfile(objectSpace, rootObjectSpace, leg);

        if (parent == null)
            return false;

        var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(objectSpace, parent);
        var parentInSpace = ResolveProfileInObjectSpace(parentSpace, parent) ?? parent;
        var profileIds = CollectProfileIdsInCommitBatch(parentSpace);
        return WouldOrphanLegForeignKey(
            parentSpace.IsNewObject(parentInSpace),
            parentInSpace.ID,
            profileIds);
    }

    private static List<ApprovalLegProfile> CollectProfilesInCommitBatch(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace)
    {
        var contracts = objectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfile>()
            .Concat(objectSpace.GetObjectsToSave(true).OfType<ApprovalLegProfile>())
            .Concat(objectSpace.ModifiedObjects.OfType<ApprovalLegProfile>())
            .ToList();

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
        {
            contracts = contracts
                .Concat(rootObjectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfile>())
                .Concat(rootObjectSpace.GetObjectsToSave(true).OfType<ApprovalLegProfile>())
                .Concat(rootObjectSpace.ModifiedObjects.OfType<ApprovalLegProfile>())
                .ToList();
        }

        return contracts.Distinct().ToList();
    }

    private static List<ApprovalLegProfileMinistryLeg> CollectLegsInCommitBatch(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        IReadOnlyList<ApprovalLegProfile> profilesToSave)
    {
        var legs = objectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfileMinistryLeg>()
            .Concat(objectSpace.GetObjectsToSave(true).OfType<ApprovalLegProfileMinistryLeg>())
            .ToList();

        foreach (var profile in profilesToSave)
        {
            if (profile.MinistryLegs == null)
                continue;

            foreach (var leg in profile.MinistryLegs)
            {
                if (!legs.Contains(leg))
                    legs.Add(leg);
            }
        }

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
        {
            legs = legs
                .Concat(rootObjectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfileMinistryLeg>())
                .Concat(rootObjectSpace.GetObjectsToSave(true).OfType<ApprovalLegProfileMinistryLeg>())
                .ToList();
        }

        return legs.Distinct().ToList();
    }

    private static IEnumerable<Guid> CollectProfileIdsInCommitBatch(IObjectSpace objectSpace)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        return CollectProfilesInCommitBatch(objectSpace, rootObjectSpace).Select(c => c.ID).Distinct();
    }

    private static void RehomeProfileLegsInObjectSpace(IObjectSpace targetSpace, ApprovalLegProfile profile)
    {
        if (profile.MinistryLegs == null)
            return;

        foreach (var leg in profile.MinistryLegs.ToList())
            EnsureLegInObjectSpace(targetSpace, profile, leg);
    }

    /// <summary>
    /// Ensures the leg instance tracked for commit lives in <paramref name="targetSpace"/>
    /// and is linked to <paramref name="contractInTarget"/>.
    /// </summary>
    internal static ApprovalLegProfileMinistryLeg EnsureLegInObjectSpace(
        IObjectSpace targetSpace,
        ApprovalLegProfile contractInTarget,
        ApprovalLegProfileMinistryLeg sourceLeg)
    {
        var resolved = ResolveLegInObjectSpace(targetSpace, sourceLeg, contractInTarget);
        if (resolved != null)
        {
            AttachLegToProfile(contractInTarget, resolved, targetSpace);
            return resolved;
        }

        var sourceSpace = ObjectSpaceHelper.Get(sourceLeg);
        if (sourceSpace != null && ReferenceEquals(sourceSpace, targetSpace))
        {
            AttachLegToProfile(contractInTarget, sourceLeg, targetSpace);
            return sourceLeg;
        }

        var copy = targetSpace.CreateObject<ApprovalLegProfileMinistryLeg>();
        copy.Sequence = sourceLeg.Sequence;
        if (sourceLeg.ApprovingMinistry != null)
            copy.ApprovingMinistry = targetSpace.GetObject(sourceLeg.ApprovingMinistry);
        AttachLegToProfile(contractInTarget, copy, targetSpace);

        if (sourceSpace != null
            && !ReferenceEquals(sourceSpace, targetSpace)
            && sourceSpace.IsNewObject(sourceLeg)
            && !IsLegCommitRedirectInProgress)
        {
            sourceSpace.Delete(sourceLeg);
        }

        return copy;
    }

    private static void EnsureLegHasParentNavigation(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        ApprovalLegProfileMinistryLeg leg)
    {
        if (leg.ApprovalLegProfile != null)
        {
            leg.SyncForeignKeys();
            return;
        }

        TryAttachLegToParent(objectSpace, leg);
        if (leg.ApprovalLegProfile != null)
        {
            leg.SyncForeignKeys();
            return;
        }

        var parent = FindParentProfile(objectSpace, rootObjectSpace, leg);
        if (parent == null)
            return;

        var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
        var parentInTarget = ResolveProfileInObjectSpace(targetSpace, parent) ?? parent;
        EnsureLegInObjectSpace(targetSpace, parentInTarget, leg);
    }

    private static void WireOrphanLegsInRoot(
        IObjectSpace committingObjectSpace,
        IObjectSpace rootObjectSpace,
        IReadOnlyList<ApprovalLegProfileMinistryLeg> legsToSave)
    {
        foreach (var leg in legsToSave)
        {
            if (!NeedsParentInCommitBatch(committingObjectSpace, leg, out var parent) || parent == null)
                continue;

            var rootParent = ResolveProfileInObjectSpace(rootObjectSpace, parent);
            if (rootParent == null)
                continue;

            var rootLeg = EnsureLegInObjectSpace(rootObjectSpace, rootParent, leg);
        }
    }

    private static IEnumerable<ApprovalLegProfile> CollectParentProfiles(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        IReadOnlyList<ApprovalLegProfileMinistryLeg> legsToSave)
    {
        var parents = new HashSet<ApprovalLegProfile>();
        foreach (var leg in legsToSave)
        {
            if (leg.ApprovalLegProfile != null)
                parents.Add(leg.ApprovalLegProfile);
        }

        foreach (var leg in legsToSave)
        {
            if (leg.ApprovalLegProfile != null)
                continue;

            var parent = FindParentProfile(objectSpace, rootObjectSpace, leg);
            if (parent == null)
                continue;

            var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
            var parentInTarget = ResolveProfileInObjectSpace(targetSpace, parent) ?? parent;
            EnsureLegInObjectSpace(targetSpace, parentInTarget, leg);
            parents.Add(parentInTarget);
        }

        return parents;
    }

    private static void EnsureLegParentProfile(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        ApprovalLegProfileMinistryLeg leg,
        IReadOnlyList<ApprovalLegProfile> profilesToSave)
    {
        var parent = FindParentProfile(objectSpace, rootObjectSpace, leg, profilesToSave);
        if (parent == null)
            return;

        var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
        var parentInTarget = ResolveProfileInObjectSpace(targetSpace, parent) ?? parent;
        EnsureLegInObjectSpace(targetSpace, parentInTarget, leg);
    }

    private static ApprovalLegProfile? FindParentProfile(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        ApprovalLegProfileMinistryLeg leg,
        IReadOnlyList<ApprovalLegProfile>? profilesToSave = null)
    {
        var parent = FindParentProfileInObjectSpace(objectSpace, leg, profilesToSave);
        if (parent != null)
            return parent;

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
        {
            parent = FindParentProfileInObjectSpace(
                rootObjectSpace,
                leg,
                CollectProfilesInCommitBatch(rootObjectSpace, rootObjectSpace));
        }

        return parent;
    }

    private static ApprovalLegProfile? FindParentProfileInObjectSpace(
        IObjectSpace objectSpace,
        ApprovalLegProfileMinistryLeg leg,
        IReadOnlyList<ApprovalLegProfile>? profilesToSave = null)
    {
        profilesToSave ??= objectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfile>().ToList();

        var parent = profilesToSave.FirstOrDefault(c => c.MinistryLegs?.Contains(leg) == true);
        if (parent != null)
            return parent;

        foreach (var profile in objectSpace.ModifiedObjects.OfType<ApprovalLegProfile>())
        {
            if (profile.MinistryLegs?.Contains(leg) == true)
                return profile;
        }

        var modifiedContracts = objectSpace.ModifiedObjects.OfType<ApprovalLegProfile>().ToList();
        if (modifiedContracts.Count == 1)
            return modifiedContracts[0];

        var contractsBeingEdited = objectSpace.GetObjectsToSave(false).OfType<ApprovalLegProfile>().ToList();
        if (contractsBeingEdited.Count == 1)
            return contractsBeingEdited[0];

        return null;
    }

    private static ApprovalLegProfile? ResolveProfileInObjectSpace(
        IObjectSpace targetObjectSpace,
        ApprovalLegProfile source)
    {
        if (targetObjectSpace.IsNewObject(source))
            return targetObjectSpace.GetObject(source) as ApprovalLegProfile ?? source;

        return targetObjectSpace.GetObjectByKey<ApprovalLegProfile>(source.ID);
    }

    private static ApprovalLegProfileMinistryLeg? ResolveLegInObjectSpace(
        IObjectSpace targetObjectSpace,
        ApprovalLegProfileMinistryLeg source,
        ApprovalLegProfile? parent)
    {
        var leg = targetObjectSpace.GetObject(source) as ApprovalLegProfileMinistryLeg;
        if (leg != null)
            return leg;

        return parent?.MinistryLegs?.FirstOrDefault(l => l.ID == source.ID);
    }

    private static IObjectSpace ResolveObjectSpaceFor(
        ApprovalLegProfile profile,
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace) =>
        ObjectSpaceHelper.Get(profile) ?? rootObjectSpace ?? objectSpace;

    public static int GetLegCount(ApprovalLegProfile? profile) =>
        profile?.MinistryLegs?.Count(l => l.ApprovingMinistry != null) ?? 0;

    public static bool HasConfiguredLegs(ApprovalLegProfile? profile) =>
        GetLegCount(profile) > 0;

    public static bool TryValidateLegSla(
        IObjectSpace objectSpace,
        ApprovalLegProfile? profile,
        out string? errorMessage)
    {
        errorMessage = null;
        if (profile == null || !profile.IsActive)
            return true;

        if (!HasConfiguredLegs(profile))
            return true;

        return MinistryReviewSlaHelper.TryValidateConfigured(objectSpace, out errorMessage);
    }


    public static void ApplySnapshot(IObjectSpace objectSpace, Application application, ApprovalLegProfile? profile)
    {
        if (application.ApprovalLegSnapshots == null)
            return;

        foreach (var existing in application.ApprovalLegSnapshots.ToList())
            objectSpace.Delete(existing);

        if (profile?.MinistryLegs == null)
            return;

        foreach (var leg in profile.MinistryLegs
                     .Where(l => l.ApprovingMinistry != null)
                     .OrderBy(l => l.Sequence))
        {
            var snapshot = objectSpace.CreateObject<ApplicationApprovalLegSnapshot>();
            snapshot.Application = application;
            snapshot.Sequence = leg.Sequence;
            snapshot.ApprovingMinistryId = leg.ApprovingMinistry.ID;
            snapshot.MinistryShortName = leg.ApprovingMinistry.ShortNameTm ?? leg.ApprovingMinistry.NameTm ?? string.Empty;
            snapshot.MinistryNameTm = leg.ApprovingMinistry.NameTm ?? string.Empty;
            if (MinistryReviewSlaHelper.TryGetEffectiveSla(objectSpace, out var maxDays, out var warningDays))
            {
                snapshot.MaxDaysInReview = maxDays;
                snapshot.WarningDaysBeforeMax = warningDays;
            }

            application.ApprovalLegSnapshots.Add(snapshot);
        }
    }
    public static bool IsProfileReferencedByApplications(ApprovalLegProfile profile, IObjectSpace objectSpace) =>
        objectSpace.GetObjectsQuery<Application>()
            .Any(a => a.ApprovalLegProfile != null && a.ApprovalLegProfile.ID == profile.ID);

    public static string? GetMinistryShortNameForLeg(Application? application, int leg)
    {
        if (application?.ApprovalLegSnapshots == null || leg < 1)
            return null;

        return application.ApprovalLegSnapshots
            .Where(s => s.Sequence == leg)
            .Select(s => s.MinistryShortName)
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
    }

    public static string? GetMinistryShortNameForProgressStep(
        Application? application,
        string? stateCode,
        string? locationCode)
    {
        if (ApplicationProgressLegCodes.TryParseMinistryLegFromLocationCode(locationCode, out var legFromLocation))
            return GetMinistryShortNameForLeg(application, legFromLocation);

        if (ApplicationProgressLegCodes.TryParseMinistryLegFromStateCode(stateCode, out var legFromState))
            return GetMinistryShortNameForLeg(application, legFromState);

        return null;
    }

    private static class LegCommitRedirectScope
    {
        private static readonly AsyncLocal<int> Depth = new();

        internal static bool IsActive => Depth.Value > 0;

        internal static IDisposable? TryEnter()
        {
            if (Depth.Value > 0)
                return null;

            Depth.Value++;
            return new PopScope();
        }

        private sealed class PopScope : IDisposable
        {
            public void Dispose() => Depth.Value--;
        }
    }

    private static class PrepareLegsForCommitScope
    {
        private static readonly AsyncLocal<HashSet<IObjectSpace>?> ActiveSpaces = new();

        internal static IDisposable? TryEnter(IObjectSpace objectSpace)
        {
            var spaces = ActiveSpaces.Value ??= new HashSet<IObjectSpace>();
            if (!spaces.Add(objectSpace))
                return null;

            return new PopScope(objectSpace);
        }

        private sealed class PopScope : IDisposable
        {
            private readonly IObjectSpace _objectSpace;

            internal PopScope(IObjectSpace objectSpace) => _objectSpace = objectSpace;

            public void Dispose()
            {
                var spaces = ActiveSpaces.Value;
                if (spaces == null)
                    return;

                spaces.Remove(_objectSpace);
                if (spaces.Count == 0)
                    ActiveSpaces.Value = null;
            }
        }
    }
}
