using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.ReportDashboard;

/// <summary>
/// Report Dashboard roster reads: prefer <see cref="ApplicationPerson"/> M2M rows; fall back to legacy
/// <see cref="ApplicationItem"/> when an application has no M2M roster (same cutover as
/// <see cref="ApplicationPersonRoster.ApplicationRosterHelper"/>).
/// </summary>
internal static class ReportDashboardRosterQueryHelper
{
    internal sealed record RosterApplicationLine(Guid RecordId, Person Person, Application Application);

    internal sealed record TravelLine(Guid RecordId, Person Person, Application? Application, DateTime? TravelDate);

    internal static IQueryable<Guid> ApplicationIdsWithM2mRoster(Visa2026EFCoreDbContext db) =>
        db.ApplicationPeople.AsNoTracking().Select(ap => ap.ApplicationId).Distinct();

    internal static IQueryable<ApplicationPerson> RegistrationOnProcessM2mQuery(
        Visa2026EFCoreDbContext db,
        IReadOnlyCollection<string> regTypes,
        DateTime cutoff)
    {
        return db.ApplicationPeople.AsNoTracking()
            .Where(ap => ap.Application != null
                && ap.Application.ApplicationType != null
                && ap.Application.ApplicationType.Name != null
                && regTypes.Contains(ap.Application.ApplicationType.Name)
                && (ap.Application.ApplicationDate == null || ap.Application.ApplicationDate >= cutoff)
                && ap.Person != null
                && !ap.Person.IsArchived);
    }

    internal static IQueryable<ApplicationItem> RegistrationOnProcessLegacyQuery(
        Visa2026EFCoreDbContext db,
        IReadOnlyCollection<string> regTypes,
        DateTime cutoff,
        IQueryable<Guid> appsWithM2m)
    {
        return db.ApplicationItems.AsNoTracking()
            .Where(ai => ai.Application != null
                && ai.Application.ApplicationType != null
                && ai.Application.ApplicationType.Name != null
                && regTypes.Contains(ai.Application.ApplicationType.Name)
                && (ai.Application.ApplicationDate == null || ai.Application.ApplicationDate >= cutoff)
                && ai.Person != null
                && !ai.Person.IsArchived
                && !appsWithM2m.Contains(ai.Application!.ID));
    }

    internal static List<RosterApplicationLine> LoadRegistrationOnProcessLines(
        Visa2026EFCoreDbContext db,
        IReadOnlyCollection<string> regTypes,
        DateTime cutoff,
        PersonRecordRole? role,
        string projectKey,
        HashSet<Guid>? validVisaPersonIds)
    {
        var appsWithM2m = ApplicationIdsWithM2mRoster(db);

        var m2mQuery = RegistrationOnProcessM2mQuery(db, regTypes, cutoff);
        var legacyQuery = RegistrationOnProcessLegacyQuery(db, regTypes, cutoff, appsWithM2m);

        if (role.HasValue)
        {
            var roleValue = role.Value;
            m2mQuery = m2mQuery.Where(ap => ap.Person!.PersonRole == roleValue);
            legacyQuery = legacyQuery.Where(ai => ai.Person!.PersonRole == roleValue);
        }

        if (validVisaPersonIds != null)
        {
            m2mQuery = m2mQuery.Where(ap => validVisaPersonIds.Contains(ap.Person!.ID));
            legacyQuery = legacyQuery.Where(ai => validVisaPersonIds.Contains(ai.Person!.ID));
        }

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            m2mQuery = m2mQuery.Where(ap =>
                (ap.Application!.ProjectContract != null
                    && (ap.Application.ProjectContract.Name == projectKey
                        || ap.Application.ProjectContract.NameTm == projectKey))
                || (ap.Person!.ProjectContract != null
                    && (ap.Person.ProjectContract.Name == projectKey
                        || ap.Person.ProjectContract.NameTm == projectKey)));

            legacyQuery = legacyQuery.Where(ai =>
                (ai.Application!.ProjectContract != null
                    && (ai.Application.ProjectContract.Name == projectKey
                        || ai.Application.ProjectContract.NameTm == projectKey))
                || (ai.Person!.ProjectContract != null
                    && (ai.Person.ProjectContract.Name == projectKey
                        || ai.Person.ProjectContract.NameTm == projectKey)));
        }

