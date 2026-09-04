using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Report Dashboard roster reads via skip-navigation People + sticky ResolvedLinks.
/// </summary>
internal static class ReportDashboardRosterQueryHelper
{
    internal sealed record RosterApplicationLine(Guid RecordId, Person Person, ApplicationProfileInstance ApplicationProfileInstance);

    internal sealed record TravelLine(Guid RecordId, Person Person, ApplicationProfileInstance? ApplicationProfileInstance, DateTime? TravelDate);

    internal static IQueryable<Guid> ApplicationProfileInstanceIdsWithM2mRoster(Visa2026EFCoreDbContext db) =>
        db.ApplicationProfileInstances.AsNoTracking()
            .Where(a => a.People.Any())
            .Select(a => a.ID);

    internal static IQueryable<ApplicationProfileInstance> RegistrationOnProcessApplicationsQuery(
        Visa2026EFCoreDbContext db,
        IReadOnlyCollection<string> regTypes,
        DateTime cutoff)
    {
        return db.ApplicationProfileInstances.AsNoTracking()
            .Where(a => a.ApplicationType != null
                && a.ApplicationType.Name != null
                && regTypes.Contains(a.ApplicationType.Name)
                && (a.ApplicationDate == null || a.ApplicationDate >= cutoff)
                && a.People.Any(p => p != null && !p.IsArchived));
    }

    internal static List<RosterApplicationLine> LoadRegistrationOnProcessLines(
        Visa2026EFCoreDbContext db,
        IReadOnlyCollection<string> regTypes,
        DateTime cutoff,
        PersonRecordRole? role,
        string projectKey,
        HashSet<Guid>? validVisaPersonIds)
    {
        var applications = RegistrationOnProcessApplicationsQuery(db, regTypes, cutoff)
            .Include(a => a.People)
            .Include(a => a.ApplicationType)
            .Include(a => a.ProjectContract)
            .Include(a => a.ApprovalLegSnapshots)
            .Include(a => a.ApprovalLegProfile!)
                .ThenInclude(p => p.MinistryLegs!)
                .ThenInclude(l => l.ApprovingMinistry)
            .Include(a => a.LatestProgress!)
                .ThenInclude(p => p.State)
            .ToList();

        var lines = new List<RosterApplicationLine>();
        foreach (var application in applications)
        {
            foreach (var person in application.People ?? [])
            {
                if (person == null || person.IsArchived)
                    continue;
                if (role.HasValue && person.PersonRole != role.Value)
                    continue;
                if (validVisaPersonIds != null && !validVisaPersonIds.Contains(person.ID))
                    continue;
                if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
                {
                    var onApp = application.ProjectContract != null
                        && (application.ProjectContract.Name == projectKey
                            || application.ProjectContract.NameTm == projectKey);
                    var onPerson = person.ProjectContract != null
                        && (person.ProjectContract.Name == projectKey
                            || person.ProjectContract.NameTm == projectKey);
                    if (!onApp && !onPerson)
                        continue;
                }

                lines.Add(new RosterApplicationLine(person.ID, person, application));
            }
        }

        return lines;
    }

    internal static int CountTravelLines(Visa2026EFCoreDbContext db, PersonRecordRole? role, DateTime cutoff)
    {
        return (
            from rl in db.ApplicationProfileInstancePersonResolvedLinks.AsNoTracking()
            join th in db.TravelHistories.AsNoTracking()
                on rl.LinkedObjectId equals th.ID
            where rl.LinkKind == ApplicationProfileInstancePersonLinkKind.TravelHistory
                && th.TravelDate != null
                && th.TravelDate >= cutoff
                && rl.Person != null
                && (role == null || rl.Person.PersonRole == role)
            select rl.PersonId).Count();
    }

