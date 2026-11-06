using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;

namespace Visa2026.Module.Services.PreviewSlot;

public static partial class IssueIssuedHeaderComposeService
{
    private static IssueIssuedHeaderCreateResult CreateWorkPermitLocationGrant(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedHeaderComposeDraft draft,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        var grant = objectSpace.CreateObject<WorkPermitLocationGrant>();
        grant.ApplicationProfileInstance = instance;
        grant.GrantNumber = draft.HeaderNumber.Trim();
        grant.IssuedDate = draft.PrimaryDate.Date;
        grant.Items ??= new System.Collections.ObjectModel.ObservableCollection<WorkPermitLocationGrantItem>();

        WorkPermit? sourceHeader = null;
        foreach (var row in selected)
        {
            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null)
                continue;

            var sourceItem = ResolveLinkedWorkPermitItem(objectSpace, instance, person.ID);
            if (sourceHeader == null && sourceItem?.WorkPermit != null)
                sourceHeader = sourceItem.WorkPermit;

            var item = objectSpace.CreateObject<WorkPermitLocationGrantItem>();
            item.Grant = grant;
            item.Person = person;
            item.Passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            item.GrantedLocations = string.IsNullOrWhiteSpace(row.WorkPermittedLocations)
                ? CasePersonRequestedLocation.ResolveForPrefill(
                    objectSpace,
                    instance.ID,
                    person.ID,
                    ApplicationProfileInstancePersonRequestedLocationKind.WorkPermit,
                    instance.MovementPermitLocation)
                : row.WorkPermittedLocations.Trim();
            item.SourceWorkPermitItem = sourceItem;
            grant.Items.Add(item);
        }

