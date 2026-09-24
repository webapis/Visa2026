using System;
using System.Collections.Generic;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.OfficerShell;

public sealed record ApplicationProfileInstanceExclusionAddresseeOption(
    string Key,
    ApplicationProfileInstanceExclusionAddresseeKind Kind,
    int? Leg,
    string Label,
    string AddresseeName,
    bool IsCurrentHolder);

public sealed class ApplicationProfileInstanceExclusionDraft
{
    public DateTime LetterDate { get; set; } = DateTime.Today;
    public string? AddresseeKey { get; set; }
    public string AddresseeName { get; set; } = string.Empty;
    public string? Salutation { get; set; }
    public string? ReferenceMinistryName { get; set; }
    public DateTime? ReferenceLetterDate { get; set; }
    public string? ReferenceLetterNumber { get; set; }
    public int OriginalRosterCount { get; set; }
    public string? Subject { get; set; }
    public HashSet<Guid> PersonIds { get; } = new();
}

public sealed class ApplicationProfileInstanceExclusionRosterRow
{
    public int Index { get; init; }
    public Guid PersonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? PassportNumber { get; init; }
    public Guid? ExclusionId { get; init; }
    public string? ExcludedLetterNumber { get; init; }
    public DateTime? ExcludedLetterDate { get; init; }
    public bool IsExcluded => ExclusionId.HasValue;
}

public sealed record ApplicationProfileInstanceExcludedPerson(Guid ExclusionId, string LetterNumber, DateTime LetterDate);

public sealed class ApplicationProfileInstanceExclusionResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }
    public Guid? ExclusionId { get; private init; }

    /// <summary>True when no active (non-excluded) people remain on the roster after this save.</summary>
    public bool AllPeopleExcluded { get; private init; }

    public static ApplicationProfileInstanceExclusionResult Failed(string message) => new() { ErrorMessage = message };

    public static ApplicationProfileInstanceExclusionResult Succeeded(Guid? exclusionId = null, bool allPeopleExcluded = false) =>
        new() { Success = true, ExclusionId = exclusionId, AllPeopleExcluded = allPeopleExcluded };
}

public interface IApplicationProfileInstanceExclusionService
{
    /// <summary>Seretmezlik is allowed after office preparation and before the workflow is terminal.</summary>
    bool CanCreate(ApplicationProfileInstance instance, out string? reason);

    IReadOnlyList<ApplicationProfileInstanceExclusionAddresseeOption> GetAddresseeOptions(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance);

    ApplicationProfileInstanceExclusionDraft CreateDraft(IObjectSpace objectSpace, ApplicationProfileInstance instance);

    IReadOnlyList<ApplicationProfileInstanceExclusionRosterRow> GetRoster(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance);

    IReadOnlyList<ApplicationProfileInstanceExclusion> GetExclusions(IObjectSpace objectSpace, Guid instanceId);

    /// <summary>Creates the letter, numbers it, and excludes the selected people. Caller commits.</summary>
    ApplicationProfileInstanceExclusionResult Create(
        IObjectSpace objectSpace,
        Guid instanceId,
        ApplicationProfileInstanceExclusionDraft draft,
        string? userName);

    /// <summary>Edits letter details only; the excluded people list is locked after save. Caller commits.</summary>
    ApplicationProfileInstanceExclusionResult UpdateLetterDetails(
        IObjectSpace objectSpace,
        Guid exclusionId,
        ApplicationProfileInstanceExclusionDraft draft);

    /// <summary>Closes the case as <c>PROCESS_CANCELLED</c> (everyone excluded). Caller commits.</summary>
    ApplicationProfileInstanceExclusionResult CloseAsCancelled(IObjectSpace objectSpace, Guid instanceId, DateTime date);
}

public sealed class ApplicationProfileInstanceExclusionService : IApplicationProfileInstanceExclusionService
{
    public const string MigrationKeyPrefix = "migration:";
    public const string MinistryKeyPrefix = "leg:";

    private readonly IOfficerShellCaseProgressService _progress;

    public ApplicationProfileInstanceExclusionService(IOfficerShellCaseProgressService progress)
    {
        _progress = progress;
    }

    public bool CanCreate(ApplicationProfileInstance instance, out string? reason)
    {
        reason = null;
        if (instance == null)
        {
            reason = "Case not found.";
            return false;
        }

        if (!instance.IsLockedAfterOfficePreparation)
        {
            reason = "Seretmezlik is available after the case has left office preparation.";
            return false;
        }

        if (instance.IsWorkflowTerminal)
        {
            reason = "This case is closed. Seretmezlik letters can no longer be prepared.";
            return false;
        }

        return true;
    }