    internal static List<TravelLine> LoadTravelLines(
        Visa2026EFCoreDbContext db,
        PersonRecordRole? role,
        string projectKey,
        DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds,
        int take)
    {
        var m2mQuery =
            from rl in db.ApplicationProfileInstancePersonResolvedLinks.AsNoTracking()
            join th in db.TravelHistories.AsNoTracking()
                on rl.LinkedObjectId equals th.ID
            where rl.LinkKind == ApplicationProfileInstancePersonLinkKind.TravelHistory
                && th.TravelDate != null
                && th.TravelDate >= cutoff
                && rl.Person != null
                && (role == null || rl.Person.PersonRole == role)
            select new TravelLine(rl.PersonId, rl.Person!, rl.ApplicationProfileInstance, th.TravelDate);

        if (validVisaPersonIds != null)
            m2mQuery = m2mQuery.Where(x => validVisaPersonIds.Contains(x.Person.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            m2mQuery = m2mQuery.Where(x =>
                x.ApplicationProfileInstance != null
                && x.ApplicationProfileInstance.ProjectContract != null
                && (x.ApplicationProfileInstance.ProjectContract.Name == projectKey
                    || x.ApplicationProfileInstance.ProjectContract.NameTm == projectKey));
        }

        return m2mQuery
            .OrderByDescending(x => x.TravelDate)
            .Take(take)
            .ToList();
    }

    internal static int CountPassportApplicationLines(
        Visa2026EFCoreDbContext db, PersonRecordRole? role, DateTime cutoff)
    {
        return (
            from rl in db.ApplicationProfileInstancePersonResolvedLinks.AsNoTracking()
            where rl.LinkKind == ApplicationProfileInstancePersonLinkKind.Passport
                && rl.LinkedObjectId != null
                && rl.Person != null
                && !rl.Person.IsArchived
                && (role == null || rl.Person.PersonRole == role)
                && rl.ApplicationProfileInstance != null
                && rl.ApplicationProfileInstance.ApplicationDate != null
                && rl.ApplicationProfileInstance.ApplicationDate >= cutoff
            select rl.PersonId).Count();
    }

    internal static int CountAddressApplicationLines(
        Visa2026EFCoreDbContext db, PersonRecordRole? role, DateTime cutoff)
    {
        return (
            from rl in db.ApplicationProfileInstancePersonResolvedLinks.AsNoTracking()
            where rl.LinkKind == ApplicationProfileInstancePersonLinkKind.AddressOfResidence
                && rl.LinkedObjectId != null
                && rl.Person != null
                && !rl.Person.IsArchived
                && (role == null || rl.Person.PersonRole == role)
                && rl.ApplicationProfileInstance != null
                && rl.ApplicationProfileInstance.ApplicationDate >= cutoff
            select rl.PersonId).Count();
    }

    /// <summary>
    /// Child BO ids linked on applications with ApplicationDate on/after <paramref name="cutoff"/>.
    /// </summary>
    internal static HashSet<Guid> GetLinkedChildIdsInApplicationDateRange(
        Visa2026EFCoreDbContext db,
        ApplicationProfileInstancePersonLinkKind linkKind,
        DateTime cutoff)
    {
        return (
            from rl in db.ApplicationProfileInstancePersonResolvedLinks.AsNoTracking()
            where rl.LinkKind == linkKind
                && rl.LinkedObjectId != null
                && rl.ApplicationProfileInstance != null
                && rl.ApplicationProfileInstance.ApplicationDate >= cutoff
            select rl.LinkedObjectId!.Value).ToHashSet();
    }

    /// <summary>ApplicationProfileInstance ids with at least one roster person of <paramref name="role"/>.</summary>
    internal static HashSet<Guid> ApplicationProfileInstanceIdsWithPersonRole(
        Visa2026EFCoreDbContext db,
        PersonRecordRole role)
    {
        var roleValue = role;
        return db.ApplicationProfileInstances.AsNoTracking()
            .Where(a => a.People.Any(p => p.PersonRole == roleValue))
            .Select(a => a.ID)
            .ToHashSet();
    }
}
