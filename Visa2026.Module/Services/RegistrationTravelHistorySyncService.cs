using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Keeps <see cref="TravelHistory"/> in sync with registration movement data on
/// <see cref="ApplicationItem"/> (four check-in/out application types). Application line is the source of truth.
/// </summary>
public static class RegistrationTravelHistorySyncService
{
    public const string AppRegCheckIn = "App_Reg_Check_In";
    public const string AppRegCheckOut = "App_Reg_Check_Out";
    public const string AppRegCheckInInternal = "App_Reg_Check_In_Internal";
    public const string AppRegCheckOutInternal = "App_Reg_Check_Out_Internal";

    public static bool IsSyncApplicationTypeName(string? applicationTypeName) =>
        applicationTypeName switch
        {
            AppRegCheckIn or AppRegCheckOut or AppRegCheckInInternal or AppRegCheckOutInternal => true,
            _ => false
        };

    public static void SyncFromApplicationItem(ApplicationItem item)
    {
        var objectSpace = ObjectSpaceHelper.Get(item);
        if (objectSpace == null)
            return;

        var applicationTypeName = item.Application?.ApplicationType?.Name;
        if (!IsSyncApplicationTypeName(applicationTypeName))
        {
            DeleteLinkedTravelHistory(item, objectSpace);
            return;
        }

        if (!TryValidateTravelData(item, out var validationMessage))
        {
            Tracing.Tracer.LogText(
                $"RegistrationTravelHistorySync: skipped ApplicationItem {item.ID}: {validationMessage}");
            return;
        }

        var travelHistoryType = GetTravelHistoryType(applicationTypeName!);
        if (travelHistoryType == null)
            return;

        var history = FindOrCreateLinkedTravelHistory(item, objectSpace, travelHistoryType);
        if (history == null)
            return;

        ApplyFieldsFromApplicationItem(item, history);
    }

    public static void DeleteLinkedTravelHistory(ApplicationItem item, IObjectSpace objectSpace)
    {
        var history = FindLinkedTravelHistory(item, objectSpace);
        if (history == null)
            return;

        objectSpace.Delete(history);
    }

    public static void DeleteAllLinkedTravelHistoryForApplication(Application application, IObjectSpace objectSpace)
    {
        if (application?.ApplicationItems == null)
            return;

        foreach (var item in application.ApplicationItems)
        {
            if (item == null)
                continue;
            DeleteLinkedTravelHistory(item, objectSpace);
        }
    }

    private static Type? GetTravelHistoryType(string applicationTypeName) =>
        applicationTypeName switch
        {
            AppRegCheckIn => typeof(ExternalArrival),
            AppRegCheckOut => typeof(ExternalDeparture),
            AppRegCheckInInternal => typeof(InternalArrival),
            AppRegCheckOutInternal => typeof(InternalDeparture),
            _ => null
        };

    private static bool TryValidateTravelData(ApplicationItem item, out string message)
    {
        message = string.Empty;
        if (item.Person == null)
        {
            message = "Person is required.";
            return false;
        }

        if (!item.TravelDate.HasValue || item.TravelDate.Value == default)
        {
            message = "TravelDate is not set.";
            return false;
        }

        if (!item.TravelType.HasValue || !item.MovementType.HasValue)
        {
            message = "TravelType or MovementType is not set.";
            return false;
        }

        if (item.TravelType == BusinessObjects.TravelType.External && item.CheckPoint == null)
        {
            message = "CheckPoint is required for external travel.";
            return false;
        }

        if (item.TravelType == BusinessObjects.TravelType.Internal
            && !TryGetInternalCityAndRegion(item, out _, out _))
        {
            message = "Application FromCity/ToCity (and region) are required for internal travel.";
            return false;
        }

        return true;
    }

    private static bool TryGetInternalCityAndRegion(
        ApplicationItem item,
        out City? city,
        out Region? region)
    {
        city = null;
        region = null;
        var application = item.Application;
        if (application == null || !item.MovementType.HasValue)
            return false;

        city = item.MovementType == MovementType.Entry
            ? application.ToCity
            : application.FromCity;
        region = city?.Region;
        return city != null && region != null;
    }

