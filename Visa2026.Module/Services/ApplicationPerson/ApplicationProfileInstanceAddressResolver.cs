using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Shared-catalog address resolution for Linked Records overrides. Case summary Address 1/2 are free text (not AoR FKs).
/// </summary>
public static class ApplicationProfileInstanceAddressResolver
{
    public const int Slot1 = 1;
    public const int Slot2 = 2;

    public static AddressOfResidence? GetInstanceSummaryAddress(ApplicationProfileInstance? instance, int slot)
    {
        if (instance == null)
            return null;
        #pragma warning disable CS0618
        return slot >= Slot2 ? instance.Address2 : instance.Address1;
#pragma warning restore CS0618
    }

    public static void SetInstanceSummaryAddress(
        ApplicationProfileInstance instance,
        int slot,
        AddressOfResidence? address)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (address != null)
            AddressOfResidenceCatalogHelper.ClearOwnership(address);
        if (slot >= Slot2)
        {
            // Case summary Address 2 catalog FK is obsolete (People Address2Text notes).
#pragma warning disable CS0618
            instance.Address2 = null;
#pragma warning restore CS0618
            return;
        }

        // Case summary Address 1 catalog FK is obsolete (Address1DefaultText free text).
#pragma warning disable CS0618
        instance.Address1 = null;
#pragma warning restore CS0618
        _ = address;
    }

    /// <summary>
    /// Resolve address for a roster person slot: override ResolvedLink when present and different
    /// from the case summary FK; otherwise case summary Address1/Address2.
    /// </summary>
    public static AddressOfResidence? ResolveAddressSlot(
        ApplicationProfileInstance? instance,
        Person? person,
        int slot,
        IObjectSpace? objectSpace = null)
    {
        if (instance == null || slot < Slot1)
            return null;

        var normalizedSlot = slot >= Slot2 ? Slot2 : Slot1;
        var summary = GetInstanceSummaryAddress(instance, normalizedSlot);

        if (person != null && person.ID != Guid.Empty)
        {
            var overrideId = FindOverrideLinkedObjectId(instance, person, normalizedSlot, objectSpace);
            if (overrideId is Guid id && id != Guid.Empty && id != summary?.ID)
            {
                if (objectSpace != null)
                    return objectSpace.GetObjectByKey<AddressOfResidence>(id) ?? summary;
                return instance.AddressesOfResidence?.FirstOrDefault(a => a != null && a.ID == id)
                    ?? summary;
            }
        }

        return summary;
    }

    public static Guid? FindOverrideLinkedObjectId(
        ApplicationProfileInstance instance,
        Person person,
        int slot,
        IObjectSpace? objectSpace = null)
    {
        var personId = person.ID;
        var normalizedSlot = slot >= Slot2 ? Slot2 : Slot1;

        if (objectSpace != null && instance.ID != Guid.Empty)
        {
            return objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
                .Where(link => link.ApplicationProfileInstanceId == instance.ID
                    && link.PersonId == personId
                    && link.LinkKind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                    && link.LinkedObjectId != null
                    && link.LinkedObjectId != Guid.Empty
                    && (link.LinkSlotIndex == normalizedSlot
                        || (normalizedSlot == Slot1 && link.LinkSlotIndex <= 0)))
                .OrderByDescending(link => link.LinkSlotIndex == normalizedSlot)
                .Select(link => link.LinkedObjectId)
                .FirstOrDefault();
        }

        if (instance.PersonResolvedLinks == null)
            return null;

        var match = instance.PersonResolvedLinks
            .Where(link =>
                link.LinkKind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                && link.PersonId == personId
                && link.LinkedObjectId is Guid linked
                && linked != Guid.Empty)
            .OrderByDescending(link =>
            {
                var linkSlot = link.LinkSlotIndex > 0 ? link.LinkSlotIndex : Slot1;
                return linkSlot == normalizedSlot;
            })
            .FirstOrDefault(link =>
            {
                var linkSlot = link.LinkSlotIndex > 0 ? link.LinkSlotIndex : Slot1;
                return linkSlot == normalizedSlot;
            });

        return match?.LinkedObjectId;
    }

    /// <summary>
    /// Return existing summary FK or create a blank shared catalog draft (ownership cleared).
    /// Prefer <see cref="AddressOfResidenceCatalogHelper.TryUpsertFromEdit"/> for officer saves.
    /// </summary>
    public static AddressOfResidence EnsureSummaryAddress(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        int slot)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(instance);

        var normalizedSlot = slot >= Slot2 ? Slot2 : Slot1;
        var existing = GetInstanceSummaryAddress(instance, normalizedSlot);
        if (existing != null)
        {
            AddressOfResidenceCatalogHelper.ClearOwnership(existing);
            return existing;
        }

        var address = objectSpace.CreateObject<AddressOfResidence>();
        AddressOfResidenceCatalogHelper.ClearOwnership(address);
        SetInstanceSummaryAddress(instance, normalizedSlot, address);
        return address;
    }

    /// <summary>
    /// Create a shared catalog draft for Linked Records edit (no case ownership).
    /// </summary>
    public static AddressOfResidence CreateOverrideAddress(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        Person person)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(instance);
        ArgumentNullException.ThrowIfNull(person);

        var address = objectSpace.CreateObject<AddressOfResidence>();
        AddressOfResidenceCatalogHelper.ClearOwnership(address);
        return address;
    }
}