    /// <summary>
    /// Who holds the case now: <c>N_REVIEW_STARTED</c> / <c>N_REVIEW_REJECTED</c> → leg N;
    /// <c>N_REVIEW_APPROVED</c> → leg N+1 when configured, otherwise Migration Service; anything else → Migration Service.
    /// </summary>
    public static (ApplicationProfileInstanceExclusionAddresseeKind Kind, int? Leg) ResolveCurrentHolder(
        string? latestStateCode,
        IReadOnlyCollection<int> configuredLegs)
    {
        if (ApplicationProfileInstanceProgressLegCodes.TryParseMinistryLegFromStateCode(latestStateCode, out var leg))
        {
            if (latestStateCode!.Trim().EndsWith("_REVIEW_APPROVED", StringComparison.OrdinalIgnoreCase))
            {
                var next = leg + 1;
                return configuredLegs.Contains(next)
                    ? (ApplicationProfileInstanceExclusionAddresseeKind.Ministry, next)
                    : (ApplicationProfileInstanceExclusionAddresseeKind.MigrationService, null);
            }

            if (configuredLegs.Count == 0 || configuredLegs.Contains(leg))
                return (ApplicationProfileInstanceExclusionAddresseeKind.Ministry, leg);
        }

        return (ApplicationProfileInstanceExclusionAddresseeKind.MigrationService, null);
    }

    public IReadOnlyList<ApplicationProfileInstanceExclusionAddresseeOption> GetAddresseeOptions(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance)
    {
        var legs = GetLegSnapshots(instance);
        var holder = ResolveCurrentHolder(
            ApplicationProfileInstanceProgressPrimaryStateCodeResolver.Resolve(instance),
            legs.Select(l => l.Sequence ?? 0).Where(s => s > 0).ToList());

        var options = new List<ApplicationProfileInstanceExclusionAddresseeOption>();
        foreach (var leg in legs)
        {
            var seq = leg.Sequence ?? 0;
            if (seq <= 0)
                continue;
            var name = FirstNonEmpty(leg.MinistryNameTm, leg.MinistryShortName);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            options.Add(new ApplicationProfileInstanceExclusionAddresseeOption(
                MinistryKeyPrefix + seq,
                ApplicationProfileInstanceExclusionAddresseeKind.Ministry,
                seq,
                $"{name} (leg {seq})",
                ApplicationProfileInstanceExclusionLetterBuilder.TurkmenDative(name),
                holder.Kind == ApplicationProfileInstanceExclusionAddresseeKind.Ministry && holder.Leg == seq));
        }

        var migrationServices = instance.MigrationService != null
            ? new List<MigrationService> { instance.MigrationService }
            : objectSpace.GetObjectsQuery<MigrationService>().ToList();
        var firstMigration = true;
        foreach (var service in migrationServices.OrderBy(m => m.NameTm ?? m.Name))
        {
            var name = FirstNonEmpty(service.NameTm, service.Name);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            options.Add(new ApplicationProfileInstanceExclusionAddresseeOption(
                MigrationKeyPrefix + service.ID,
                ApplicationProfileInstanceExclusionAddresseeKind.MigrationService,
                null,
                name,
                ApplicationProfileInstanceExclusionLetterBuilder.TurkmenDative(name),
                firstMigration && holder.Kind == ApplicationProfileInstanceExclusionAddresseeKind.MigrationService));
            firstMigration = false;
        }

        return options;
    }

