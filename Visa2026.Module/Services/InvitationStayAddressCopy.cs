using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.MigrationImport;

namespace Visa2026.Module.Services;

/// <summary>
/// Copies a complete case-summary invitation stay onto a person's
/// <see cref="AddressOfResidence"/>. A later change to the case summary does not
/// rewrite an address already copied; the officer uses the tick to add a new one and relink.
/// </summary>
public static class InvitationStayAddressCopy
{
    public static bool IsComplete(InvitationAddress? stay)
    {
        if (stay?.Region == null || stay.Region.ID == Guid.Empty)
            return false;
        if (stay.City == null || stay.City.ID == Guid.Empty)
            return false;
        if (stay.Type is not ResidenceType type)
            return false;

        return type switch
        {
            ResidenceType.Lodging => HasId(stay.Lodging),
            ResidenceType.Hotel => HasId(stay.Hotel),
            ResidenceType.Hospital => HasId(stay.Hospital),
            ResidenceType.Other => HasId(stay.OtherSite),
            ResidenceType.PrivateHouse => !string.IsNullOrWhiteSpace(stay.PrivateHouseAddress),
            _ => false,
        };
    }

    public static bool Matches(AddressOfResidence? address, InvitationAddress? stay)
    {
        if (address == null || !IsComplete(stay) || stay == null)
            return false;
        if (address.Region?.ID != stay.Region.ID || address.City?.ID != stay.City.ID || address.Type != stay.Type)
            return false;

        return stay.Type switch
        {
            ResidenceType.Lodging => address.Lodging?.ID == stay.Lodging?.ID,
            ResidenceType.Hotel => address.Hotel?.ID == stay.Hotel?.ID,
            ResidenceType.Hospital => address.Hospital?.ID == stay.Hospital?.ID,
            ResidenceType.Other => address.OtherSite?.ID == stay.OtherSite?.ID,
            ResidenceType.PrivateHouse => string.Equals(
                address.FullAddress?.Trim(),
                stay.PrivateHouseAddress?.Trim(),
                StringComparison.Ordinal),
            _ => false,
        };
    }

    public static bool OfferTick(InvitationAddress? stay, AddressOfResidence? address) =>
        IsComplete(stay) && address != null && !Matches(address, stay);