        grant.SourceWorkPermit = sourceHeader;
        objectSpace.CommitChanges();

        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = grant.ID,
            HeaderCaption = grant.GrantNumber,
            Lines = grant.Items.Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                LocationsCaption = i.GrantedLocations?.Trim() ?? string.Empty,
            }).ToList(),
        };
    }

    private static IssueIssuedHeaderCreateResult UpdateWorkPermitLocationGrant(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft,
        Guid headerId,
        List<IssueIssuedHeaderPersonLineDraft> selected)
    {
        var grant = objectSpace.GetObjectByKey<WorkPermitLocationGrant>(headerId);
        if (grant == null)
            return new IssueIssuedHeaderCreateResult { Succeeded = false, ErrorMessage = "Location permission was not found." };

        grant.GrantNumber = draft.HeaderNumber.Trim();
        grant.IssuedDate = draft.PrimaryDate.Date;
        grant.Items ??= new System.Collections.ObjectModel.ObservableCollection<WorkPermitLocationGrantItem>();

        foreach (var row in selected)
        {
            var existing = grant.Items.FirstOrDefault(i => i.Person != null && i.Person.ID == row.PersonId);
            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(row.WorkPermittedLocations))
                    existing.GrantedLocations = row.WorkPermittedLocations.Trim();
                continue;
            }

            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null)
                continue;

            var sourceItem = ResolveLinkedWorkPermitItem(objectSpace, grant.ApplicationProfileInstance, person.ID);
            var item = objectSpace.CreateObject<WorkPermitLocationGrantItem>();
            item.Grant = grant;
            item.Person = person;
            item.Passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            item.GrantedLocations = row.WorkPermittedLocations?.Trim() ?? string.Empty;
            item.SourceWorkPermitItem = sourceItem;
            grant.Items.Add(item);
            if (grant.SourceWorkPermit == null && sourceItem?.WorkPermit != null)
                grant.SourceWorkPermit = sourceItem.WorkPermit;
        }

        objectSpace.CommitChanges();
        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = grant.ID,
            HeaderCaption = grant.GrantNumber,
            Lines = grant.Items.Select(i => new IssueIssuedHeaderCreatedLine
            {
                LineId = i.ID,
                PersonId = i.Person?.ID ?? Guid.Empty,
                PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                LocationsCaption = i.GrantedLocations?.Trim() ?? string.Empty,
            }).ToList(),
        };
    }

    private static WorkPermitItem? ResolveLinkedWorkPermitItem(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? instance,
        Guid personId)
    {
        if (objectSpace == null || instance == null || personId == Guid.Empty)
            return null;

        var linkedId = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l =>
                l.ApplicationProfileInstanceId == instance.ID
                && l.PersonId == personId
                && l.LinkKind == ApplicationProfileInstancePersonLinkKind.WorkPermitItem)
            .Select(l => l.LinkedObjectId)
            .FirstOrDefault();
        if (linkedId != Guid.Empty)
            return objectSpace.GetObjectByKey<WorkPermitItem>(linkedId);

        return objectSpace.GetObjectsQuery<WorkPermitItem>()
            .Where(i => i.Person != null && i.Person.ID == personId
                && i.ApplicationProfileInstances.Any(a => a.ID == instance.ID))
            .OrderByDescending(i => i.StartDate)
            .FirstOrDefault();
    }

    internal static void ApplySourceWorkPermitPreview(IssueIssuedHeaderPersonLineDraft line, WorkPermitItem? item)
    {
        line.SourceWorkPermitCaption = FormatSourceWorkPermitCaption(item);
        line.SourceWorkPermitItemId = item != null && item.ID != Guid.Empty ? item.ID : null;
        var headerId = item?.WorkPermit?.ID ?? Guid.Empty;
        line.SourceWorkPermitId = headerId != Guid.Empty ? headerId : null;
    }

    internal static string FormatSourceWorkPermitCaption(WorkPermitItem? item)
    {
        if (item == null)
            return string.Empty;

        var number = item.WorkPermit?.WorkPermitNumber?.Trim();
        if (string.IsNullOrWhiteSpace(number))
            number = item.WorkPermitNumber?.Trim();
        if (string.IsNullOrWhiteSpace(number))
            number = item.ASNumber?.Trim();
        if (string.IsNullOrWhiteSpace(number))
            return string.Empty;

        string FormatDay(DateTime? d) =>
            d is { } v && v.Year > 1900 ? v.ToString("dd.MM.yyyy") : string.Empty;

        var start = FormatDay(item.StartDate == default ? null : item.StartDate);
        var end = FormatDay(item.ExpirationDate == default ? null : item.ExpirationDate);
        if (start.Length > 0 && end.Length > 0)
            return $"{number} · {start} – {end}";
        if (start.Length > 0)
            return $"{number} · {start}";
        return number;
    }

    internal static IssueIssuedHeaderComposeDraft? BindExistingWorkPermitLocationGrant(
        IObjectSpace objectSpace,
        IssueIssuedHeaderComposeDraft draft,
        Guid headerId)
    {
        var grant = objectSpace.GetObjectByKey<WorkPermitLocationGrant>(headerId);
        if (grant == null)
            return null;

        draft.Title = "Edit location permission";
        draft.HeaderNumber = grant.GrantNumber?.Trim() ?? string.Empty;
        draft.PrimaryDate = grant.IssuedDate == default ? DateTime.Today : grant.IssuedDate.Date;

        var items = grant.Items?.Where(i => i != null).ToList()
            ?? new List<WorkPermitLocationGrantItem>();
        foreach (var person in draft.People)
        {
            var item = items.FirstOrDefault(i => i.Person != null && i.Person.ID == person.PersonId);
            person.Include = item != null;
            if (item != null)
            {
                person.ExistingLineId = item.ID;
                person.IncludeLocked = true;
                person.StatusCaption = "On this location permission";
                person.WorkPermittedLocations = item.GrantedLocations?.Trim() ?? string.Empty;
                var sourceItem = item.SourceWorkPermitItem;
                if (sourceItem == null && grant.SourceWorkPermit != null)
                {
                    sourceItem = grant.SourceWorkPermit.WorkPermitItems?
                        .FirstOrDefault(i => i.Person != null && i.Person.ID == person.PersonId);
                }
                ApplySourceWorkPermitPreview(person, sourceItem);
                if (string.IsNullOrWhiteSpace(person.SourceWorkPermitCaption) && grant.SourceWorkPermit != null)
                {
                    person.SourceWorkPermitCaption = grant.SourceWorkPermit.WorkPermitNumber?.Trim() ?? string.Empty;
                    person.SourceWorkPermitId = grant.SourceWorkPermit.ID;
                }
                if (item.Passport != null)
                {
                    person.PassportId = item.Passport.ID;
                    person.PassportNumber = item.Passport.PassportNumber?.Trim() ?? person.PassportNumber;
                    person.IsReady = true;
                }
            }
        }

        BindDocuments(objectSpace, draft, IssueIssuedHeaderKind.WorkPermitLocationGrant, headerId);
        return draft;
    }

    internal static IssueIssuedHeaderCreateResult? SnapshotWorkPermitLocationGrant(
        IObjectSpace objectSpace,
        Guid headerId)
    {
        var grant = objectSpace.GetObjectByKey<WorkPermitLocationGrant>(headerId);
        if (grant == null)
            return null;
        return new IssueIssuedHeaderCreateResult
        {
            Succeeded = true,
            HeaderId = grant.ID,
            HeaderCaption = grant.GrantNumber ?? string.Empty,
            Lines = (grant.Items ?? Array.Empty<WorkPermitLocationGrantItem>())
                .Where(i => i != null)
                .Select(i => new IssueIssuedHeaderCreatedLine
                {
                    LineId = i.ID,
                    PersonId = i.Person?.ID ?? Guid.Empty,
                    PersonName = i.Person?.FullName?.Trim() ?? string.Empty,
                    PassportNumber = i.Passport?.PassportNumber?.Trim() ?? string.Empty,
                    LocationsCaption = i.GrantedLocations?.Trim() ?? string.Empty,
                })
                .ToList(),
        };
    }
}