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
        var rootObjectSpace = ObjectSpaceHelper.GetRootObjectSpace(objectSpace) ?? objectSpace;
        var legsToSave = objectSpace.GetObjectsToSave(false).OfType<ProjectContractMinistryLeg>().ToList();

        if (!ReferenceEquals(objectSpace, rootObjectSpace))
            WireOrphanLegsInRoot(objectSpace, rootObjectSpace, legsToSave);

        var contractsToSave = objectSpace.GetObjectsToSave(false).OfType<ProjectContract>().ToList();
        if (!ReferenceEquals(objectSpace, rootObjectSpace))
        {
            contractsToSave = contractsToSave
                .Concat(rootObjectSpace.GetObjectsToSave(false).OfType<ProjectContract>())
                .Distinct()
                .ToList();
        }

        foreach (var contract in contractsToSave)
            WireMinistryLegs(contract);

        foreach (var leg in legsToSave)
            PrepareLegForSave(objectSpace, rootObjectSpace, leg);

        foreach (var contract in CollectParentContracts(objectSpace, rootObjectSpace, legsToSave))
            ResolveObjectSpaceFor(contract, objectSpace, rootObjectSpace).SetModified(contract);
    }

    /// <summary>
    /// When a leg popup commits, ensure the unsaved parent contract is in the same batch.
    /// Redirects to the parent session when the popup uses a different object space.
    /// </summary>
    public static bool TryFinalizeLegCommit(IObjectSpace objectSpace, CancelEventArgs e)
    {
        PrepareLegsForCommit(objectSpace);

        var legsToSave = objectSpace.GetObjectsToSave(false).OfType<ProjectContractMinistryLeg>().ToList();
        if (legsToSave.Count == 0)
            return true;

        foreach (var leg in legsToSave)
            EnsureLegParentInCommitBatch(objectSpace, leg);

        if (legsToSave.Any(leg => IsLegForeignKeyOrphaned(objectSpace, leg)))
        {
            e.Cancel = true;
            throw new UserFriendlyException(VisaUiMessages.Get("ProjectContract.SaveBeforeMinistryLeg"));
        }

        var parentObjectSpace = ResolveParentObjectSpaceForLegCommit(objectSpace, legsToSave);
        if (parentObjectSpace != null && !ReferenceEquals(parentObjectSpace, objectSpace))
        {
            PrepareLegsForCommit(parentObjectSpace);
            e.Cancel = true;
            parentObjectSpace.CommitChanges();
        }

        return true;
    }

    /// <summary>After frame-based parent resolution, verify the leg can commit without FK orphan.</summary>
    public static bool CanCommitLeg(IObjectSpace committingObjectSpace, ProjectContractMinistryLeg leg)
    {
        PrepareLegsForCommit(committingObjectSpace);
        EnsureLegParentInCommitBatch(committingObjectSpace, leg);
        return !IsLegForeignKeyOrphaned(committingObjectSpace, leg);
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

    private static IObjectSpace? ResolveParentObjectSpaceForLegCommit(
        IObjectSpace committingObjectSpace,
        IReadOnlyList<ProjectContractMinistryLeg> legsToSave)
    {
        foreach (var leg in legsToSave)
        {
            if (!NeedsParentInCommitBatch(committingObjectSpace, leg, out var parent) || parent == null)
                continue;

            var parentObjectSpace = ObjectSpaceHelper.Get(parent)
                ?? ObjectSpaceHelper.GetRootObjectSpace(committingObjectSpace);
            if (parentObjectSpace != null && !ReferenceEquals(parentObjectSpace, committingObjectSpace))
                return parentObjectSpace;
        }

        return null;
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
            leg.SyncForeignKeys();
            if (leg.ProjectContractId != Guid.Empty)
            {
                var parentSpace = ResolveObjectSpaceFor(leg.ProjectContract, objectSpace, rootObjectSpace);
                parentSpace.SetModified(leg.ProjectContract);
                return true;
            }
        }

        var parent = FindParentContract(objectSpace, rootObjectSpace, leg);
        if (parent == null)
            return false;

        var targetSpace = ResolveObjectSpaceFor(parent, objectSpace, rootObjectSpace);
        var legInTarget = ResolveLegInObjectSpace(targetSpace, leg, parent) ?? leg;
        AttachLegToContract(parent, legInTarget, targetSpace);
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

    private static IEnumerable<Guid> CollectContractIdsInCommitBatch(IObjectSpace objectSpace) =>
        objectSpace.GetObjectsToSave(false).OfType<ProjectContract>().Select(c => c.ID)
            .Concat(objectSpace.GetObjectsToSave(true).OfType<ProjectContract>().Select(c => c.ID))
            .Distinct();

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

            var rootLeg = ResolveLegInObjectSpace(rootObjectSpace, leg, rootParent) ?? leg;
            AttachLegToContract(rootParent, rootLeg, rootObjectSpace);
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
            var legInTarget = ResolveLegInObjectSpace(targetSpace, leg, parent) ?? leg;
            AttachLegToContract(parent, legInTarget, targetSpace);
            parents.Add(parent);
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
        var legInTarget = ResolveLegInObjectSpace(targetSpace, leg, parent) ?? leg;
        AttachLegToContract(parent, legInTarget, targetSpace);
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
                rootObjectSpace.GetObjectsToSave(false).OfType<ProjectContract>().ToList());
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
}
