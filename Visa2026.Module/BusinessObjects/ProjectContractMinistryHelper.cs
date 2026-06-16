using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.Base;
using Visa2026.Module.Localization;

namespace Visa2026.Module.BusinessObjects;

public static class ProjectContractMinistryHelper
{
    /// <summary>True while a leg popup commit is being redirected to a parent object-space commit.</summary>
    internal static bool IsLegCommitRedirectInProgress => LegCommitRedirectScope.IsActive;

    /// <summary>Skip nested popup sessions during redirect — the parent session prepares legs once.</summary>
    internal static bool ShouldPrepareLegsOnCommit(IObjectSpace objectSpace) =>
        !(IsLegCommitRedirectInProgress && ObjectSpaceHelper.IsNestedObjectSpace(objectSpace));

    /// <summary>
    /// True when a new parent contract would not be inserted in the same commit batch as its leg.
    /// </summary>
    internal static bool WouldOrphanLegForeignKey(
        bool isParentNewObject,
        Guid parentId,
        IEnumerable<Guid> contractIdsInCommitBatch) =>
        isParentNewObject
        && parentId != Guid.Empty
        && !contractIdsInCommitBatch.Contains(parentId);

    /// <summary>
    /// Ensures aggregated legs reference the parent contract before EF commit.
    /// Nested Blazor list rows often populate <see cref="ProjectContract.MinistryLegs"/> without back-references.
    /// </summary>
    public static void PrepareLegsForCommit(IObjectSpace objectSpace)
    {
        if (!ShouldPrepareLegsOnCommit(objectSpace))
            return;

        using var preparing = PrepareLegsForCommitScope.TryEnter(objectSpace);
        if (preparing == null)
            return;

        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        var contractsToSave = CollectContractsInCommitBatch(objectSpace, rootObjectSpace);
        var legsToSave = CollectLegsInCommitBatch(objectSpace, rootObjectSpace, contractsToSave);

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
            WireOrphanLegsInRoot(objectSpace, rootObjectSpace, legsToSave);

        foreach (var contract in contractsToSave)
        {
            var targetSpace = ResolveObjectSpaceFor(contract, objectSpace, rootObjectSpace);
            var contractInTarget = ResolveContractInObjectSpace(targetSpace, contract) ?? contract;
            RehomeContractLegsInObjectSpace(targetSpace, contractInTarget);
            WireMinistryLegs(contractInTarget);
            targetSpace.SetModified(contractInTarget);
        }

        foreach (var leg in legsToSave)
        {
            PrepareLegForSave(objectSpace, rootObjectSpace, leg);
            EnsureLegHasParentNavigation(objectSpace, rootObjectSpace, leg);
        }

        foreach (var contract in CollectParentContracts(objectSpace, rootObjectSpace, legsToSave))
            ResolveObjectSpaceFor(contract, objectSpace, rootObjectSpace).SetModified(contract);
    }

    /// <summary>
    /// When a leg popup commits, ensure the unsaved parent contract is in the same batch.
    /// Redirects to the parent session when the popup uses a different object space.
    /// </summary>
    public static bool TryFinalizeLegCommit(IObjectSpace objectSpace, CancelEventArgs e)
    {
        if (IsLegCommitRedirectInProgress)
            return true;

        PrepareLegsForCommit(objectSpace);

        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        var contractsToSave = CollectContractsInCommitBatch(objectSpace, rootObjectSpace);
        var legsToSave = CollectLegsInCommitBatch(objectSpace, rootObjectSpace, contractsToSave);
        if (legsToSave.Count == 0)
            return true;

        foreach (var leg in legsToSave)
            EnsureLegParentInCommitBatch(objectSpace, leg);

        if (legsToSave.Any(leg => IsLegForeignKeyOrphaned(objectSpace, leg))
            || legsToSave.Any(leg => leg.ProjectContract == null))
        {
            e.Cancel = true;
            throw new UserFriendlyException(VisaUiMessages.Get("ProjectContract.SaveBeforeMinistryLeg"));
        }

        return true;
    }

