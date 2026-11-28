using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.ApplicationResultsDesk;

/// <summary>
/// Application results desk: search, the missing queue, and the people lists
/// for a work permit or a visa. The case workspace compose is not used here.
/// </summary>
public static class ApplicationResultsDeskService
{
    public const int TrackedLimit = 400;

    public static ApplicationResultsDeskPage Load(IObjectSpace objectSpace, ApplicationResultsDeskRequest request)
    {
        request ??= new ApplicationResultsDeskRequest();
        var state = string.IsNullOrWhiteSpace(request.StateKey)
            ? ApplicationResultsDeskStates.Missing
            : request.StateKey.Trim();
        var typeKey = request.TypeKey?.Trim() ?? string.Empty;

        var tracked = LoadTracked(objectSpace);
        if (tracked.Count > 0)
            ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(objectSpace, tracked);

        var page = new ApplicationResultsDeskPage
        {
            MissingCaseCount = CountMissingCases(tracked),
            Truncated = tracked.Count >= TrackedLimit,
            SearchedText = request.SearchText?.Trim() ?? string.Empty,
            TypeKey = typeKey,
            StateKey = state,
        };

        if (!string.IsNullOrWhiteSpace(page.SearchedText))
        {
            var hit = Find(objectSpace, page.SearchedText);
            if (hit == null)
            {
                page.SearchMiss = true;
            }
            else
            {
                if (tracked.All(a => a.ID != hit.ID))
                    ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(objectSpace, new[] { hit });
                page.Hit = ToCard(hit);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.HeaderKind))
        {
            page.Header = LoadHeader(objectSpace, request.PeopleInstanceId, request.HeaderKind, out var headerNote);
            page.Notice = headerNote;
            return page;
        }

        if (request.PeopleInstanceId != Guid.Empty)
        {
            page.People = LoadPeople(objectSpace, request.PeopleInstanceId, request.PeopleKind);
            if (request.DetailOpen)
            {
                page.Detail = LoadDetail(
                    objectSpace,
                    request.PeopleInstanceId,
                    request.PeopleKind,
                    request.DetailPersonId,
                    out var detailNote);
                page.Notice = detailNote;
            }
        }

        return page;
    }

    public static string SaveDetail(IObjectSpace objectSpace, ApplicationResultsDeskDetailSave save)
    {
        if (objectSpace == null || save == null)
            return "The record could not be saved.";

        if (string.Equals(save.Kind, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase))
            return SaveVisa(objectSpace, save);

        return SaveWorkPermitItem(objectSpace, save);
    }

    public static string SaveHeader(IObjectSpace objectSpace, ApplicationResultsDeskHeaderSave save)
    {
        if (objectSpace == null || save == null || save.InstanceId == Guid.Empty)
            return "The letter could not be saved.";
        if (!IssueIssuedHeaderComposeService.TryResolveKind(save.Kind, out var kind))
            return "That result cannot be edited here.";
        if (!TryParseDate(save.PrimaryDate, out var primary))
            return "Enter the date as dd.MM.yyyy.";

        DateTime? expiration = null;
        if (!string.IsNullOrWhiteSpace(save.Expiration))
        {
            if (!TryParseDate(save.Expiration, out var parsedEnd))
                return "Enter the end date as dd.MM.yyyy.";
            expiration = parsedEnd.Date;
        }

        var existing = FindHeaderId(objectSpace, save.InstanceId, kind);
        var draft = existing == Guid.Empty
            ? IssueIssuedHeaderComposeService.LoadDraft(objectSpace, save.InstanceId, kind)
            : IssueIssuedHeaderComposeService.LoadExistingDraft(objectSpace, save.InstanceId, kind, existing);
        if (draft == null)
            return "This case does not produce that result.";

        draft.HeaderNumber = save.Number?.Trim() ?? string.Empty;
        draft.PrimaryDate = primary.Date;
        draft.ExpirationDate = expiration;
        draft.Reason = save.Reason?.Trim() ?? string.Empty;
        draft.BorderZoneLocation = save.BorderZone?.Trim() ?? string.Empty;
        if (save.VisaCategoryId != Guid.Empty)
            draft.VisaCategoryId = save.VisaCategoryId;
        if (save.VisaPeriodId != Guid.Empty)
            draft.VisaPeriodId = save.VisaPeriodId;

        var included = new HashSet<Guid>(save.IncludedPersonIds ?? Array.Empty<Guid>());
        foreach (var person in draft.People)
        {
            if (person.IncludeLocked)
            {
                person.Include = true;
                continue;
            }

            person.Include = included.Contains(person.PersonId);
        }

        draft.SyncPeopleOnSave = true;
        var result = existing == Guid.Empty
            ? IssueIssuedHeaderComposeService.Create(objectSpace, draft)
            : IssueIssuedHeaderComposeService.Update(objectSpace, draft);
        return result.Succeeded ? null : result.ErrorMessage ?? "The letter could not be saved.";
    }

    public static string SaveWorkPermitLetter(IObjectSpace objectSpace, ApplicationResultsDeskLetterSave save)
    {
        if (objectSpace == null || save == null || save.InstanceId == Guid.Empty)
            return "The case could not be found.";

        var number = save.Number?.Trim() ?? string.Empty;
        if (number.Length == 0)
            return "Work permit number is required.";

        if (!TryParseDate(save.IssuedDate, out var issued))
            return "Issued date must be dd.MM.yyyy.";

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(save.InstanceId);
        if (instance == null)
            return "The case could not be found.";
        if (!ApplicationProfileConfigurationResolver.ShowWorkPermits(instance))
            return "This case does not produce a work permit.";
        if (string.IsNullOrWhiteSpace(ApplicationProcessNumberHelper.CopyForIssuedDocument(instance)))
            return "This case has no CO / AS number.";

        var letter = objectSpace.GetObjectsQuery<WorkPermit>()
            .FirstOrDefault(w => w.ApplicationProfileInstance != null && w.ApplicationProfileInstance.ID == instance.ID);
        if (letter == null)
        {
            letter = objectSpace.CreateObject<WorkPermit>();
            letter.ApplicationProfileInstance = instance;
        }

        letter.WorkPermitNumber = number;
        letter.IssuedDate = issued.Date;

        var created = EnsureLines(objectSpace, letter, instance);
        if (created.Error != null)
            return created.Error;

        objectSpace.CommitChanges();
        return null;
    }