    private static TravelHistory? FindLinkedTravelHistory(ApplicationItem item, IObjectSpace objectSpace)
    {
        var linked = objectSpace.GetObjectsQuery<TravelHistory>()
            .FirstOrDefault(th => th.SourceApplicationItemID == item.ID);
        if (linked != null)
            return linked;

        // Soft-deleted rows are hidden from GetObjectsQuery but still block the unique index.
        ClearSoftDeletedTravelHistoryForSource(item.ID, objectSpace);
        return null;
    }

    private static void ClearSoftDeletedTravelHistoryForSource(Guid applicationItemId, IObjectSpace objectSpace)
    {
        if (objectSpace is not EFCoreObjectSpace efObjectSpace)
            return;

        efObjectSpace.DbContext.Database.ExecuteSqlRaw(
            "DELETE FROM [TravelHistories] WHERE [SourceApplicationItemID] = {0} AND [GCRecord] IS NOT NULL",
            applicationItemId);
    }

    private static void PhysicalDeleteTravelHistoryById(Guid travelHistoryId, IObjectSpace objectSpace)
    {
        if (objectSpace is not EFCoreObjectSpace efObjectSpace)
            return;

        efObjectSpace.DbContext.Database.ExecuteSqlRaw(
            "DELETE FROM [TravelHistories] WHERE [ID] = {0}",
            travelHistoryId);
    }

    private static TravelHistory? FindOrCreateLinkedTravelHistory(
        ApplicationItem item,
        IObjectSpace objectSpace,
        Type travelHistoryType)
    {
        ClearSoftDeletedTravelHistoryForSource(item.ID, objectSpace);

        var existing = objectSpace.GetObjectsQuery<TravelHistory>()
            .FirstOrDefault(th => th.SourceApplicationItemID == item.ID);
        if (existing != null && existing.GetType() != travelHistoryType)
        {
            PhysicalDeleteTravelHistoryById(existing.ID, objectSpace);
            existing = null;
        }

        if (existing != null)
            return existing;

        var heuristic = TryFindHeuristicMatch(item, objectSpace, travelHistoryType);
        if (heuristic != null)
        {
            heuristic.SourceApplicationItem = item;
            return heuristic;
        }

        return (TravelHistory)objectSpace.CreateObject(travelHistoryType);
    }

    private static TravelHistory? TryFindHeuristicMatch(
        ApplicationItem item,
        IObjectSpace objectSpace,
        Type travelHistoryType)
    {
        if (item.Person == null || !item.TravelDate.HasValue || !item.TravelType.HasValue || !item.MovementType.HasValue)
            return null;

        var travelDate = item.TravelDate.Value.Date;
        var personId = item.Person.ID;

        return objectSpace.GetObjectsQuery<TravelHistory>()
            .Where(th =>
                th.SourceApplicationItemID == null
                && th.Person != null
                && th.Person.ID == personId
                && th.TravelDate.Date == travelDate
                && th.TravelType == item.TravelType
                && th.MovementType == item.MovementType)
            .AsEnumerable()
            .FirstOrDefault(th => th.GetType() == travelHistoryType);
    }

    private static void ApplyFieldsFromApplicationItem(ApplicationItem item, TravelHistory history)
    {
        history.SourceApplicationItem = item;
        history.Person = item.Person;
        history.TravelDate = item.TravelDate!.Value;
        history.TravelType = item.TravelType;
        history.MovementType = item.MovementType;
        history.Notes = item.TravelNotes;

        if (item.TravelType == BusinessObjects.TravelType.External)
        {
            history.CheckPoint = item.CheckPoint;
            history.Region = null;
            history.City = null;
            if (history.Country == null)
            {
                var objectSpace = ObjectSpaceHelper.Get(history);
                history.Country = objectSpace?
                    .GetObjectsQuery<Country>()
                    .FirstOrDefault(c => c.IsDefault);
            }
        }
        else if (TryGetInternalCityAndRegion(item, out var city, out var region))
        {
            history.CheckPoint = null;
            history.Country = null;
            history.City = city;
            history.Region = region;
        }
    }
}