    /// <summary>
    /// Leg popup Save on a new contract: commit the root session (parent + leg) instead of the popup session.
    /// </summary>
    public static bool TryCommitParentWithLeg(
        IObjectSpace legObjectSpace,
        ProjectContractMinistryLeg leg,
        CancelEventArgs e)
    {
        PrepareLegsForCommit(legObjectSpace);
        TryAttachLegToParent(legObjectSpace, leg);

        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(legObjectSpace) ?? legObjectSpace;
        var parent = leg.ProjectContract
            ?? FindParentContract(legObjectSpace, rootObjectSpace, leg);
        if (parent == null)
            return false;

        var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(legObjectSpace, parent);
        var parentInSpace = ResolveContractInObjectSpace(parentSpace, parent);
        if (parentInSpace == null || !parentSpace.IsNewObject(parentInSpace))
            return false;

        var legInSpace = EnsureLegInObjectSpace(parentSpace, parentInSpace, leg);
        PrepareLegsForCommit(parentSpace);
        parentSpace.SetModified(parentInSpace);

        if (TryRedirectCommitToParentSpace(parentSpace, legObjectSpace, e))
            return true;

        return legInSpace.ProjectContract != null && !IsLegForeignKeyOrphaned(parentSpace, legInSpace);
    }

    /// <summary>After frame-based parent resolution, verify the leg can commit without FK orphan.</summary>
    public static bool CanCommitLeg(IObjectSpace committingObjectSpace, ProjectContractMinistryLeg leg)
    {
        PrepareLegsForCommit(committingObjectSpace);
        EnsureLegParentInCommitBatch(committingObjectSpace, leg);
        return leg.ProjectContract != null && !IsLegForeignKeyOrphaned(committingObjectSpace, leg);
    }

    private static void EnsureLegParentInCommitBatch(
        IObjectSpace committingObjectSpace,
        ProjectContractMinistryLeg leg)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(committingObjectSpace) ?? committingObjectSpace;
        TryAttachLegToParent(committingObjectSpace, leg);

