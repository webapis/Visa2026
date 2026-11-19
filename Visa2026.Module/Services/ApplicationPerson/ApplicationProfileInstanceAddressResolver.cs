using System;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ApplicationPersonRoster;

/// <summary>
/// Case-owned address resolution: Linked Records override ResolvedLink, else instance Address1/Address2.
/// </summary>
public static class ApplicationProfileInstanceAddressResolver
{
    public const int Slot1 = 1;
    public const int Slot2 = 2;

    public static AddressOfResidence? GetInstanceSummaryAddress(ApplicationProfileInstance? instance, int slot)
    {
        if (instance == null)
            return null;
        return slot >= Slot2 ? instance.Address2 : instance.Address1;
    }

    public static void SetInstanceSummaryAddress(
        ApplicationProfileInstance instance,
        int slot,
        AddressOfResidence? address)
    {
        ArgumentNullException.ThrowIfNull(instance);
        if (slot >= Slot2)
            instance.Address2 = address;
        else
            instance.Address1 = address;
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
    /// Upsert a case-summary AddressOfResidence (RosterPerson null) and set Address1 or Address2.
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
            existing.OwningApplicationProfileInstance = instance;
            existing.RosterPerson = null;
            return existing;
        }

        var address = objectSpace.CreateObject<AddressOfResidence>();
        address.OwningApplicationProfileInstance = instance;
        address.RosterPerson = null;
#pragma warning disable CS0618
        address.Person = null;
#pragma warning restore CS0618

        SetInstanceSummaryAddress(instance, normalizedSlot, address);
        return address;
    }

    /// <summary>
    /// Create a Linked Records override row owned by the case for this roster person.
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
        address.OwningApplicationProfileInstance = instance;
        address.RosterPerson = person;
#pragma warning disable CS0618
        address.Person = null;
#pragma warning restore CS0618
        return address;
    }
}