    /// <summary>
    /// Returns the work-permit line to show. A new line is not saved until the detail is saved.
    /// </summary>
    public static string OpenWorkPermitItem(
        IObjectSpace objectSpace,
        Guid instanceId,
        Guid personId,
        out WorkPermitItem item)
    {
        item = null;
        if (objectSpace == null || instanceId == Guid.Empty)
            return "The case could not be found.";

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (instance == null)
            return "The case could not be found.";

        var letter = objectSpace.GetObjectsQuery<WorkPermit>()
            .FirstOrDefault(w => w.ApplicationProfileInstance != null && w.ApplicationProfileInstance.ID == instance.ID);
        if (letter == null || string.IsNullOrWhiteSpace(letter.WorkPermitNumber) || letter.IssuedDate == default)
            return "Save the work permit letter before opening a person.";

        if (personId == Guid.Empty)
        {
            var waiting = FirstEmployeeWithoutLine(objectSpace, instance, letter);
            if (waiting.Person == null)
                return waiting.Error ?? "Everyone on this case already has a work permit line.";
            personId = waiting.Person.ID;
        }

        item = objectSpace.GetObjectsQuery<WorkPermitItem>()
            .FirstOrDefault(i => i.WorkPermit != null
                && i.WorkPermit.ID == letter.ID
                && i.Person != null
                && i.Person.ID == personId);
        if (item != null)
            return null;

        var person = objectSpace.GetObjectByKey<Person>(personId);
        var blocked = BlockReason(person);
        if (blocked != null)
            return blocked;

        item = CreateLine(objectSpace, letter, instance, person, NextItemNumber(letter, objectSpace));
        return null;
    }