        if (IsLegForeignKeyOrphaned(committingObjectSpace, leg)
            && !ReferenceEquals(committingObjectSpace, rootObjectSpace))
        {
            WireOrphanLegsInRoot(committingObjectSpace, rootObjectSpace, [leg]);
        }
    }

    private static bool IsLegForeignKeyOrphaned(IObjectSpace committingObjectSpace, ProjectContractMinistryLeg leg)
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

    public static void WireMinistryLegs(ProjectContract contract)
    {
        if (contract.MinistryLegs == null)
            return;

        foreach (var leg in contract.MinistryLegs)
        {
            leg.ProjectContract = contract;
            leg.SyncForeignKeys();
        }
    }

    public static void PrepareLegForSave(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        ProjectContractMinistryLeg leg)
    {
        if (leg.ProjectContract == null)
        {
            EnsureLegParentContract(
                objectSpace,
                rootObjectSpace,
                leg,
                objectSpace.GetObjectsToSave(false).OfType<ProjectContract>().ToList());
        }

        leg.SyncForeignKeys();
    }

    /// <summary>Links a leg to its parent contract in the given object space (popup save / nested list).</summary>
    public static bool TryAttachLegToParent(IObjectSpace objectSpace, ProjectContractMinistryLeg leg)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;

        if (leg.ProjectContract != null)
        {
            var parentSpace = ResolveObjectSpaceFor(leg.ProjectContract, objectSpace, rootObjectSpace);
            var parentInSpace = ResolveContractInObjectSpace(parentSpace, leg.ProjectContract) ?? leg.ProjectContract;
            EnsureLegInObjectSpace(parentSpace, parentInSpace, leg);
            parentSpace.SetModified(parentInSpace);
            return true;
        }

        var parent = FindParentContract(objectSpace, rootObjectSpace, leg);
        if (parent == null)
            return false;

        var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
        var legInTarget = EnsureLegInObjectSpace(targetSpace, parent, leg);
        return true;
    }

    public static void AttachLegToContract(
        ProjectContract contract,
        ProjectContractMinistryLeg leg,
        IObjectSpace objectSpace)
    {
        leg.ProjectContract = contract;
        if (contract.MinistryLegs != null && !contract.MinistryLegs.Contains(leg))
            contract.MinistryLegs.Add(leg);

        leg.SyncForeignKeys();
        objectSpace.SetModified(contract);
    }

    internal static bool NeedsParentInCommitBatch(
        IObjectSpace objectSpace,
        ProjectContractMinistryLeg leg,
        out ProjectContract? parent)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        parent = leg.ProjectContract ?? FindParentContract(objectSpace, rootObjectSpace, leg);

        if (parent == null)
            return false;

        var parentSpace = ObjectSpaceHelper.ResolveObjectSpace(objectSpace, parent);
        var parentInSpace = ResolveContractInObjectSpace(parentSpace, parent) ?? parent;
        var contractIds = CollectContractIdsInCommitBatch(parentSpace);
        return WouldOrphanLegForeignKey(
            parentSpace.IsNewObject(parentInSpace),
            parentInSpace.ID,
            contractIds);
    }

    private static List<ProjectContract> CollectContractsInCommitBatch(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace)
    {
        var contracts = objectSpace.GetObjectsToSave(false).OfType<ProjectContract>()
            .Concat(objectSpace.GetObjectsToSave(true).OfType<ProjectContract>())
            .Concat(objectSpace.ModifiedObjects.OfType<ProjectContract>())
            .ToList();

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
        {
            contracts = contracts
                .Concat(rootObjectSpace.GetObjectsToSave(false).OfType<ProjectContract>())
                .Concat(rootObjectSpace.GetObjectsToSave(true).OfType<ProjectContract>())
                .Concat(rootObjectSpace.ModifiedObjects.OfType<ProjectContract>())
                .ToList();
        }

        return contracts.Distinct().ToList();
    }

    private static List<ProjectContractMinistryLeg> CollectLegsInCommitBatch(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        IReadOnlyList<ProjectContract> contractsToSave)
    {
        var legs = objectSpace.GetObjectsToSave(false).OfType<ProjectContractMinistryLeg>()
            .Concat(objectSpace.GetObjectsToSave(true).OfType<ProjectContractMinistryLeg>())
            .ToList();

        foreach (var contract in contractsToSave)
        {
            if (contract.MinistryLegs == null)
                continue;

            foreach (var leg in contract.MinistryLegs)
            {
                if (!legs.Contains(leg))
                    legs.Add(leg);
            }
        }

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
        {
            legs = legs
                .Concat(rootObjectSpace.GetObjectsToSave(false).OfType<ProjectContractMinistryLeg>())
                .Concat(rootObjectSpace.GetObjectsToSave(true).OfType<ProjectContractMinistryLeg>())
                .ToList();
        }

        return legs.Distinct().ToList();
    }

    private static IEnumerable<Guid> CollectContractIdsInCommitBatch(IObjectSpace objectSpace)
    {
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        return CollectContractsInCommitBatch(objectSpace, rootObjectSpace).Select(c => c.ID).Distinct();
    }

    private static void RehomeContractLegsInObjectSpace(IObjectSpace targetSpace, ProjectContract contract)
    {
        if (contract.MinistryLegs == null)
            return;

        foreach (var leg in contract.MinistryLegs.ToList())
            EnsureLegInObjectSpace(targetSpace, contract, leg);
    }

    /// <summary>
    /// Ensures the leg instance tracked for commit lives in <paramref name="targetSpace"/>
    /// and is linked to <paramref name="contractInTarget"/>.
    /// </summary>
    internal static ProjectContractMinistryLeg EnsureLegInObjectSpace(
        IObjectSpace targetSpace,
        ProjectContract contractInTarget,
        ProjectContractMinistryLeg sourceLeg)
    {
        var resolved = ResolveLegInObjectSpace(targetSpace, sourceLeg, contractInTarget);
        if (resolved != null)
        {
            AttachLegToContract(contractInTarget, resolved, targetSpace);
            return resolved;
        }

        var sourceSpace = ObjectSpaceHelper.Get(sourceLeg);
        if (sourceSpace != null && ReferenceEquals(sourceSpace, targetSpace))
        {
            AttachLegToContract(contractInTarget, sourceLeg, targetSpace);
            return sourceLeg;
        }

        var copy = targetSpace.CreateObject<ProjectContractMinistryLeg>();
        copy.Sequence = sourceLeg.Sequence;
        copy.MaxDaysInReview = sourceLeg.MaxDaysInReview;
        copy.WarningDaysBeforeMax = sourceLeg.WarningDaysBeforeMax;
        if (sourceLeg.ApprovingMinistry != null)
            copy.ApprovingMinistry = targetSpace.GetObject(sourceLeg.ApprovingMinistry);
        AttachLegToContract(contractInTarget, copy, targetSpace);

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
        ProjectContractMinistryLeg leg)
    {
        if (leg.ProjectContract != null)
        {
            leg.SyncForeignKeys();
            return;
        }

        TryAttachLegToParent(objectSpace, leg);
        if (leg.ProjectContract != null)
        {
            leg.SyncForeignKeys();
            return;
        }

        var parent = FindParentContract(objectSpace, rootObjectSpace, leg);
        if (parent == null)
            return;

        var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
        var parentInTarget = ResolveContractInObjectSpace(targetSpace, parent) ?? parent;
        EnsureLegInObjectSpace(targetSpace, parentInTarget, leg);
    }

    private static void WireOrphanLegsInRoot(
        IObjectSpace committingObjectSpace,
        IObjectSpace rootObjectSpace,
        IReadOnlyList<ProjectContractMinistryLeg> legsToSave)
    {
        foreach (var leg in legsToSave)
        {
            if (!NeedsParentInCommitBatch(committingObjectSpace, leg, out var parent) || parent == null)
                continue;

            var rootParent = ResolveContractInObjectSpace(rootObjectSpace, parent);
            if (rootParent == null)
                continue;

            var rootLeg = EnsureLegInObjectSpace(rootObjectSpace, rootParent, leg);
        }
    }

    private static IEnumerable<ProjectContract> CollectParentContracts(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        IReadOnlyList<ProjectContractMinistryLeg> legsToSave)
    {
        var parents = new HashSet<ProjectContract>();
        foreach (var leg in legsToSave)
        {
            if (leg.ProjectContract != null)
                parents.Add(leg.ProjectContract);
        }

        foreach (var leg in legsToSave)
        {
            if (leg.ProjectContract != null)
                continue;

            var parent = FindParentContract(objectSpace, rootObjectSpace, leg);
            if (parent == null)
                continue;

            var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
            var parentInTarget = ResolveContractInObjectSpace(targetSpace, parent) ?? parent;
            EnsureLegInObjectSpace(targetSpace, parentInTarget, leg);
            parents.Add(parentInTarget);
        }

        return parents;
    }

    private static void EnsureLegParentContract(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        ProjectContractMinistryLeg leg,
        IReadOnlyList<ProjectContract> contractsToSave)
    {
        var parent = FindParentContract(objectSpace, rootObjectSpace, leg, contractsToSave);
        if (parent == null)
            return;

        var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
        var parentInTarget = ResolveContractInObjectSpace(targetSpace, parent) ?? parent;
        EnsureLegInObjectSpace(targetSpace, parentInTarget, leg);
    }

    private static ProjectContract? FindParentContract(
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace,
        ProjectContractMinistryLeg leg,
        IReadOnlyList<ProjectContract>? contractsToSave = null)
    {
        var parent = FindParentContractInObjectSpace(objectSpace, leg, contractsToSave);
        if (parent != null)
            return parent;

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
        {
            parent = FindParentContractInObjectSpace(
                rootObjectSpace,
                leg,
                CollectContractsInCommitBatch(rootObjectSpace, rootObjectSpace));
        }

        return parent;
    }

    private static ProjectContract? FindParentContractInObjectSpace(
        IObjectSpace objectSpace,
        ProjectContractMinistryLeg leg,
        IReadOnlyList<ProjectContract>? contractsToSave = null)
    {
        contractsToSave ??= objectSpace.GetObjectsToSave(false).OfType<ProjectContract>().ToList();

        var parent = contractsToSave.FirstOrDefault(c => c.MinistryLegs?.Contains(leg) == true);
        if (parent != null)
            return parent;

        foreach (var contract in objectSpace.ModifiedObjects.OfType<ProjectContract>())
        {
            if (contract.MinistryLegs?.Contains(leg) == true)
                return contract;
        }

        var modifiedContracts = objectSpace.ModifiedObjects.OfType<ProjectContract>().ToList();
        if (modifiedContracts.Count == 1)
            return modifiedContracts[0];

        var contractsBeingEdited = objectSpace.GetObjectsToSave(false).OfType<ProjectContract>().ToList();
        if (contractsBeingEdited.Count == 1)
            return contractsBeingEdited[0];

        return null;
    }

    private static ProjectContract? ResolveContractInObjectSpace(
        IObjectSpace targetObjectSpace,
        ProjectContract source)
    {
        if (targetObjectSpace.IsNewObject(source))
            return targetObjectSpace.GetObject(source) as ProjectContract ?? source;

        return targetObjectSpace.GetObjectByKey<ProjectContract>(source.ID);
    }

    private static ProjectContractMinistryLeg? ResolveLegInObjectSpace(
        IObjectSpace targetObjectSpace,
        ProjectContractMinistryLeg source,
        ProjectContract? parent)
    {
        var leg = targetObjectSpace.GetObject(source) as ProjectContractMinistryLeg;
        if (leg != null)
            return leg;

        return parent?.MinistryLegs?.FirstOrDefault(l => l.ID == source.ID);
    }

    private static IObjectSpace ResolveObjectSpaceFor(
        ProjectContract contract,
        IObjectSpace objectSpace,
        IObjectSpace rootObjectSpace) =>
        ObjectSpaceHelper.Get(contract) ?? rootObjectSpace ?? objectSpace;

    public static int GetLegCount(ProjectContract? contract) =>
        contract?.MinistryLegs?.Count(l => l.ApprovingMinistry != null) ?? 0;

    public static bool HasConfiguredLegs(ProjectContract? contract) =>
        GetLegCount(contract) > 0;

    public static bool TryValidateLegSla(ProjectContract? contract, out string? errorMessage)
    {
        errorMessage = null;
        if (contract == null || !contract.IsActive)
            return true;

        var legs = contract.MinistryLegs?
            .Where(l => l.ApprovingMinistry != null)
            .OrderBy(l => l.Sequence)
            .ToList() ?? [];

        if (legs.Count == 0)
            return true;

        foreach (var leg in legs)
        {
            if (leg.MaxDaysInReview is not > 0)
            {
                errorMessage = VisaUiMessages.Format(
                    "ProjectContract.MinistryLegMaxDaysRequired",
                    leg.Sequence ?? 0);
                return false;
            }

            if (leg.WarningDaysBeforeMax is > 0 && leg.WarningDaysBeforeMax >= leg.MaxDaysInReview)
            {
                errorMessage = VisaUiMessages.Format(
                    "ProjectContract.MinistryLegWarningDaysInvalid",
                    leg.Sequence ?? 0);
                return false;
            }
        }

        return true;
    }

    public static void ApplySnapshot(IObjectSpace objectSpace, Application application, ProjectContract? contract)
    {
        if (application.ApprovalLegSnapshots == null)
            return;

        // Do not call ObservableCollection.Clear() — EF Core change tracking rejects the Reset notification.
        foreach (var existing in application.ApprovalLegSnapshots.ToList())
            objectSpace.Delete(existing);

        if (contract?.MinistryLegs == null)
            return;

        foreach (var leg in contract.MinistryLegs
                     .Where(l => l.ApprovingMinistry != null)
                     .OrderBy(l => l.Sequence))
        {
            var snapshot = objectSpace.CreateObject<ApplicationApprovalLegSnapshot>();
            snapshot.Application = application;
            snapshot.Sequence = leg.Sequence;
            snapshot.ApprovingMinistryId = leg.ApprovingMinistry.ID;
            snapshot.MinistryShortName = leg.ApprovingMinistry.ShortNameTm ?? leg.ApprovingMinistry.NameTm ?? string.Empty;
            snapshot.MinistryNameTm = leg.ApprovingMinistry.NameTm ?? string.Empty;
            snapshot.MaxDaysInReview = leg.MaxDaysInReview;
            snapshot.WarningDaysBeforeMax = leg.WarningDaysBeforeMax;
            application.ApprovalLegSnapshots.Add(snapshot);
        }
    }

    public static bool IsContractReferencedByApplications(ProjectContract contract, IObjectSpace objectSpace) =>
        objectSpace.GetObjectsQuery<Application>()
            .Any(a => a.ProjectContract != null && a.ProjectContract.ID == contract.ID);

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