        var m2mRows = m2mQuery
            .Include(ap => ap.Person!).ThenInclude(p => p.ProjectContract)
            .Include(ap => ap.Application!).ThenInclude(a => a.ApplicationType)
            .Include(ap => ap.Application!).ThenInclude(a => a.ProjectContract)
            .Include(ap => ap.Application!).ThenInclude(a => a.ApprovalLegSnapshots)
            .Include(ap => ap.Application!).ThenInclude(a => a.ApprovalLegProfile!)
                .ThenInclude(p => p.MinistryLegs!)
                .ThenInclude(l => l.ApprovingMinistry)
            .Include(ap => ap.Application!).ThenInclude(a => a.LatestProgress!)
                .ThenInclude(p => p.State)
            .ToList()
            .Select(ap => new RosterApplicationLine(ap.ID, ap.Person!, ap.Application!))
            .ToList();

        var legacyRows = legacyQuery
            .Include(ai => ai.Person!).ThenInclude(p => p.ProjectContract)
            .Include(ai => ai.Application!).ThenInclude(a => a.ApplicationType)
            .Include(ai => ai.Application!).ThenInclude(a => a.ProjectContract)
            .Include(ai => ai.Application!).ThenInclude(a => a.ApprovalLegSnapshots)
            .Include(ai => ai.Application!).ThenInclude(a => a.ApprovalLegProfile!)
                .ThenInclude(p => p.MinistryLegs!)
                .ThenInclude(l => l.ApprovingMinistry)
            .Include(ai => ai.Application!).ThenInclude(a => a.LatestProgress!)
                .ThenInclude(p => p.State)
            .ToList()
            .Select(ai => new RosterApplicationLine(ai.ID, ai.Person!, ai.Application!))
            .ToList();