    /// <summary>
    /// Returns the visa to show. A new visa is not saved until the detail is saved.
    /// Values are copied from the invitation or the case and stay editable.
    /// </summary>
    public static string OpenVisa(
        IObjectSpace objectSpace,
        Guid instanceId,
        Guid personId,
        out Visa visa)
    {
        visa = null;
        if (objectSpace == null || instanceId == Guid.Empty)
            return "The case could not be found.";

        var draft = IssueIssuedVisaComposeService.LoadDraft(objectSpace, instanceId);
        if (draft == null)
            return "This case does not produce a visa.";

        var card = personId == Guid.Empty
            ? draft.AllPeople.FirstOrDefault(p => !p.AlreadyIssued && p.IsReady)
            : draft.AllPeople.FirstOrDefault(p => p.PersonId == personId);
        if (card == null)
            return personId == Guid.Empty
                ? "Everyone who can receive a visa on this case already has one."
                : "That person is not waiting for a visa on this case.";
        if (!card.IsReady || card.PassportId is not Guid)
            return card.PersonName + " has no passport.";

        if (card.ExistingVisaId is Guid existing && existing != Guid.Empty)
        {
            visa = objectSpace.GetObjectByKey<Visa>(existing);
            return visa == null ? "The visa could not be opened." : null;
        }

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (instance == null)
            return "The case could not be found.";

        visa = objectSpace.CreateObject<Visa>();
        visa.IssuingApplicationProfileInstance = instance;
        if (card.InvitationItemId != Guid.Empty)
        {
            visa.IssuingInvitationItem = objectSpace.GetObjectByKey<InvitationItem>(card.InvitationItemId);
            visa.PathAIssuingLinksApplied = visa.IssuingInvitationItem != null;
        }

        visa.Passport = objectSpace.GetObjectByKey<Passport>(card.PassportId.Value);
        ApplicationProcessNumberHelper.ApplyToVisa(visa, instance);
        if (card.VisaTypeId is Guid typeId && typeId != Guid.Empty)
            visa.VisaType = objectSpace.GetObjectByKey<VisaType>(typeId);
        if (card.VisaCategoryId is Guid categoryId && categoryId != Guid.Empty)
            visa.VisaCategory = objectSpace.GetObjectByKey<VisaCategory>(categoryId);
        if (card.VisaIssuedPlaceId is Guid placeId && placeId != Guid.Empty)
            visa.VisaIssuedPlace = objectSpace.GetObjectByKey<VisaIssuedPlace>(placeId);
        if (card.IssueDate != default)
            visa.IssueDate = card.IssueDate.Date;
        if (card.StartDate != default)
            visa.StartDate = card.StartDate.Date;
        if (card.ExpirationDate is DateTime expiration && expiration != default)
            visa.ExpirationDate = expiration.Date;
        if (!string.IsNullOrWhiteSpace(card.BorderZoneLocation))
            visa.BorderZoneLocation = card.BorderZoneLocation.Trim();
        BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);
        return null;
    }

    private static List<ApplicationProfileInstance> LoadTracked(IObjectSpace objectSpace)
    {
        return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ProcessNumber != null && a.ProcessNumber != "")
            .OrderByDescending(a => a.ApplicationDate)
            .ThenBy(a => a.FullApplicationNumber)
            .Take(TrackedLimit)
            .ToList();
    }

    private static ApplicationProfileInstance Find(IObjectSpace objectSpace, string term)
    {
        var lower = term.Trim().ToLowerInvariant();
        return objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .FirstOrDefault(a =>
                (a.ProcessNumber != null && a.ProcessNumber.ToLower() == lower)
                || (a.FullApplicationNumber != null && a.FullApplicationNumber.ToLower() == lower)
                || (a.ApplicationNumber != null && a.ApplicationNumber.ToLower() == lower));
    }

    private static int CountMissingCases(IReadOnlyList<ApplicationProfileInstance> tracked)
    {
        var ids = new HashSet<Guid>();
        foreach (var application in tracked)
        {
            if (ApplicationProfileConfigurationResolver.IsSeretmezlikInstance(application))
                continue;
            if (RequiredShort(application).Any())
                ids.Add(application.ID);
        }

        return ids.Count;
    }

    private static IReadOnlyList<ApplicationResultsDeskQueueRow> BuildRows(
        IObjectSpace objectSpace,
        IReadOnlyList<ApplicationProfileInstance> tracked,
        string state,
        string typeKey)
    {
        if (string.Equals(state, ApplicationResultsDeskStates.Seretmezlik, StringComparison.OrdinalIgnoreCase))
            return LoadSeretmezlik(objectSpace);

        if (string.Equals(state, ApplicationResultsDeskStates.Cancelled, StringComparison.OrdinalIgnoreCase))
            return LoadFamily(objectSpace, ApplicationProfileActionFamily.Cancellation, "Cancelled", typeKey);

        if (string.Equals(state, ApplicationResultsDeskStates.Changed, StringComparison.OrdinalIgnoreCase))
            return LoadFamily(objectSpace, ApplicationProfileActionFamily.Change, "Changed", typeKey);

        var rows = new List<ApplicationResultsDeskQueueRow>();
        foreach (var application in tracked)
        {
            if (ApplicationProfileConfigurationResolver.IsSeretmezlikInstance(application))
                continue;

            foreach (var chip in application.ListViewResultCoverageChips)
            {
                if (!string.IsNullOrEmpty(typeKey)
                    && !string.Equals(chip.Key, typeKey, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (!MatchesCoverageState(chip, state))
                    continue;

                rows.Add(CoverageRow(application, chip, state));
            }
        }

        return rows;
    }

    private static bool MatchesCoverageState(ApplicationWorkspaceIssuedResultListCoverage.Chip chip, string state)
    {
        if (string.Equals(state, ApplicationResultsDeskStates.Issued, StringComparison.OrdinalIgnoreCase))
            return chip.CoverageCount > 0;

        if (string.Equals(state, ApplicationResultsDeskStates.Valid, StringComparison.OrdinalIgnoreCase))
            return !chip.IsOptional && chip.ExpectedCount > 0 && chip.CoverageCount >= chip.ExpectedCount;

        return !chip.IsOptional && chip.ExpectedCount > 0 && chip.CoverageCount < chip.ExpectedCount;
    }

    private static IEnumerable<ApplicationWorkspaceIssuedResultListCoverage.Chip> RequiredShort(
        ApplicationProfileInstance application)
    {
        foreach (var chip in application.ListViewResultCoverageChips)
        {
            if (!chip.IsOptional && chip.ExpectedCount > 0 && chip.CoverageCount < chip.ExpectedCount)
                yield return chip;
        }
    }

    private static ApplicationResultsDeskQueueRow CoverageRow(
        ApplicationProfileInstance application,
        ApplicationWorkspaceIssuedResultListCoverage.Chip chip,
        string state)
    {
        var label = StateLabel(state, chip);
        return new ApplicationResultsDeskQueueRow
        {
            InstanceId = application.ID,
            ProcessNumber = application.ProcessNumber?.Trim() ?? string.Empty,
            ApplicationNumber = DisplayNumber(application),
            ProfileName = application.ApplicationProfile?.Name?.Trim() ?? string.Empty,
            ResultKey = chip.Key,
            ResultLabel = chip.CatalogLabel,
            CoverageText = ApplicationWorkspaceIssuedResultListCoverage.RatioText(chip),
            GapText = GapText(chip),
            StateKey = state,
            StateLabel = label,
            OpensOnDesk = OpensOnDesk(chip.Key)
                && !string.Equals(state, ApplicationResultsDeskStates.Valid, StringComparison.OrdinalIgnoreCase)
                && !chip.IsOptional
                && chip.CoverageCount < chip.ExpectedCount,
        };
    }

    private static string GapText(ApplicationWorkspaceIssuedResultListCoverage.Chip chip)
    {
        var name = GapName(chip.Key, chip.CatalogLabel);
        var missing = Math.Max(0, chip.ExpectedCount - chip.CoverageCount);
        if (chip.CoverageCount <= 0)
        {
            return missing == 1
                ? "No " + name + " yet — 1 person"
                : "No " + name + " yet — " + missing.ToString(CultureInfo.InvariantCulture) + " people";
        }

        var expected = chip.ExpectedCount.ToString(CultureInfo.InvariantCulture);
        var shortfall = missing.ToString(CultureInfo.InvariantCulture);
        return missing == 1
            ? "1 of " + expected + " people still has no " + name
            : shortfall + " of " + expected + " people still have no " + name;
    }

    private static string GapName(string key, string fallback)
    {
        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.Invitation, StringComparison.OrdinalIgnoreCase))
            return "invitation";
        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, StringComparison.OrdinalIgnoreCase))
            return "work permit";
        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase))
            return "visa";
        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.BorderZone, StringComparison.OrdinalIgnoreCase))
            return "border zone permit";
        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermitLocationGrant, StringComparison.OrdinalIgnoreCase))
            return "work location";
        if (string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.Rejection, StringComparison.OrdinalIgnoreCase))
            return "rejection";
        return string.IsNullOrWhiteSpace(fallback) ? "result" : fallback.Trim().ToLowerInvariant();
    }

    private static string StateLabel(string state, ApplicationWorkspaceIssuedResultListCoverage.Chip chip)
    {
        if (string.Equals(state, ApplicationResultsDeskStates.Issued, StringComparison.OrdinalIgnoreCase))
            return "Issued";
        if (string.Equals(state, ApplicationResultsDeskStates.Valid, StringComparison.OrdinalIgnoreCase))
            return "Valid";
        if (chip.IsOptional)
            return "Optional";
        return chip.CoverageCount >= chip.ExpectedCount && chip.ExpectedCount > 0 ? "Complete" : "Missing";
    }

    private static IReadOnlyList<ApplicationResultsDeskQueueRow> LoadSeretmezlik(IObjectSpace objectSpace)
    {
        var rows = new List<ApplicationResultsDeskQueueRow>();
        var submitted = ApplicationProfileInstanceProgressStateCodes.SubmittedToMigration;
        var apps = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ApplicationProfile != null
                && a.ApplicationProfile.RequireTargetApplicationProfileInstance
                && a.LatestPrimaryStateCode == submitted)
            .OrderByDescending(a => a.ApplicationDate)
            .Take(TrackedLimit)
            .ToList();

        foreach (var application in apps)
        {

            rows.Add(new ApplicationResultsDeskQueueRow
            {
                InstanceId = application.ID,
                ProcessNumber = "—",
                ApplicationNumber = DisplayNumber(application),
                ProfileName = application.ApplicationProfile?.Name?.Trim() ?? "Seretmezlik",
                ResultKey = ApplicationResultsDeskStates.Seretmezlik,
                ResultLabel = "Seretmezlik",
                CoverageText = "—",
                StateKey = ApplicationResultsDeskStates.Seretmezlik,
                StateLabel = "Submitted",
            });
        }

        return rows;
    }

    private static IReadOnlyList<ApplicationResultsDeskQueueRow> LoadFamily(
        IObjectSpace objectSpace,
        ApplicationProfileActionFamily family,
        string stateLabel,
        string typeKey)
    {
        var apps = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(a => a.ApplicationProfile != null && a.ApplicationProfile.ActionFamily == family)
            .OrderByDescending(a => a.ApplicationDate)
            .Take(TrackedLimit)
            .ToList();

        var rows = new List<ApplicationResultsDeskQueueRow>();
        foreach (var application in apps)
        {
            if (ApplicationProfileConfigurationResolver.IsSeretmezlikInstance(application))
                continue;

            var done = family == ApplicationProfileActionFamily.Change
                ? !string.IsNullOrWhiteSpace(application.ProcessNumber)
                : string.Equals(
                    application.LatestPrimaryStateCode,
                    ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
                    StringComparison.OrdinalIgnoreCase);
            if (!done)
                continue;

            var key = FamilyResultKey(application);
            if (!string.IsNullOrEmpty(typeKey)
                && !string.Equals(key, typeKey, StringComparison.OrdinalIgnoreCase))
                continue;

            rows.Add(new ApplicationResultsDeskQueueRow
            {
                InstanceId = application.ID,
                ProcessNumber = string.IsNullOrWhiteSpace(application.ProcessNumber) ? "—" : application.ProcessNumber.Trim(),
                ApplicationNumber = DisplayNumber(application),
                ProfileName = application.ApplicationProfile?.Name?.Trim() ?? string.Empty,
                ResultKey = key,
                ResultLabel = FamilyResultLabel(key, application),
                CoverageText = "—",
                StateKey = stateLabel.ToLowerInvariant(),
                StateLabel = stateLabel,
            });
        }

        return rows;
    }

    private static string FamilyResultKey(ApplicationProfileInstance application)
    {
        var code = application.ApplicationProfile?.Code?.Trim() ?? string.Empty;
        if (code.Contains("border", StringComparison.OrdinalIgnoreCase))
            return ApplicationWorkspaceIssuedRecordsCatalog.BorderZone;
        if (code.Contains("workpermit", StringComparison.OrdinalIgnoreCase)
            || code.Contains("work_permit", StringComparison.OrdinalIgnoreCase))
            return ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit;
        if (code.Contains("invitation", StringComparison.OrdinalIgnoreCase) || code.Contains("inv", StringComparison.OrdinalIgnoreCase))
            return ApplicationWorkspaceIssuedRecordsCatalog.Invitation;
        if (code.Contains("visa", StringComparison.OrdinalIgnoreCase))
            return ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa;
        if (code.Contains("location", StringComparison.OrdinalIgnoreCase))
            return ApplicationWorkspaceIssuedRecordsCatalog.WorkPermitLocationGrant;
        return string.Empty;
    }

    private static string FamilyResultLabel(string key, ApplicationProfileInstance application)
    {
        if (ApplicationWorkspaceIssuedRecordsCatalog.TryGet(key, out var definition))
            return definition.Label;
        return application.ApplicationProfile?.Name?.Trim() ?? string.Empty;
    }

    private static ApplicationResultsDeskCaseCard ToCard(ApplicationProfileInstance application)
    {
        var types = new List<ApplicationResultsDeskTypeCard>();
        foreach (var chip in application.ListViewResultCoverageChips)
        {
            var complete = !chip.IsOptional && chip.ExpectedCount > 0 && chip.CoverageCount >= chip.ExpectedCount;
            types.Add(new ApplicationResultsDeskTypeCard
            {
                Key = chip.Key,
                Label = chip.CatalogLabel,
                Coverage = chip.CoverageCount,
                Expected = chip.ExpectedCount,
                Optional = chip.IsOptional,
                Complete = complete,
                OpensOnDesk = OpensOnDesk(chip.Key),
                Ratio = chip.IsOptional
                    ? chip.CoverageCount.ToString(CultureInfo.InvariantCulture)
                    : chip.CoverageCount + " / " + chip.ExpectedCount,
            });
        }

        return new ApplicationResultsDeskCaseCard
        {
            InstanceId = application.ID,
            ProcessNumber = application.ProcessNumber?.Trim() ?? string.Empty,
            ApplicationNumber = DisplayNumber(application),
            ProfileName = application.ApplicationProfile?.Name?.Trim() ?? string.Empty,
            PeopleCount = Math.Max(0, application.TotalPersonCount),
            HasProcessNumber = !string.IsNullOrWhiteSpace(application.ProcessNumber),
            Types = types,
        };
    }

    private static bool OpensOnDesk(string key) =>
        string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, StringComparison.OrdinalIgnoreCase)
        || string.Equals(key, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase);

    private static ApplicationResultsDeskPeoplePage LoadPeople(IObjectSpace objectSpace, Guid instanceId, string kind)
    {
        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (instance == null)
            return null;

        if (string.Equals(kind, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase))
            return LoadVisaPeople(objectSpace, instance);

        return LoadWorkPermitPeople(objectSpace, instance);
    }

    private static ApplicationResultsDeskPeoplePage LoadWorkPermitPeople(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance)
    {
        var letter = objectSpace.GetObjectsQuery<WorkPermit>()
            .FirstOrDefault(w => w.ApplicationProfileInstance != null && w.ApplicationProfileInstance.ID == instance.ID);
        var ready = letter != null
            && !string.IsNullOrWhiteSpace(letter.WorkPermitNumber)
            && letter.IssuedDate != default;
        var page = Head(instance, ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit, "Work permits");
        page.LetterReady = ready;
        page.LetterId = letter?.ID ?? Guid.Empty;
        page.LetterNumber = letter?.WorkPermitNumber?.Trim() ?? string.Empty;
        page.LetterIssued = ready ? letter.IssuedDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) : string.Empty;
        page.Note = ready
            ? "One letter for this case. Click a row to open that person's work permit detail. The AS number is the case CO / AS number."
            : "Save the letter number and issued date first. The copy is uploaded from the letter.";

        var rows = new List<ApplicationResultsDeskPersonRow>();
        foreach (var person in Employees(instance))
        {
            var line = letter?.WorkPermitItems?.FirstOrDefault(i => i.Person != null && i.Person.ID == person.ID);
            var blocked = line == null ? BlockReason(person) : null;
            rows.Add(new ApplicationResultsDeskPersonRow
            {
                PersonId = person.ID,
                RecordId = line?.ID ?? Guid.Empty,
                PersonName = person.FullName,
                Passport = line?.Passport?.PassportNumber?.Trim()
                    ?? ApplicationProfileInstancePersonValidItems.ResolvePassport(person)?.PassportNumber?.Trim()
                    ?? string.Empty,
                Number = line?.WorkPermitNumber?.Trim() ?? string.Empty,
                Detail = line?.CurrentPositionHistory?.ToString()
                    ?? PersonCurrentItems.GetCurrentPositionHistory(person)?.ToString()
                    ?? string.Empty,
                Start = FormatDate(line?.StartDate ?? default),
                End = FormatDate(line?.ExpirationDate ?? default),
                Locations = line?.WorkPermittedLocations?.Trim() ?? string.Empty,
                CanOpen = ready && blocked == null,
                BlockReason = ready ? blocked ?? string.Empty : "Save the letter first.",
            });
        }

        page.Rows = rows;
        return page;
    }

    private static ApplicationResultsDeskPeoplePage LoadVisaPeople(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance)
    {
        var page = Head(instance, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, "Visas");
        page.LetterReady = true;
        var draft = IssueIssuedVisaComposeService.LoadDraft(objectSpace, instance.ID);
        if (draft == null)
        {
            page.Note = "This case does not produce a visa.";
            return page;
        }

        page.Note = draft.UsesInvitationSource
            ? "One row per person who already has an invitation on this case. Click a row to open the visa detail. Type, category, place, and dates are copied and stay editable."
            : "One row per person on this case. Click a row to open the visa detail. Values are copied from the case and stay editable.";
        if (draft.PeopleWithoutIssuedInvitation.Count > 0)
            page.Note += " People with no invitation on this case are omitted.";

        var rows = new List<ApplicationResultsDeskPersonRow>();
        foreach (var card in draft.AllPeople)
        {
            rows.Add(new ApplicationResultsDeskPersonRow
            {
                PersonId = card.PersonId,
                RecordId = card.ExistingVisaId ?? Guid.Empty,
                PersonName = card.PersonName,
                Passport = card.PassportNumber,
                Number = card.AlreadyIssued ? card.ExistingVisaNumber ?? card.VisaNumber : string.Empty,
                Detail = card.InvitationNumber,
                Start = FormatDate(card.IssueDate),
                End = card.ExpirationDate is DateTime end ? FormatDate(end) : string.Empty,
                CanOpen = card.IsReady,
                BlockReason = card.IsReady ? string.Empty : card.PersonName + " has no passport.",
            });
        }

        page.Rows = rows;
        return page;
    }

    private static ApplicationResultsDeskPeoplePage Head(ApplicationProfileInstance instance, string kind, string title) =>
        new()
        {
            InstanceId = instance.ID,
            Kind = kind,
            Title = title,
            ProcessNumber = instance.ProcessNumber?.Trim() ?? string.Empty,
            ApplicationNumber = DisplayNumber(instance),
            ProfileName = instance.ApplicationProfile?.Name?.Trim() ?? string.Empty,
        };

    private static (string Error, int Count) EnsureLines(
        IObjectSpace objectSpace,
        WorkPermit letter,
        ApplicationProfileInstance instance)
    {
        var already = (letter.WorkPermitItems?.Count ?? 0) > 0
            || (letter.ID != Guid.Empty
                && objectSpace.GetObjectsQuery<WorkPermitItem>()
                    .Any(i => i.WorkPermit != null && i.WorkPermit.ID == letter.ID));
        if (already)
            return (null, letter.WorkPermitItems?.Count ?? 0);

        var index = 1;
        var created = 0;
        foreach (var person in Employees(instance))
        {
            if (BlockReason(person) != null)
                continue;

            CreateLine(objectSpace, letter, instance, person, letter.WorkPermitNumber.Trim() + "-" + index.ToString(CultureInfo.InvariantCulture));
            index++;
            created++;
        }

        if (created == 0)
            return ("No employee on this case has a passport and a position, so the letter was not saved.", 0);

        return (null, created);
    }

    private static WorkPermitItem CreateLine(
        IObjectSpace objectSpace,
        WorkPermit letter,
        ApplicationProfileInstance instance,
        Person person,
        string itemNumber)
    {
        var item = objectSpace.CreateObject<WorkPermitItem>();
        item.WorkPermit = letter;
        item.Person = person;
        item.Passport = ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
        item.CurrentPositionHistory = PersonCurrentItems.GetCurrentPositionHistory(person);
        item.WorkPermitNumber = itemNumber;
        item.ASNumber = ApplicationProcessNumberHelper.CopyForIssuedDocument(instance);
        item.WorkPermittedLocations = string.IsNullOrWhiteSpace(instance.MovementPermitLocation)
            ? "Ýok"
            : instance.MovementPermitLocation.Trim();

        var visa = PersonCurrentItems.GetCurrentVisa(person);
        if (visa != null && visa.ExpirationDate.HasValue && visa.ExpirationDate.Value.Date > visa.StartDate.Date
            && visa.ExpirationDate.Value.Date >= DateTime.Today)
        {
            item.StartDate = visa.StartDate.Date;
            item.ExpirationDate = visa.ExpirationDate.Value.Date;
        }
        else
        {
            item.StartDate = letter.IssuedDate.Date;
            item.ExpirationDate = letter.IssuedDate.Date.AddYears(1);
        }

        letter.WorkPermitItems ??= new System.Collections.ObjectModel.ObservableCollection<WorkPermitItem>();
        if (!letter.WorkPermitItems.Contains(item))
            letter.WorkPermitItems.Add(item);
        return item;
    }

    private static (Person Person, string Error) FirstEmployeeWithoutLine(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        WorkPermit letter)
    {
        foreach (var person in Employees(instance))
        {
            var exists = objectSpace.GetObjectsQuery<WorkPermitItem>()
                .Any(i => i.WorkPermit != null && i.WorkPermit.ID == letter.ID && i.Person != null && i.Person.ID == person.ID);
            if (exists)
                continue;
            var blocked = BlockReason(person);
            if (blocked != null)
                continue;
            return (person, null);
        }

        return (null, "Everyone on this case already has a work permit line.");
    }

    private static string NextItemNumber(WorkPermit letter, IObjectSpace objectSpace)
    {
        var count = objectSpace.GetObjectsQuery<WorkPermitItem>().Count(i => i.WorkPermit != null && i.WorkPermit.ID == letter.ID);
        return letter.WorkPermitNumber.Trim() + "-" + (count + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static IEnumerable<Person> Employees(ApplicationProfileInstance instance) =>
        ApplicationRosterHelper.GetRosterPeople(instance)
            .Where(p => p != null && p.IsEmployee && p.ID != Guid.Empty)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName);

    private static string BlockReason(Person person)
    {
        if (person == null)
            return "The person could not be found.";
        if (ApplicationProfileInstancePersonValidItems.ResolvePassport(person) == null)
            return person.FullName + " has no passport.";
        if (PersonCurrentItems.GetCurrentPositionHistory(person) == null)
            return person.FullName + " has no position.";
        return null;
    }

    private static string DisplayNumber(ApplicationProfileInstance application)
    {
        if (!string.IsNullOrWhiteSpace(application.FullApplicationNumber))
            return application.FullApplicationNumber.Trim();
        return application.ApplicationNumber?.Trim() ?? string.Empty;
    }

    private static ApplicationResultsDeskDetail LoadDetail(
        IObjectSpace objectSpace,
        Guid instanceId,
        string kind,
        Guid personId,
        out string notice)
    {
        notice = null;
        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (instance == null)
        {
            notice = "The case could not be found.";
            return null;
        }

        if (string.Equals(kind, ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa, StringComparison.OrdinalIgnoreCase))
        {
            var error = OpenVisa(objectSpace, instanceId, personId, out var visa);
            if (error != null || visa == null)
            {
                notice = error ?? "The visa could not be opened.";
                return null;
            }

            return new ApplicationResultsDeskDetail
            {
                Kind = ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa,
                IsVisa = true,
                InstanceId = instanceId,
                PersonId = visa.Passport?.Person?.ID ?? personId,
                RecordId = visa.ID,
                Title = "Visa",
                PersonName = visa.Passport?.Person?.FullName ?? string.Empty,
                Passport = visa.Passport?.PassportNumber?.Trim() ?? string.Empty,
                ProcessNumber = visa.ProcessNumber?.Trim() ?? instance.ProcessNumber?.Trim() ?? string.Empty,
                ApplicationNumber = DisplayNumber(instance),
                ProfileName = instance.ApplicationProfile?.Name?.Trim() ?? string.Empty,
                VisaNumber = visa.VisaNumber?.Trim() ?? string.Empty,
                VisaTypeId = visa.VisaType?.ID ?? Guid.Empty,
                VisaCategoryId = visa.VisaCategory?.ID ?? Guid.Empty,
                VisaIssuedPlaceId = visa.VisaIssuedPlace?.ID ?? Guid.Empty,
                Issued = FormatDate(visa.IssueDate),
                Start = FormatDate(visa.StartDate),
                End = visa.ExpirationDate is DateTime end ? FormatDate(end) : string.Empty,
                BorderZone = visa.BorderZoneLocation?.Trim() ?? string.Empty,
                InvitationNumber = visa.IssuingInvitationItem?.Invitation?.InvitationNumber?.Trim() ?? string.Empty,
                VisaTypes = LookupCaptions(objectSpace.GetObjectsQuery<VisaType>().ToList(), row => row.ID, row => row.ToString()),
                VisaCategories = LookupCaptions(objectSpace.GetObjectsQuery<VisaCategory>().ToList(), row => row.ID, row => row.ToString()),
                IssuedPlaces = LookupCaptions(objectSpace.GetObjectsQuery<VisaIssuedPlace>().ToList(), row => row.ID, row => row.ToString()),
            };
        }

        var lineError = OpenWorkPermitItem(objectSpace, instanceId, personId, out var item);
        if (lineError != null || item == null)
        {
            notice = lineError ?? "The work permit line could not be opened.";
            return null;
        }

        return new ApplicationResultsDeskDetail
        {
            Kind = ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit,
            InstanceId = instanceId,
            PersonId = item.Person?.ID ?? personId,
            RecordId = item.ID,
            Title = "Work permit",
            PersonName = item.Person?.FullName ?? string.Empty,
            Passport = item.Passport?.PassportNumber?.Trim() ?? string.Empty,
            ProcessNumber = instance.ProcessNumber?.Trim() ?? string.Empty,
            ApplicationNumber = DisplayNumber(instance),
            ProfileName = instance.ApplicationProfile?.Name?.Trim() ?? string.Empty,
            ItemNumber = item.WorkPermitNumber?.Trim() ?? string.Empty,
            LetterNumber = item.WorkPermit?.WorkPermitNumber?.Trim() ?? string.Empty,
            Position = item.CurrentPositionHistory?.ToString() ?? string.Empty,
            AsNumber = item.ASNumber?.Trim() ?? string.Empty,
            Start = FormatDate(item.StartDate),
            End = FormatDate(item.ExpirationDate),
            Locations = item.WorkPermittedLocations?.Trim() ?? string.Empty,
        };
    }

    private static string SaveWorkPermitItem(IObjectSpace objectSpace, ApplicationResultsDeskDetailSave save)
    {
        if (string.IsNullOrWhiteSpace(save.ItemNumber))
            return "Enter the item work-permit number.";
        if (!TryParseDate(save.Start, out var start) || !TryParseDate(save.End, out var end))
            return "Enter the start and end as dd.MM.yyyy.";
        if (end.Date <= start.Date)
            return "The end date must be after the start date.";
        if (string.IsNullOrWhiteSpace(save.Locations))
            return "Enter the work permitted locations.";

        var error = OpenWorkPermitItem(objectSpace, save.InstanceId, save.PersonId, out var item);
        if (error != null || item == null)
            return error ?? "The work permit line could not be opened.";

        item.WorkPermitNumber = save.ItemNumber.Trim();
        item.StartDate = start.Date;
        item.ExpirationDate = end.Date;
        item.WorkPermittedLocations = save.Locations.Trim();
        objectSpace.CommitChanges();
        return null;
    }

    private static string SaveVisa(IObjectSpace objectSpace, ApplicationResultsDeskDetailSave save)
    {
        if (string.IsNullOrWhiteSpace(save.VisaNumber))
            return "Enter the visa number.";
        if (save.VisaTypeId == Guid.Empty || save.VisaCategoryId == Guid.Empty || save.VisaIssuedPlaceId == Guid.Empty)
            return "Choose the visa type, category, and issued place.";
        if (!TryParseDate(save.Issued, out var issued) || !TryParseDate(save.Start, out var start) || !TryParseDate(save.End, out var end))
            return "Enter the issued, start, and expiration dates as dd.MM.yyyy.";
        if (start.Date < issued.Date)
            return "The start date cannot be before the issued date.";
        if (end.Date <= start.Date)
            return "The expiration must be after the start date.";
        if (string.IsNullOrWhiteSpace(save.BorderZone))
            return "Enter the border zone.";

        var error = OpenVisa(objectSpace, save.InstanceId, save.PersonId, out var visa);
        if (error != null || visa == null)
            return error ?? "The visa could not be opened.";

        visa.VisaNumber = save.VisaNumber.Trim();
        visa.VisaType = objectSpace.GetObjectByKey<VisaType>(save.VisaTypeId);
        visa.VisaCategory = objectSpace.GetObjectByKey<VisaCategory>(save.VisaCategoryId);
        visa.VisaIssuedPlace = objectSpace.GetObjectByKey<VisaIssuedPlace>(save.VisaIssuedPlaceId);
        visa.IssueDate = issued.Date;
        visa.StartDate = start.Date;
        visa.ExpirationDate = end.Date;
        visa.BorderZoneLocation = save.BorderZone.Trim();
        objectSpace.CommitChanges();
        return null;
    }

    private static ApplicationResultsDeskHeaderForm LoadHeader(
        IObjectSpace objectSpace,
        Guid instanceId,
        string catalogKey,
        out string notice)
    {
        notice = null;
        if (!IssueIssuedHeaderComposeService.TryResolveKind(catalogKey, out var kind)
            || kind == IssueIssuedHeaderKind.WorkPermit)
        {
            notice = "That result cannot be edited here.";
            return null;
        }

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(instanceId);
        if (instance == null)
        {
            notice = "The case could not be found.";
            return null;
        }

        var existing = FindHeaderId(objectSpace, instanceId, kind);
        var draft = existing == Guid.Empty
            ? IssueIssuedHeaderComposeService.LoadDraft(objectSpace, instanceId, kind)
            : IssueIssuedHeaderComposeService.LoadExistingDraft(objectSpace, instanceId, kind, existing);
        if (draft == null)
        {
            notice = "This case does not produce that result.";
            return null;
        }

        return new ApplicationResultsDeskHeaderForm
        {
            InstanceId = instanceId,
            Kind = catalogKey,
            Title = draft.Title,
            ProcessNumber = instance.ProcessNumber?.Trim() ?? string.Empty,
            ApplicationNumber = DisplayNumber(instance),
            ProfileName = instance.ApplicationProfile?.Name?.Trim() ?? string.Empty,
            IsEdit = existing != Guid.Empty,
            Number = draft.HeaderNumber?.Trim() ?? string.Empty,
            PrimaryDate = FormatDate(draft.PrimaryDate),
            Expiration = draft.ExpirationDate is DateTime end ? FormatDate(end) : string.Empty,
            Reason = draft.Reason?.Trim() ?? string.Empty,
            BorderZone = draft.BorderZoneLocation?.Trim() ?? string.Empty,
            VisaCategoryId = draft.VisaCategoryId ?? Guid.Empty,
            VisaPeriodId = draft.VisaPeriodId ?? Guid.Empty,
            ShowReason = kind == IssueIssuedHeaderKind.Rejection,
            ShowBorder = kind == IssueIssuedHeaderKind.BorderZone,
            ShowVisaLookups = kind == IssueIssuedHeaderKind.Invitation,
            ShowExpiration = kind != IssueIssuedHeaderKind.Rejection,
            VisaCategories = kind == IssueIssuedHeaderKind.Invitation
                ? OptionsFrom(draft.VisaCategories)
                : Array.Empty<ApplicationResultsDeskOption>(),
            VisaPeriods = kind == IssueIssuedHeaderKind.Invitation
                ? OptionsFrom(draft.VisaPeriods)
                : Array.Empty<ApplicationResultsDeskOption>(),
            People = draft.People.Select(person => new ApplicationResultsDeskHeaderPerson
            {
                PersonId = person.PersonId,
                Name = person.PersonName,
                Passport = person.PassportNumber,
                Status = person.StatusCaption,
                Include = person.Include,
                Ready = person.IsReady,
                Locked = person.IncludeLocked,
            }).ToList(),
        };
    }

    private static Guid FindHeaderId(IObjectSpace objectSpace, Guid instanceId, IssueIssuedHeaderKind kind)
    {
        return kind switch
        {
            IssueIssuedHeaderKind.Invitation => objectSpace.GetObjectsQuery<Invitation>()
                .Where(row => row.ApplicationProfileInstance != null && row.ApplicationProfileInstance.ID == instanceId)
                .Select(row => row.ID)
                .FirstOrDefault(),
            IssueIssuedHeaderKind.Rejection => objectSpace.GetObjectsQuery<Rejection>()
                .Where(row => row.ApplicationProfileInstance != null && row.ApplicationProfileInstance.ID == instanceId)
                .Select(row => row.ID)
                .FirstOrDefault(),
            IssueIssuedHeaderKind.BorderZone => objectSpace.GetObjectsQuery<BorderZone>()
                .Where(row => row.ApplicationProfileInstance != null && row.ApplicationProfileInstance.ID == instanceId)
                .Select(row => row.ID)
                .FirstOrDefault(),
            IssueIssuedHeaderKind.WorkPermitLocationGrant => objectSpace.GetObjectsQuery<WorkPermitLocationGrant>()
                .Where(row => row.ApplicationProfileInstance != null && row.ApplicationProfileInstance.ID == instanceId)
                .Select(row => row.ID)
                .FirstOrDefault(),
            _ => Guid.Empty,
        };
    }

    private static IReadOnlyList<ApplicationResultsDeskOption> LookupCaptions<TLookup>(
        IEnumerable<TLookup> rows,
        Func<TLookup, Guid> id,
        Func<TLookup, string> caption)
    {
        return rows
            .OrderBy(row => caption(row), StringComparer.OrdinalIgnoreCase)
            .Select(row => new ApplicationResultsDeskOption
            {
                Id = id(row),
                Caption = caption(row) ?? string.Empty,
            })
            .ToList();
    }

    private static IReadOnlyList<ApplicationResultsDeskOption> OptionsFrom(
        IReadOnlyList<IssueIssuedHeaderLookupOption> options)
    {
        if (options == null)
            return Array.Empty<ApplicationResultsDeskOption>();

        return options.Select(option => new ApplicationResultsDeskOption
        {
            Id = option.Id,
            Caption = option.Caption,
        }).ToList();
    }

    private static string FormatDate(DateTime value) =>
        value == default ? string.Empty : value.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    private static bool TryParseDate(string text, out DateTime value)
    {
        return DateTime.TryParseExact(
            text?.Trim(),
            "dd.MM.yyyy",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out value);
    }
}
