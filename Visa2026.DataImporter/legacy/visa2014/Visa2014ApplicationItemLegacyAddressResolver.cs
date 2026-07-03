using DevExpress.ExpressApp;
using Bo = Visa2026.Module.BusinessObjects;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014ApplicationItemLegacyAddressResolver
{
    internal static bool TryEnsureLegacyAddressMapped(
        IObjectSpace objectSpace,
        Visa2014ODataLookupResolver resolver,
        string legacyConnectionString,
        IReadOnlyList<string> lookupTranslationPaths,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        Dictionary<Guid, Guid> addressIdMap,
        Guid legacyAddressKey,
        Visa2014ApplicationItemRawRow raw,
        Bo.Person linePerson,
        bool dryRun,
        out Bo.AddressOfResidence? address)
    {
        address = null;

        if (addressIdMap.TryGetValue(legacyAddressKey, out var mappedId))
        {
            address = objectSpace.GetObjectByKey<Bo.AddressOfResidence>(mappedId);
            return address != null;
        }

        if (TryResolveSponsorCanonicalAddress(
                objectSpace,
                personIdMap,
                addressIdMap,
                legacyAddressKey,
                raw,
                out address))
        {
            return address != null;
        }

        if (!Visa2014AddressOfResidenceLegacyLoader.TryBuildImportRowForLegacyAddressKey(
                legacyConnectionString,
                lookupTranslationPaths,
                legacyAddressKey,
                raw,
                out var importRow)
            || importRow == null)
        {
            return false;
        }

        if (!TryResolveOwnerPerson(
                objectSpace,
                personIdMap,
                importRow,
                raw,
                linePerson,
                out var ownerPerson))
        {
            return false;
        }

        if (TryFindExistingOnPerson(ownerPerson, importRow, out var existing))
        {
            address = objectSpace.GetObject(existing);
            addressIdMap[legacyAddressKey] = address!.ID;
            return true;
        }

        if (dryRun)
            return true;

        if (!Visa2014AddressOfResidenceImportApplier.TryCreateOnObjectSpace(
                objectSpace,
                ownerPerson,
                importRow,
                resolver,
                out var created)
            || created == null)
        {
            return false;
        }

        address = created;
        addressIdMap[legacyAddressKey] = created.ID;
        return true;
    }

    private static bool TryResolveSponsorCanonicalAddress(
        IObjectSpace objectSpace,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        Dictionary<Guid, Guid> addressIdMap,
        Guid legacyAddressKey,
        Visa2014ApplicationItemRawRow raw,
        out Bo.AddressOfResidence? address)
    {
        address = null;
        var canonicalPersonLegacyOid = raw.ForFamilyMember ? raw.LegacyEmployeeOid : ResolveLinePersonLegacyOid(raw);
        if (!canonicalPersonLegacyOid.HasValue)
            return false;

        if (legacyAddressKey != Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(canonicalPersonLegacyOid.Value))
            return false;

        if (!personIdMap.TryGetValue(canonicalPersonLegacyOid.Value, out var ownerPersonId))
            return false;

        var ownerPerson = objectSpace.GetObjectByKey<Bo.Person>(ownerPersonId);
        if (ownerPerson == null)
            return false;

        var current = Bo.PersonCurrentItems.GetCurrentAddressOfResidence(ownerPerson);
        if (current == null)
            return false;

        address = objectSpace.GetObject(current);
        if (address == null)
            return false;

        addressIdMap[legacyAddressKey] = address.ID;
        return true;
    }

    private static bool TryResolveOwnerPerson(
        IObjectSpace objectSpace,
        IReadOnlyDictionary<Guid, Guid> personIdMap,
        IReadOnlyDictionary<string, object?> importRow,
        Visa2014ApplicationItemRawRow raw,
        Bo.Person linePerson,
        out Bo.Person ownerPerson)
    {
        ownerPerson = null!;
        if (TryParseLegacyGuid(importRow, "Person", out var legacyPersonOid)
            && personIdMap.TryGetValue(legacyPersonOid, out var ownerPersonId))
        {
            var resolved = objectSpace.GetObjectByKey<Bo.Person>(ownerPersonId);
            if (resolved != null)
            {
                ownerPerson = resolved;
                return true;
            }
        }

        ownerPerson = objectSpace.GetObject(linePerson);
        return true;
    }

    private static bool TryFindExistingOnPerson(
        Bo.Person person,
        IReadOnlyDictionary<string, object?> importRow,
        out Bo.AddressOfResidence? existing)
    {
        existing = null;
        if (person.AddressesOfResidence == null)
            return false;

        var typeText = importRow.GetValueOrDefault("Type") as string;
        if (!Enum.TryParse<Bo.ResidenceType>(typeText, ignoreCase: true, out var residenceType))
            return false;

        foreach (var candidate in person.AddressesOfResidence.Where(a => a != null))
        {
            if (candidate.Type != residenceType)
                continue;

            switch (residenceType)
            {
                case Bo.ResidenceType.PrivateHouse:
                    var fullAddress = importRow.GetValueOrDefault("FullAddress") as string;
                    if (!string.IsNullOrWhiteSpace(fullAddress)
                        && string.Equals(candidate.FullAddress?.Trim(), fullAddress.Trim(), StringComparison.Ordinal))
                    {
                        existing = candidate;
                        return true;
                    }

                    break;

                case Bo.ResidenceType.Lodging:
                    var lodgingName = importRow.GetValueOrDefault("Lodging") as string;
                    if (!string.IsNullOrWhiteSpace(lodgingName)
                        && string.Equals(candidate.Lodging?.FullAddress?.Trim(), lodgingName.Trim(), StringComparison.Ordinal))
                    {
                        existing = candidate;
                        return true;
                    }

                    break;

                case Bo.ResidenceType.Hotel:
                    var hotelName = importRow.GetValueOrDefault("Hotel") as string;
                    if (!string.IsNullOrWhiteSpace(hotelName)
                        && string.Equals(candidate.Hotel?.Name?.Trim(), hotelName.Trim(), StringComparison.Ordinal))
                    {
                        existing = candidate;
                        return true;
                    }

                    break;
            }
        }

        return false;
    }

    private static bool TryParseLegacyGuid(IReadOnlyDictionary<string, object?> row, string field, out Guid legacyOid)
    {
        legacyOid = Guid.Empty;
        var text = row.GetValueOrDefault(field) as string;
        return !string.IsNullOrWhiteSpace(text) && Guid.TryParse(text, out legacyOid);
    }

    private static Guid? ResolveLinePersonLegacyOid(Visa2014ApplicationItemRawRow raw) =>
        raw.ForEmployee ? raw.LegacyEmployeeOid
        : raw.ForFamilyMember ? raw.LegacyFamilyMemberOid
        : null;
}