    public ApplicationProfileInstanceExclusionDraft CreateDraft(IObjectSpace objectSpace, ApplicationProfileInstance instance)
    {
        var options = GetAddresseeOptions(objectSpace, instance);
        var holder = options.FirstOrDefault(o => o.IsCurrentHolder) ?? options.FirstOrDefault();

        var firstLeg = GetLegSnapshots(instance).FirstOrDefault(l => (l.Sequence ?? 0) == 1)
            ?? GetLegSnapshots(instance).FirstOrDefault();
        var approvedFirstLeg = (instance.ProgressHistory ?? Enumerable.Empty<ApplicationProfileInstanceProgress>())
            .Where(p => string.Equals(
                p.State?.Code,
                ApplicationProfileInstanceProgressLegCodes.ReviewApproved(1),
                StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Order)
            .FirstOrDefault();

        return new ApplicationProfileInstanceExclusionDraft
        {
            LetterDate = DateTime.Today,
            AddresseeKey = holder?.Key,
            AddresseeName = holder?.AddresseeName ?? string.Empty,
            ReferenceMinistryName = firstLeg != null ? FirstNonEmpty(firstLeg.MinistryNameTm, firstLeg.MinistryShortName) : null,
            ReferenceLetterDate = approvedFirstLeg?.Date,
            OriginalRosterCount = instance.People?.Count ?? 0,
            Subject = BuildDefaultSubject(instance),
        };
    }

    public IReadOnlyList<ApplicationProfileInstanceExclusionRosterRow> GetRoster(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance)
    {
        var excluded = ApplicationProfileInstanceExclusionQueries.GetExcludedPeople(objectSpace, instance.ID);
        var passports = ResolvePassportNumbers(objectSpace, instance);
        return (instance.People ?? new List<Person>())
            .OrderBy(p => p.FullName, StringComparer.CurrentCultureIgnoreCase)
            .Select((p, i) =>
            {
                excluded.TryGetValue(p.ID, out var ex);
                return new ApplicationProfileInstanceExclusionRosterRow
                {
                    Index = i + 1,
                    PersonId = p.ID,
                    FullName = p.FullName,
                    PassportNumber = passports.TryGetValue(p.ID, out var number) ? number : null,
                    ExclusionId = ex?.ExclusionId,
                    ExcludedLetterNumber = ex?.LetterNumber,
                    ExcludedLetterDate = ex?.LetterDate,
                };
            })
            .ToList();
    }

    public IReadOnlyList<ApplicationProfileInstanceExclusion> GetExclusions(IObjectSpace objectSpace, Guid instanceId) =>
        objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusion>()
            .Include(e => e.People)
            .Where(e => e.ApplicationProfileInstanceId == instanceId)
            .AsEnumerable()
            .OrderByDescending(e => e.LetterDate)
            .ThenByDescending(e => e.CreatedOnUtc)
            .ToList();

    public ApplicationProfileInstanceExclusionResult Create(
        IObjectSpace objectSpace,
        Guid instanceId,
        ApplicationProfileInstanceExclusionDraft draft,
        string? userName)
    {
        if (objectSpace == null)
            return ApplicationProfileInstanceExclusionResult.Failed("ObjectSpace is required.");
        if (draft == null)
            return ApplicationProfileInstanceExclusionResult.Failed("Letter details are required.");

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (instance == null)
            return ApplicationProfileInstanceExclusionResult.Failed("Case not found.");
        if (!CanCreate(instance, out var reason))
            return ApplicationProfileInstanceExclusionResult.Failed(reason ?? "Seretmezlik is not available.");

        var detailsError = ValidateDetails(draft);
        if (detailsError != null)
            return ApplicationProfileInstanceExclusionResult.Failed(detailsError);
        if (draft.PersonIds.Count == 0)
            return ApplicationProfileInstanceExclusionResult.Failed("Select at least one person to exclude.");

        var roster = GetRoster(objectSpace, instance);
        var rosterById = roster.ToDictionary(r => r.PersonId);
        foreach (var personId in draft.PersonIds)
        {
            if (!rosterById.TryGetValue(personId, out var row))
                return ApplicationProfileInstanceExclusionResult.Failed("A selected person is no longer on this case roster.");
            if (row.IsExcluded)
                return ApplicationProfileInstanceExclusionResult.Failed(
                    $"{row.FullName} is already excluded by letter № {row.ExcludedLetterNumber}.");
        }

        var exclusion = objectSpace.CreateObject<ApplicationProfileInstanceExclusion>();
        exclusion.ApplicationProfileInstance = instance;
        exclusion.ApplicationProfileInstanceId = instance.ID;
        exclusion.CreatedOnUtc = DateTime.UtcNow;
        exclusion.CreatedByUserName = userName;
        ApplyDetails(objectSpace, instance, exclusion, draft);
        ApplicationProfileInstanceExclusionNumbering.Allocate(objectSpace, exclusion);

        var sequence = 0;
        foreach (var row in roster.Where(r => draft.PersonIds.Contains(r.PersonId)))
        {
            var line = objectSpace.CreateObject<ApplicationProfileInstanceExclusionPerson>();
            line.Exclusion = exclusion;
            line.ExclusionId = exclusion.ID;
            line.Person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            line.PersonId = row.PersonId;
            line.Sequence = ++sequence;
            line.FullName = row.FullName;
            line.PassportNumber = row.PassportNumber;
            exclusion.People.Add(line);
        }

        instance.Exclusions.Add(exclusion);

        var remaining = roster.Count(r => !r.IsExcluded && !draft.PersonIds.Contains(r.PersonId));
        return ApplicationProfileInstanceExclusionResult.Succeeded(exclusion.ID, allPeopleExcluded: remaining == 0);
    }

    public ApplicationProfileInstanceExclusionResult UpdateLetterDetails(
        IObjectSpace objectSpace,
        Guid exclusionId,
        ApplicationProfileInstanceExclusionDraft draft)
    {
        if (objectSpace == null)
            return ApplicationProfileInstanceExclusionResult.Failed("ObjectSpace is required.");

        var exclusion = objectSpace.GetObjectByKey<ApplicationProfileInstanceExclusion>(exclusionId);
        if (exclusion == null)
            return ApplicationProfileInstanceExclusionResult.Failed("Seretmezlik letter not found.");

        var detailsError = ValidateDetails(draft);
        if (detailsError != null)
            return ApplicationProfileInstanceExclusionResult.Failed(detailsError);

        ApplyDetails(objectSpace, exclusion.ApplicationProfileInstance, exclusion, draft);
        return ApplicationProfileInstanceExclusionResult.Succeeded(exclusion.ID);
    }

    public ApplicationProfileInstanceExclusionResult CloseAsCancelled(IObjectSpace objectSpace, Guid instanceId, DateTime date)
    {
        var result = _progress.Advance(
            objectSpace,
            instanceId,
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            notesOnLatestStep: null,
            stepDate: date);
        return result.Success
            ? ApplicationProfileInstanceExclusionResult.Succeeded()
            : ApplicationProfileInstanceExclusionResult.Failed(result.ErrorMessage ?? "The case could not be closed as Cancelled.");
    }

    public static ApplicationProfileInstanceExclusionDraft DraftFrom(ApplicationProfileInstanceExclusion exclusion)
    {
        var draft = new ApplicationProfileInstanceExclusionDraft
        {
            LetterDate = exclusion.LetterDate,
            AddresseeKey = exclusion.AddresseeKind switch
            {
                ApplicationProfileInstanceExclusionAddresseeKind.Ministry when exclusion.AddresseeLeg is int leg => MinistryKeyPrefix + leg,
                ApplicationProfileInstanceExclusionAddresseeKind.MigrationService
                    when exclusion.ApplicationProfileInstance?.MigrationService is MigrationService service => MigrationKeyPrefix + service.ID,
                _ => null,
            },
            AddresseeName = exclusion.AddresseeName,
            Salutation = exclusion.Salutation,
            ReferenceMinistryName = exclusion.ReferenceMinistryName,
            ReferenceLetterDate = exclusion.ReferenceLetterDate,
            ReferenceLetterNumber = exclusion.ReferenceLetterNumber,
            OriginalRosterCount = exclusion.OriginalRosterCount,
            Subject = exclusion.Subject,
        };
        foreach (var person in exclusion.People ?? Enumerable.Empty<ApplicationProfileInstanceExclusionPerson>())
            draft.PersonIds.Add(person.PersonId);
        return draft;
    }

    private static string? ValidateDetails(ApplicationProfileInstanceExclusionDraft draft)
    {
        if (draft.LetterDate == default)
            return "Letter date is required.";
        if (string.IsNullOrWhiteSpace(draft.AddresseeName))
            return "Addressee is required.";
        if (draft.OriginalRosterCount < 0)
            return "Original roster count cannot be negative.";
        return null;
    }

    private void ApplyDetails(
        IObjectSpace objectSpace,
        ApplicationProfileInstance? instance,
        ApplicationProfileInstanceExclusion exclusion,
        ApplicationProfileInstanceExclusionDraft draft)
    {
        exclusion.LetterDate = draft.LetterDate.Date;
        exclusion.AddresseeName = draft.AddresseeName.Trim();
        exclusion.Salutation = Trim(draft.Salutation);
        exclusion.ReferenceMinistryName = Trim(draft.ReferenceMinistryName);
        exclusion.ReferenceLetterDate = draft.ReferenceLetterDate?.Date;
        exclusion.ReferenceLetterNumber = Trim(draft.ReferenceLetterNumber);
        exclusion.OriginalRosterCount = draft.OriginalRosterCount;
        exclusion.Subject = Trim(draft.Subject);

        var option = instance == null || string.IsNullOrWhiteSpace(draft.AddresseeKey)
            ? null
            : GetAddresseeOptions(objectSpace, instance).FirstOrDefault(o => o.Key == draft.AddresseeKey);
        if (option != null)
        {
            exclusion.AddresseeKind = option.Kind;
            exclusion.AddresseeLeg = option.Leg;
        }
        else if (objectSpace.IsNewObject(exclusion) || !string.IsNullOrWhiteSpace(draft.AddresseeKey))
        {
            exclusion.AddresseeKind = ApplicationProfileInstanceExclusionAddresseeKind.Other;
            exclusion.AddresseeLeg = null;
        }
    }

    private static List<ApplicationProfileInstanceApprovalLegSnapshot> GetLegSnapshots(ApplicationProfileInstance instance) =>
        (instance.ApprovalLegSnapshots ?? new List<ApplicationProfileInstanceApprovalLegSnapshot>())
            .OrderBy(s => s.Sequence ?? int.MaxValue)
            .ToList();

    private static string BuildDefaultSubject(ApplicationProfileInstance instance)
    {
        var parts = new[]
            {
                instance.VisaPeriod?.NameTm,
                instance.VisaCategory?.NameTm,
                instance.ApplicationProfile?.Name,
            }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Trim());
        return string.Join(" ", parts);
    }