        return m2mRows.Concat(legacyRows).ToList();
    }

    internal static int CountTravelLines(Visa2026EFCoreDbContext db, PersonRecordRole? role, DateTime cutoff)
    {
        var appsWithM2m = ApplicationIdsWithM2mRoster(db);

        var m2mCount = (
            from ap in db.ApplicationPeople.AsNoTracking()
            join rl in db.ApplicationPersonResolvedLinks.AsNoTracking()
                on ap.ID equals rl.ApplicationPersonId
            join th in db.TravelHistories.AsNoTracking()
                on rl.LinkedObjectId equals th.ID
            where rl.LinkKind == ApplicationPersonLinkKind.TravelHistory
                && th.TravelDate != null
                && th.TravelDate >= cutoff
                && ap.Person != null
                && (role == null || ap.Person.PersonRole == role)
            select ap.ID).Count();

        var legacyCount = db.ApplicationItems.AsNoTracking()
            .Count(ai => ai.Person != null
                && (role == null || ai.Person.PersonRole == role)
                && ai.TravelDate != null
                && ai.TravelDate >= cutoff
                && ai.Application != null
                && !appsWithM2m.Contains(ai.Application.ID));

        return m2mCount + legacyCount;
    }

    internal static List<TravelLine> LoadTravelLines(
        Visa2026EFCoreDbContext db,
        PersonRecordRole? role,
        string projectKey,
        DateTime cutoff,
        HashSet<Guid>? validVisaPersonIds,
        int take)
    {
        var appsWithM2m = ApplicationIdsWithM2mRoster(db);

        var m2mQuery =
            from ap in db.ApplicationPeople.AsNoTracking()
            join rl in db.ApplicationPersonResolvedLinks.AsNoTracking()
                on ap.ID equals rl.ApplicationPersonId
            join th in db.TravelHistories.AsNoTracking()
                on rl.LinkedObjectId equals th.ID
            where rl.LinkKind == ApplicationPersonLinkKind.TravelHistory
                && th.TravelDate != null
                && th.TravelDate >= cutoff
                && ap.Person != null
                && (role == null || ap.Person.PersonRole == role)
            select new TravelLine(ap.ID, ap.Person!, ap.Application, th.TravelDate);

        if (validVisaPersonIds != null)
            m2mQuery = m2mQuery.Where(x => validVisaPersonIds.Contains(x.Person.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            m2mQuery = m2mQuery.Where(x =>
                x.Application != null
                && x.Application.ProjectContract != null
                && (x.Application.ProjectContract.Name == projectKey
                    || x.Application.ProjectContract.NameTm == projectKey));
        }

        var legacyQuery = db.ApplicationItems.AsNoTracking()
            .Where(ai => ai.Person != null
                && (role == null || ai.Person.PersonRole == role)
                && ai.TravelDate != null
                && ai.TravelDate >= cutoff
                && ai.Application != null
                && !appsWithM2m.Contains(ai.Application.ID));

        if (validVisaPersonIds != null)
            legacyQuery = legacyQuery.Where(ai => validVisaPersonIds.Contains(ai.Person!.ID));

        if (!string.IsNullOrWhiteSpace(projectKey) && projectKey != "All")
        {
            legacyQuery = legacyQuery.Where(ai =>
                ai.Application!.ProjectContract != null
                && (ai.Application.ProjectContract.Name == projectKey
                    || ai.Application.ProjectContract.NameTm == projectKey));
        }

        var m2mRows = m2mQuery
            .OrderByDescending(x => x.TravelDate)
            .Take(take)
            .ToList();

        if (m2mRows.Count >= take)
            return m2mRows;

        var remaining = take - m2mRows.Count;
        var legacyRows = legacyQuery
            .OrderByDescending(ai => ai.TravelDate)
            .Take(remaining)
            .AsEnumerable()
            .Select(ai => new TravelLine(ai.ID, ai.Person!, ai.Application, ai.TravelDate))
            .ToList();

        return m2mRows.Concat(legacyRows)
            .OrderByDescending(x => x.TravelDate)
            .Take(take)
            .ToList();
    }

    internal static int CountPassportApplicationLines(
        Visa2026EFCoreDbContext db, PersonRecordRole? role, DateTime cutoff)
    {
        var appsWithM2m = ApplicationIdsWithM2mRoster(db);

        var m2mCount = (
            from ap in db.ApplicationPeople.AsNoTracking()
            join rl in db.ApplicationPersonResolvedLinks.AsNoTracking()
                on ap.ID equals rl.ApplicationPersonId
            where rl.LinkKind == ApplicationPersonLinkKind.Passport
                && rl.LinkedObjectId != null
                && ap.Person != null
                && !ap.Person.IsArchived
                && (role == null || ap.Person.PersonRole == role)
                && ap.Application != null
                && ap.Application.ApplicationDate != null
                && ap.Application.ApplicationDate >= cutoff
            select ap.ID).Count();

        var legacyCount = db.ApplicationItems.AsNoTracking()
            .Count(ai => ai.CurrentPassport != null
                && ai.Person != null
                && !ai.Person.IsArchived
                && (role == null || ai.Person.PersonRole == role)
                && ai.Application != null
                && ai.Application.ApplicationDate != null
                && ai.Application.ApplicationDate >= cutoff
                && !appsWithM2m.Contains(ai.Application.ID));

        return m2mCount + legacyCount;
    }

    internal static int CountAddressApplicationLines(
        Visa2026EFCoreDbContext db, PersonRecordRole? role, DateTime cutoff)
    {
        var appsWithM2m = ApplicationIdsWithM2mRoster(db);

        var m2mCount = (
            from ap in db.ApplicationPeople.AsNoTracking()
            join rl in db.ApplicationPersonResolvedLinks.AsNoTracking()
                on ap.ID equals rl.ApplicationPersonId
            where rl.LinkKind == ApplicationPersonLinkKind.AddressOfResidence
                && rl.LinkedObjectId != null
                && ap.Person != null
                && !ap.Person.IsArchived
                && (role == null || ap.Person.PersonRole == role)
                && ap.Application != null
                && ap.Application.ApplicationDate >= cutoff
            select ap.ID).Count();

        var legacyCount = db.ApplicationItems.AsNoTracking()
            .Count(ai => ai.CurrentAddressOfResidence != null
                && ai.Person != null
                && !ai.Person.IsArchived
                && (role == null || ai.Person.PersonRole == role)
                && ai.Application != null
                && ai.Application.ApplicationDate >= cutoff
                && !appsWithM2m.Contains(ai.Application.ID));

        return m2mCount + legacyCount;
    }

    /// <summary>
    /// Child BO ids linked on applications with <see cref="Application.ApplicationDate"/> on/after
    /// <paramref name="cutoff"/> — M2M resolved links first, legacy <see cref="ApplicationItem"/> fallback.
    /// </summary>
    internal static HashSet<Guid> GetLinkedChildIdsInApplicationDateRange(
        Visa2026EFCoreDbContext db,
        ApplicationPersonLinkKind linkKind,
        DateTime cutoff)
    {
        var appsWithM2m = ApplicationIdsWithM2mRoster(db);

        var m2mIds =
            from ap in db.ApplicationPeople.AsNoTracking()
            join rl in db.ApplicationPersonResolvedLinks.AsNoTracking()
                on ap.ID equals rl.ApplicationPersonId
            where rl.LinkKind == linkKind
                && rl.LinkedObjectId != null
                && ap.Application != null
                && ap.Application.ApplicationDate >= cutoff
            select rl.LinkedObjectId!.Value;

        IQueryable<Guid> legacyIds = linkKind switch
        {
            ApplicationPersonLinkKind.Education => db.ApplicationItems.AsNoTracking()
                .Where(ai => ai.CurrentEducation != null
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff
                    && !appsWithM2m.Contains(ai.Application.ID))
                .Select(ai => ai.CurrentEducation!.ID),
            ApplicationPersonLinkKind.AddressOfResidence => db.ApplicationItems.AsNoTracking()
                .Where(ai => ai.CurrentAddressOfResidence != null
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff
                    && !appsWithM2m.Contains(ai.Application.ID))
                .Select(ai => ai.CurrentAddressOfResidence!.ID),
            ApplicationPersonLinkKind.Position => db.ApplicationItems.AsNoTracking()
                .Where(ai => ai.CurrentPositionHistory != null
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff
                    && !appsWithM2m.Contains(ai.Application.ID))
                .Select(ai => ai.CurrentPositionHistory!.ID),
            ApplicationPersonLinkKind.MedicalRecord => db.ApplicationItems.AsNoTracking()
                .Where(ai => ai.CurrentMedicalRecord != null
                    && ai.Application != null
                    && ai.Application.ApplicationDate >= cutoff
                    && !appsWithM2m.Contains(ai.Application.ID))
                .Select(ai => ai.CurrentMedicalRecord!.ID),
            _ => throw new ArgumentOutOfRangeException(nameof(linkKind), linkKind, "Unsupported link kind for application-date filter.")
        };

        return m2mIds.Union(legacyIds).ToHashSet();
    }

    /// <summary>Application ids with at least one roster person of <paramref name="role"/> (M2M + legacy fallback).</summary>
    internal static HashSet<Guid> ApplicationIdsWithPersonRole(
        Visa2026EFCoreDbContext db,
        PersonRecordRole role)
    {
        var appsWithM2m = ApplicationIdsWithM2mRoster(db);
        var roleValue = role;

        var m2mAppIds = db.ApplicationPeople.AsNoTracking()
            .Where(ap => ap.Person != null && ap.Person.PersonRole == roleValue)
            .Select(ap => ap.ApplicationId);

        var legacyAppIds = db.ApplicationItems.AsNoTracking()
            .Where(ai => ai.Application != null
                && ai.Person != null
                && ai.Person.PersonRole == roleValue
                && !appsWithM2m.Contains(ai.Application!.ID))
            .Select(ai => ai.Application!.ID);

        return m2mAppIds.Union(legacyAppIds).ToHashSet();
    }
}