    /// <summary>
    /// When the stay is complete, people on this case who have no address of residence
    /// get one created from the stay and linked. People who already have an address are left alone.
    /// </summary>
    public static void EnsureForPeopleWithoutAddress(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        if (!CanCopy(objectSpace, application, out var stay))
            return;

        foreach (var person in application.People.Where(p => p != null && p.ID != Guid.Empty))
        {
            if (PersonHasAddress(objectSpace, person.ID))
                continue;

            var created = CreateFromStay(objectSpace, person, stay!);
            if (created == null || created.ID == Guid.Empty)
                continue;

            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace,
                application,
                person,
                ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                created.ID);
        }
    }

    /// <summary>
    /// Join accepts the Case summary stay. Creates a new address and links it unless the
    /// person's newest address already matches that stay.
    /// </summary>
    public static bool AcceptOnJoin(IObjectSpace objectSpace, ApplicationProfileInstance application, Person person)
    {
        if (!CanCopy(objectSpace, application, out var stay) || person == null || person.ID == Guid.Empty)
            return false;

        var last = objectSpace.GetObjectsQuery<AddressOfResidence>()
            .Where(address => address.Person != null && address.Person.ID == person.ID)
            .OrderByDescending(address => address.ID)
            .FirstOrDefault();
        if (!ShouldInsertFromStay(last, stay))
            return false;

        var created = CreateFromStay(objectSpace, person, stay!);
        if (created == null || created.ID == Guid.Empty)
            return false;

        RetargetAddressLink(objectSpace, application, person, created);
        return true;
    }

    public static bool ShouldInsertFromStay(AddressOfResidence? last, InvitationAddress? stay) =>
        IsComplete(stay) && !Matches(last, stay);

    public static void LinkAddress(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        Guid addressId)
    {
        if (objectSpace == null || application == null || person == null || addressId == Guid.Empty)
            return;

        var address = objectSpace.GetObjectByKey<AddressOfResidence>(addressId);
        if (address == null)
            return;

        RetargetAddressLink(objectSpace, application, person, address);
    }

    private static void RetargetAddressLink(
        IObjectSpace objectSpace,
        ApplicationProfileInstance application,
        Person person,
        AddressOfResidence created)
    {
        var link = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(row => row.ApplicationProfileInstanceId == application.ID
                && row.PersonId == person.ID
                && row.LinkKind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                && row.LinkedObjectId != null
                && row.LinkedObjectId != Guid.Empty)
            .FirstOrDefault();

        _ = application.AddressesOfResidence?.ToList();
        if (link?.LinkedObjectId is not Guid previous || previous == Guid.Empty || previous == created.ID)
        {
            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace,
                application,
                person,
                ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                created.ID);
            return;
        }

        ApplicationProfileInstanceChildMembership.Remove(
            application,
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            previous,
            objectSpace);
        link.LinkedObjectId = created.ID;
        ApplicationProfileInstanceChildMembership.Add(
            objectSpace,
            application,
            ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
            created.ID);
    }

    public static bool TryRelinkFromStay(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid personId,
        Guid currentAddressId,
        out string? error)
    {
        error = null;
        if (objectSpace == null || applicationId == Guid.Empty || personId == Guid.Empty || currentAddressId == Guid.Empty)
        {
            error = "Choose an address on this case.";
            return false;
        }

        var application = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        var person = objectSpace.GetObjectByKey<Person>(personId);
        if (!CanCopy(objectSpace, application, out var stay))
        {
            error = "Fill invitation region, city, address type, and the place on Case summary first.";
            return false;
        }

        if (person == null)
        {
            error = "Person was not found on this case.";
            return false;
        }

        var created = CreateFromStay(objectSpace, person, stay!);
        if (created == null || created.ID == Guid.Empty)
        {
            error = "Could not create an address from the case summary.";
            return false;
        }

        var link = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(row => row.ApplicationProfileInstanceId == applicationId
                && row.PersonId == personId
                && row.LinkKind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                && row.LinkedObjectId == currentAddressId)
            .FirstOrDefault();

        _ = application!.AddressesOfResidence?.ToList();
        if (link == null)
        {
            ApplicationProfileInstancePersonResolver.EnsureResolvedLink(
                objectSpace,
                application!,
                person,
                ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                created.ID);
        }
        else
        {
            ApplicationProfileInstanceChildMembership.Remove(
                application!,
                ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                currentAddressId,
                objectSpace);
            link.LinkedObjectId = created.ID;
            ApplicationProfileInstanceChildMembership.Add(
                objectSpace,
                application!,
                ApplicationProfileInstancePersonLinkKind.AddressOfResidence,
                created.ID);
        }

        return true;
    }

    public static bool TryUpdate(IObjectSpace objectSpace, Guid addressId, AddressOfResidenceEdit edit, out string? error)
    {
        error = null;
        if (objectSpace == null || addressId == Guid.Empty || edit == null)
        {
            error = "Address was not found.";
            return false;
        }

        var address = objectSpace.GetObjectByKey<AddressOfResidence>(addressId);
        if (address == null)
        {
            error = "Address was not found.";
            return false;
        }

        if (edit.Type is not ResidenceType type)
        {
            error = "Choose an address type.";
            return false;
        }

        var region = objectSpace.GetObjectByKey<Region>(edit.RegionId ?? Guid.Empty);
        var city = objectSpace.GetObjectByKey<City>(edit.CityId ?? Guid.Empty);
        if (region == null || city == null || city.Region == null || city.Region.ID != region.ID)
        {
            error = "Choose a region and a city in that region.";
            return false;
        }

        address.DeferPrivateHouseExpiration = false;
        if (address.Region?.ID != region.ID)
            address.Region = region;
        address.City = city;
        if (address.Type != type)
            address.Type = type;

        switch (type)
        {
            case ResidenceType.Lodging:
                var lodging = objectSpace.GetObjectByKey<Lodging>(edit.SiteId ?? Guid.Empty);
                if (lodging == null || lodging.City?.ID != city.ID)
                {
                    error = "Choose a lodging in this city.";
                    return false;
                }
                address.Lodging = lodging;
                break;
            case ResidenceType.Hotel:
                var hotel = objectSpace.GetObjectByKey<Hotel>(edit.SiteId ?? Guid.Empty);
                if (hotel == null || hotel.City?.ID != city.ID)
                {
                    error = "Choose a hotel in this city.";
                    return false;
                }
                address.Hotel = hotel;
                break;
            case ResidenceType.Hospital:
                var hospital = objectSpace.GetObjectByKey<Hospital>(edit.SiteId ?? Guid.Empty);
                if (hospital == null || hospital.City?.ID != city.ID)
                {
                    error = "Choose a hospital in this city.";
                    return false;
                }
                address.Hospital = hospital;
                break;
            case ResidenceType.Other:
                var other = objectSpace.GetObjectByKey<OtherSite>(edit.SiteId ?? Guid.Empty);
                if (other == null || other.City?.ID != city.ID)
                {
                    error = "Choose an other site in this city.";
                    return false;
                }
                address.OtherSite = other;
                break;
            case ResidenceType.PrivateHouse:
                var house = edit.PrivateHouseAddress?.Trim();
                if (string.IsNullOrWhiteSpace(house))
                {
                    error = "Enter the private house address.";
                    return false;
                }
                if (edit.ExpirationDate == null)
                {
                    error = "Enter the private house expiration date.";
                    return false;
                }
                address.FullAddress = house;
                address.ExpirationDate = edit.ExpirationDate;
                break;
        }

        return true;
    }

    public static AddressOfResidence? CreateFromStay(IObjectSpace objectSpace, Person person, InvitationAddress stay)
    {
        if (objectSpace == null || person == null || !IsComplete(stay))
            return null;

        var address = objectSpace.CreateObject<AddressOfResidence>();
        address.Person = person;
        address.Region = stay.Region;
        address.City = stay.City;
        address.Type = stay.Type;
        switch (stay.Type)
        {
            case ResidenceType.Lodging:
                address.Lodging = stay.Lodging;
                break;
            case ResidenceType.Hotel:
                address.Hotel = stay.Hotel;
                break;
            case ResidenceType.Hospital:
                address.Hospital = stay.Hospital;
                break;
            case ResidenceType.Other:
                address.OtherSite = stay.OtherSite;
                break;
            case ResidenceType.PrivateHouse:
                address.FullAddress = stay.PrivateHouseAddress?.Trim();
                address.DeferPrivateHouseExpiration = true;
                break;
        }

        person.AddressesOfResidence ??= new System.Collections.ObjectModel.ObservableCollection<AddressOfResidence>();
        if (!person.AddressesOfResidence.Contains(address))
            person.AddressesOfResidence.Add(address);

        return address;
    }

    private static bool CanCopy(IObjectSpace? objectSpace, ApplicationProfileInstance? application, out InvitationAddress? stay)
    {
        stay = null;
        if (objectSpace == null || application == null)
            return false;
        if (MigrationImportContext.IsDataImport)
            return false;
        if (ApplicationProfileInstancePersonRosterLockHelper.AreResolvedLinksLocked(application))
            return false;
        if (!ApplicationProfileInvitationAddressPolicy.CollectsInvitationStay(application.ApplicationProfile))
            return false;
        if (!ApplicationProfileConfigurationResolver.ShowCurrentAddressOfResidence(application))
            return false;

        stay = application.InvitationAddress;
        return IsComplete(stay);
    }

    private static bool PersonHasAddress(IObjectSpace objectSpace, Guid personId) =>
        objectSpace.GetObjectsQuery<AddressOfResidence>().Any(address => address.Person != null && address.Person.ID == personId);

    private static bool HasId(BaseObject? entity) => entity != null && entity.ID != Guid.Empty;
}

public sealed class AddressOfResidenceEdit
{
    public ResidenceType? Type { get; init; }
    public Guid? RegionId { get; init; }
    public Guid? CityId { get; init; }
    public Guid? SiteId { get; init; }
    public string? PrivateHouseAddress { get; init; }
    public DateTime? ExpirationDate { get; init; }
}