    private static Dictionary<Guid, string?> ResolvePassportNumbers(IObjectSpace objectSpace, ApplicationProfileInstance instance)
    {
        var linkedIds = objectSpace.GetObjectsQuery<ApplicationProfileInstancePersonResolvedLink>()
            .Where(l => l.ApplicationProfileInstanceId == instance.ID
                && l.LinkKind == ApplicationProfileInstancePersonLinkKind.Passport
                && l.LinkedObjectId != null)
            .Select(l => new { l.PersonId, PassportId = l.LinkedObjectId!.Value })
            .ToList();
        var passportIds = linkedIds.Select(l => l.PassportId).Distinct().ToList();
        var numbers = passportIds.Count == 0
            ? new Dictionary<Guid, string>()
            : objectSpace.GetObjectsQuery<Passport>()
                .Where(p => passportIds.Contains(p.ID))
                .Select(p => new { p.ID, p.PassportNumber })
                .ToList()
                .ToDictionary(p => p.ID, p => p.PassportNumber);

        var result = new Dictionary<Guid, string?>();
        foreach (var link in linkedIds)
        {
            if (numbers.TryGetValue(link.PassportId, out var number) && !string.IsNullOrWhiteSpace(number))
                result[link.PersonId] = number;
        }

        foreach (var person in instance.People ?? new List<Person>())
        {
            if (!result.ContainsKey(person.ID))
                result[person.ID] = ApplicationProfileInstancePersonValidItems.ResolvePassport(person)?.PassportNumber;
        }

        return result;
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
}

/// <summary>
/// Read-side lookups for workspace snapshot / other tabs.
/// Excluded (Seretmezlik) people are left out of Resminamalar, Document copies,
/// People &amp; links completeness, and Application Result expected / coverage counts.
/// </summary>
public static class ApplicationProfileInstanceExclusionQueries
{
    public static Dictionary<Guid, ApplicationProfileInstanceExcludedPerson> GetExcludedPeople(IObjectSpace? objectSpace, Guid instanceId)
    {
        var result = new Dictionary<Guid, ApplicationProfileInstanceExcludedPerson>();
        if (objectSpace == null || instanceId == Guid.Empty)
            return result;

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusionPerson>()
            .Where(p => p.Exclusion.ApplicationProfileInstanceId == instanceId)
            .Select(p => new
            {
                p.PersonId,
                p.ExclusionId,
                p.Exclusion.LetterNumber,
                p.Exclusion.LetterDate,
            })
            .ToList();

        foreach (var row in rows.OrderBy(r => r.LetterDate))
        {
            if (!result.ContainsKey(row.PersonId))
                result[row.PersonId] = new ApplicationProfileInstanceExcludedPerson(row.ExclusionId, row.LetterNumber, row.LetterDate);
        }

        return result;
    }

    /// <summary>
    /// Person ids on Seretmezlik letters for this instance.
    /// Prefers ObjectSpace query; falls back to in-memory <see cref="ApplicationProfileInstance.Exclusions"/>.
    /// </summary>
    public static HashSet<Guid> GetExcludedPersonIds(ApplicationProfileInstance? application, IObjectSpace? objectSpace)
    {
        var ids = new HashSet<Guid>();
        if (application == null)
            return ids;

        if (objectSpace != null && application.ID != Guid.Empty)
        {
            foreach (var id in GetExcludedPeople(objectSpace, application.ID).Keys)
                ids.Add(id);
            if (ids.Count > 0)
                return ids;
        }

        if (application.Exclusions == null)
            return ids;

        foreach (var exclusion in application.Exclusions)
        {
            if (exclusion?.People == null)
                continue;
            foreach (var row in exclusion.People)
            {
                if (row == null)
                    continue;
                if (row.PersonId != Guid.Empty)
                    ids.Add(row.PersonId);
                else if (row.Person != null && row.Person.ID != Guid.Empty)
                    ids.Add(row.Person.ID);
            }
        }

        return ids;
    }
}
