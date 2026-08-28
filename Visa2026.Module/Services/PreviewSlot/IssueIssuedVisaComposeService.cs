using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.PreviewSlot;

/// <summary>
/// Issued-visa compose from case workspace Issued records.
/// Invitation+visa: people from issued invitation lines (never input M2M InvitationItems).
/// Visa without invitation: people from the case roster (never skip-nav Visas M2M).
/// </summary>
public static class IssueIssuedVisaComposeService
{
    public static bool CanOpenInSlot(ApplicationProfileInstance? application)
    {
        if (application == null)
            return false;

        return ApplicationProfileConfigurationResolver.ShowIssuedVisas(application);
    }

    public static bool UsesInvitationSource(ApplicationProfileInstance? application) =>
        CanOpenInSlot(application)
        && ApplicationProfileConfigurationResolver.CanIssueInvitation(application);

    public static IssueIssuedVisaComposeDraft? LoadDraft(IObjectSpace objectSpace, Guid applicationId)
    {
        if (objectSpace == null || applicationId == Guid.Empty)
            return null;

        var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(applicationId);
        if (instance == null || !CanOpenInSlot(instance))
            return null;

        return UsesInvitationSource(instance)
            ? LoadInvitationSourceDraft(objectSpace, instance)
            : LoadRosterSourceDraft(objectSpace, instance);
    }

    private static IssueIssuedVisaComposeDraft LoadInvitationSourceDraft(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance)
    {
        var applicationId = instance.ID;
        var invitations = objectSpace.GetObjectsQuery<Invitation>()
            .Where(i => i.ApplicationProfileInstance != null && i.ApplicationProfileInstance.ID == applicationId)
            .OrderBy(i => i.IssuedDate)
            .ThenBy(i => i.InvitationNumber)
            .ToList();

        var invitationIds = invitations.Select(i => i.ID).ToList();
        var items = invitationIds.Count == 0
            ? new List<InvitationItem>()
            : IssuedDocumentLifecycle.WhereInvitationItemNotChanged(
                IssuedDocumentLifecycle.WhereInvitationItemNotCancelled(
                    objectSpace.GetObjectsQuery<InvitationItem>()
                        .Where(ii => ii.Invitation != null && invitationIds.Contains(ii.Invitation.ID))))
                .ToList();

        var itemIds = items.Select(ii => ii.ID).ToList();
        var visaByItemId = itemIds.Count == 0
            ? new Dictionary<Guid, Visa>()
            : objectSpace.GetObjectsQuery<Visa>()
                .Where(v => v.IssuingInvitationItem != null && itemIds.Contains(v.IssuingInvitationItem.ID))
                .ToList()
                .Where(v => v.IssuingInvitationItem != null)
                .GroupBy(v => v.IssuingInvitationItem!.ID)
                .ToDictionary(g => g.Key, g => g.First());

        var visaTypes = LoadLookups<VisaType>(objectSpace);
        var visaCategories = LoadLookups<VisaCategory>(objectSpace);
        var visaPeriods = LoadLookups<VisaPeriod>(objectSpace);
        var visaIssuedPlaces = LoadLookups<VisaIssuedPlace>(objectSpace);
        var defaultPlaceId = objectSpace.GetObjectsQuery<VisaIssuedPlace>().FirstOrDefault(p => p.IsDefault)?.ID
            ?? visaIssuedPlaces.FirstOrDefault()?.Id;
        var borderZoneNames = CommaSeparatedCatalogHelper.LoadCatalogNames(
            objectSpace,
            typeof(BorderZoneName),
            CommaSeparatedSelectionHelper.NoneValue);

        var defaultTypeId = instance.VisaType?.ID
            ?? instance.ApplicationProfile?.DefaultVisaType?.ID
            ?? visaTypes.FirstOrDefault()?.Id;
        var issuedPersonIds = new HashSet<Guid>();
        var groups = new List<IssueIssuedVisaInvitationGroupDraft>();
        foreach (var invitation in invitations)
        {
            var letterItems = items
                .Where(ii => ii.Invitation != null && ii.Invitation.ID == invitation.ID)
                .OrderBy(ii => ii.Person != null ? ii.Person.LastName : string.Empty)
                .ThenBy(ii => ii.Person != null ? ii.Person.FirstName : string.Empty)
                .ToList();
            if (letterItems.Count == 0)
                continue;

            var people = new List<IssueIssuedVisaPersonCardDraft>();
            foreach (var item in letterItems)
            {
                if (item.Person == null)
                    continue;

                issuedPersonIds.Add(item.Person.ID);
                visaByItemId.TryGetValue(item.ID, out var existingVisa);
                existingVisa ??= item.IssuedVisa;
                var already = item.IsUsed || existingVisa != null;
                var passport = existingVisa?.Passport
                    ?? item.Passport
                    ?? ApplicationProfileInstancePersonValidItems.ResolvePassport(item.Person);
                var issueDate = already && existingVisa != null && existingVisa.IssueDate != default
                    ? existingVisa.IssueDate.Date
                    : DefaultIssueDate(instance, invitation.IssuedDate);
                DateTime? expiration;
                if (already && existingVisa?.ExpirationDate is DateTime existingExp && existingExp != default)
                    expiration = existingExp.Date;
                else if (invitation.ExpirationDate is DateTime invExp && invExp != default && invExp.Date > issueDate.Date)
                    expiration = invExp.Date;
                else
                    expiration = issueDate.AddMonths(6);

                people.Add(new IssueIssuedVisaPersonCardDraft
                {
                    InvitationId = invitation.ID,
                    InvitationNumber = FormatInvitationNumber(invitation),
                    InvitationIssuedDate = invitation.IssuedDate.Date,
                    InvitationItemId = item.ID,
                    PersonId = item.Person.ID,
                    PersonName = item.Person.FullName?.Trim() ?? item.Person.ToString() ?? string.Empty,
                    PassportId = passport?.ID,
                    PassportNumber = passport?.PassportNumber?.Trim() ?? string.Empty,
                    Include = !already && passport != null,
                    AlreadyIssued = already,
                    IsReady = passport != null,
                    ExistingVisaId = existingVisa?.ID,
                    ExistingVisaNumber = already ? existingVisa?.VisaNumber?.Trim() : null,
                    VisaNumber = already ? existingVisa?.VisaNumber?.Trim() ?? string.Empty : string.Empty,
                    VisaTypeId = already
                        ? existingVisa?.VisaType?.ID ?? defaultTypeId
                        : defaultTypeId,
                    VisaCategoryId = already
                        ? existingVisa?.VisaCategory?.ID ?? invitation.VisaCategory?.ID
                        : invitation.VisaCategory?.ID
                            ?? instance.VisaCategory?.ID
                            ?? visaCategories.FirstOrDefault()?.Id,
                    VisaPeriodId = invitation.VisaPeriod?.ID
                        ?? instance.VisaPeriod?.ID
                        ?? visaPeriods.FirstOrDefault()?.Id,
                    VisaIssuedPlaceId = already
                        ? existingVisa?.VisaIssuedPlace?.ID ?? defaultPlaceId
                        : defaultPlaceId,
                    IssueDate = issueDate,
                    ExpirationDate = expiration,
                    BorderZoneLocation = already
                        ? existingVisa?.BorderZoneLocation?.Trim()
                            ?? BorderZoneSelectionHelper.ResolveForIssuedVisa(invitation, instance)
                        : BorderZoneSelectionHelper.ResolveForIssuedVisa(invitation, instance),
                    Documents = already && existingVisa != null
                        ? ListDocuments(objectSpace, existingVisa.ID).ToList()
                        : [],
                });
            }

            if (people.Count == 0)
                continue;

            groups.Add(new IssueIssuedVisaInvitationGroupDraft
            {
                InvitationId = invitation.ID,
                InvitationNumber = FormatInvitationNumber(invitation),
                InvitationIssuedDate = invitation.IssuedDate.Date,
                People = people,
            });
        }

        var omitted = ApplicationRosterHelper.GetRosterPeople(instance)
            .Where(p => p != null && !issuedPersonIds.Contains(p.ID))
            .OrderBy(p => p!.LastName)
            .ThenBy(p => p!.FirstName)
            .Select(p => p!.FullName?.Trim() ?? p.ToString() ?? string.Empty)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();

        return new IssueIssuedVisaComposeDraft
        {
            ApplicationProfileInstanceId = applicationId,
            ApplicationCaption = instance.FullApplicationNumber?.Trim()
                ?? instance.ApplicationNumber?.ToString()
                ?? applicationId.ToString("N")[..8],
            ProfileCode = instance.ApplicationProfile?.Code?.Trim() ?? string.Empty,
            UsesInvitationSource = true,
            PeopleWithoutIssuedInvitation = omitted,
            VisaTypes = visaTypes,
            VisaCategories = visaCategories,
            VisaPeriods = visaPeriods,
            VisaIssuedPlaces = visaIssuedPlaces,
            BorderZoneNames = borderZoneNames,
            Groups = groups,
        };
    }

    private static IssueIssuedVisaComposeDraft LoadRosterSourceDraft(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance)
    {
        var applicationId = instance.ID;
        var visaTypes = LoadLookups<VisaType>(objectSpace);
        var visaCategories = LoadLookups<VisaCategory>(objectSpace);
        var visaPeriods = LoadLookups<VisaPeriod>(objectSpace);
        var visaIssuedPlaces = LoadLookups<VisaIssuedPlace>(objectSpace);
        var defaultPlaceId = objectSpace.GetObjectsQuery<VisaIssuedPlace>().FirstOrDefault(p => p.IsDefault)?.ID
            ?? visaIssuedPlaces.FirstOrDefault()?.Id;
        var borderZoneNames = CommaSeparatedCatalogHelper.LoadCatalogNames(
            objectSpace,
            typeof(BorderZoneName),
            CommaSeparatedSelectionHelper.NoneValue);

        var defaultTypeId = instance.VisaType?.ID
            ?? instance.ApplicationProfile?.DefaultVisaType?.ID
            ?? visaTypes.FirstOrDefault()?.Id;
        var defaultCategoryId = instance.VisaCategory?.ID
            ?? visaCategories.FirstOrDefault()?.Id;
        var defaultPeriodId = instance.VisaPeriod?.ID
            ?? visaPeriods.FirstOrDefault()?.Id;
        var defaultBorderZone = BorderZoneSelectionHelper.ResolveForIssuedVisa(null, instance);

        var issuedVisas = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.IssuingApplicationProfileInstance != null
                && v.IssuingApplicationProfileInstance.ID == applicationId)
            .ToList();
        var visaByPersonId = issuedVisas
            .Where(v => v.Passport?.Person != null)
            .GroupBy(v => v.Passport!.Person!.ID)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(v => v.IssueDate).ThenByDescending(v => v.ID).First());

        var people = ApplicationRosterHelper.GetRosterPeople(instance)
            .Where(p => p != null)
            .OrderBy(p => p!.LastName)
            .ThenBy(p => p!.FirstName)
            .ToList();

        var cards = new List<IssueIssuedVisaPersonCardDraft>();
        foreach (var person in people)
        {
            visaByPersonId.TryGetValue(person.ID, out var existingVisa);
            var already = existingVisa != null;
            var passport = existingVisa?.Passport
                ?? ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            var issueDate = already && existingVisa != null && existingVisa.IssueDate != default
                ? existingVisa.IssueDate.Date
                : DefaultIssueDate(instance, invitationIssuedDate: null);
            DateTime? expiration;
            if (already && existingVisa?.ExpirationDate is DateTime existingExp && existingExp != default)
                expiration = existingExp.Date;
            else
                expiration = issueDate.AddMonths(6);

            cards.Add(new IssueIssuedVisaPersonCardDraft
            {
                InvitationId = Guid.Empty,
                InvitationNumber = string.Empty,
                InvitationIssuedDate = default,
                InvitationItemId = Guid.Empty,
                PersonId = person.ID,
                PersonName = person.FullName?.Trim() ?? person.ToString() ?? string.Empty,
                PassportId = passport?.ID,
                PassportNumber = passport?.PassportNumber?.Trim() ?? string.Empty,
                Include = !already && passport != null,
                AlreadyIssued = already,
                IsReady = passport != null,
                ExistingVisaId = existingVisa?.ID,
                ExistingVisaNumber = already ? existingVisa?.VisaNumber?.Trim() : null,
                VisaNumber = already ? existingVisa?.VisaNumber?.Trim() ?? string.Empty : string.Empty,
                VisaTypeId = already
                    ? existingVisa?.VisaType?.ID ?? defaultTypeId
                    : defaultTypeId,
                VisaCategoryId = already
                    ? existingVisa?.VisaCategory?.ID ?? defaultCategoryId
                    : defaultCategoryId,
                VisaPeriodId = defaultPeriodId,
                VisaIssuedPlaceId = already
                    ? existingVisa?.VisaIssuedPlace?.ID ?? defaultPlaceId
                    : defaultPlaceId,
                IssueDate = issueDate,
                ExpirationDate = expiration,
                BorderZoneLocation = already
                    ? existingVisa?.BorderZoneLocation?.Trim() ?? defaultBorderZone
                    : defaultBorderZone,
                Documents = already && existingVisa != null
                    ? ListDocuments(objectSpace, existingVisa.ID).ToList()
                    : [],
            });
        }

        var groups = cards.Count == 0
            ? new List<IssueIssuedVisaInvitationGroupDraft>()
            : new List<IssueIssuedVisaInvitationGroupDraft>
            {
                new()
                {
                    InvitationId = Guid.Empty,
                    InvitationNumber = string.Empty,
                    InvitationIssuedDate = default,
                    People = cards,
                },
            };

        return new IssueIssuedVisaComposeDraft
        {
            ApplicationProfileInstanceId = applicationId,
            ApplicationCaption = instance.FullApplicationNumber?.Trim()
                ?? instance.ApplicationNumber?.ToString()
                ?? applicationId.ToString("N")[..8],
            ProfileCode = instance.ApplicationProfile?.Code?.Trim() ?? string.Empty,
            UsesInvitationSource = false,
            PeopleWithoutIssuedInvitation = Array.Empty<string>(),
            VisaTypes = visaTypes,
            VisaCategories = visaCategories,
            VisaPeriods = visaPeriods,
            VisaIssuedPlaces = visaIssuedPlaces,
            BorderZoneNames = borderZoneNames,
            Groups = groups,
        };
    }

    public static IssueIssuedVisaComposeDraft? LoadExistingDraft(
        IObjectSpace objectSpace,
        Guid applicationId,
        Guid visaId)
    {
        if (objectSpace == null || applicationId == Guid.Empty || visaId == Guid.Empty)
            return null;

        var visa = objectSpace.GetObjectByKey<Visa>(visaId);
        if (visa?.IssuingApplicationProfileInstance == null
            || visa.IssuingApplicationProfileInstance.ID != applicationId
            || !CanOpenInSlot(visa.IssuingApplicationProfileInstance))
        {
            return null;
        }

        var full = LoadDraft(objectSpace, applicationId);
        if (full == null)
            return null;

        var sourceGroup = full.Groups.FirstOrDefault(g => g.People.Any(p => p.ExistingVisaId == visaId));
        var person = sourceGroup?.People.FirstOrDefault(p => p.ExistingVisaId == visaId)
            ?? PersonFromVisa(objectSpace, visa);
        var group = new IssueIssuedVisaInvitationGroupDraft
        {
            InvitationId = sourceGroup?.InvitationId ?? person.InvitationId,
            InvitationNumber = sourceGroup?.InvitationNumber ?? person.InvitationNumber,
            InvitationIssuedDate = sourceGroup?.InvitationIssuedDate ?? person.InvitationIssuedDate,
            People = [person],
        };
        var number = visa.VisaNumber?.Trim();

        return new IssueIssuedVisaComposeDraft
        {
            ApplicationProfileInstanceId = applicationId,
            ExistingVisaId = visaId,
            Title = string.IsNullOrWhiteSpace(number) ? "Issued visa" : $"Visa {number}",
            ApplicationCaption = full.ApplicationCaption,
            ProfileCode = full.ProfileCode,
            UsesInvitationSource = full.UsesInvitationSource,
            VisaTypes = full.VisaTypes,
            VisaCategories = full.VisaCategories,
            VisaPeriods = full.VisaPeriods,
            VisaIssuedPlaces = full.VisaIssuedPlaces,
            BorderZoneNames = full.BorderZoneNames,
            Groups = [group],
        };
    }

    public static IssueIssuedVisaCreateResult Create(IObjectSpace objectSpace, IssueIssuedVisaComposeDraft draft)
    {
        if (objectSpace == null || draft == null)
        {
            return new IssueIssuedVisaCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Compose session is not available.",
            };
        }

        try
        {
            var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(draft.ApplicationProfileInstanceId);
            if (instance == null || !CanOpenInSlot(instance))
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This application cannot issue visas.",
                };
            }

            return UsesInvitationSource(instance)
                ? CreateFromInvitationLines(objectSpace, instance, draft)
                : CreateFromRoster(objectSpace, instance, draft);
        }
        catch (Exception ex)
        {
            return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    private static IssueIssuedVisaCreateResult CreateFromInvitationLines(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedVisaComposeDraft draft)
    {
        var selected = draft.AllPeople
            .Where(p => p.Include && !p.AlreadyIssued)
            .ToList();
        if (selected.Count == 0)
        {
            return new IssueIssuedVisaCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Cannot create visas — no unused issued invitation line.",
            };
        }

            var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var row in selected)
            {
                var error = ValidateRow(objectSpace, instance, row, numbers, excludeVisaId: null);
                if (error != null)
                {
                    return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = error };
                }

                if (row.PendingCopyBytes is { Length: > 0 })
                {
                    var copyError = IssueIssuedHeaderComposeService.ValidateDocumentBytes(
                        objectSpace,
                        row.PendingCopyFileName ?? "visa-copy.pdf",
                        row.PendingCopyBytes);
                    if (!string.IsNullOrEmpty(copyError))
                        return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = $"{row.PersonName}: {copyError}" };
                }
            }

            var created = new List<IssueIssuedVisaCreatedRow>();
            foreach (var row in selected)
            {
                var item = objectSpace.GetObjectByKey<InvitationItem>(row.InvitationItemId);
                if (item == null || item.Invitation?.ApplicationProfileInstance == null
                    || item.Invitation.ApplicationProfileInstance.ID != instance.ID)
                {
                    return new IssueIssuedVisaCreateResult
                    {
                        Succeeded = false,
                        ErrorMessage = "An invitation line is no longer on this application.",
                    };
                }

                if (item.IsCancelled || item.IsChanged || item.IsUsed || item.IssuedVisa != null)
                {
                    return new IssueIssuedVisaCreateResult
                    {
                        Succeeded = false,
                        ErrorMessage = $"{row.PersonName} already has a visa on this invitation line.",
                    };
                }

                var passport = row.PassportId is Guid pid
                    ? objectSpace.GetObjectByKey<Passport>(pid)
                    : item.Passport ?? ApplicationProfileInstancePersonValidItems.ResolvePassport(item.Person);
                if (passport == null)
                {
                    return new IssueIssuedVisaCreateResult
                    {
                        Succeeded = false,
                        ErrorMessage = $"{row.PersonName} has no passport.",
                    };
                }

                var visa = objectSpace.CreateObject<Visa>();
                visa.IssuingApplicationProfileInstance = instance;
                visa.IssuingInvitationItem = item;
                visa.Passport = passport;
                visa.PathAIssuingLinksApplied = true;
                visa.VisaNumber = row.VisaNumber.Trim();
                visa.ProcessNumber = visa.VisaNumber;
                visa.VisaType = row.VisaTypeId is Guid typeId
                    ? objectSpace.GetObjectByKey<VisaType>(typeId)
                    : instance.VisaType;
                visa.VisaCategory = row.VisaCategoryId is Guid catId
                    ? objectSpace.GetObjectByKey<VisaCategory>(catId)
                    : item.Invitation.VisaCategory;
                var issuedPlace = row.VisaIssuedPlaceId is Guid placeId
                    ? objectSpace.GetObjectByKey<VisaIssuedPlace>(placeId)
                    : objectSpace.GetObjectsQuery<VisaIssuedPlace>().FirstOrDefault(p => p.IsDefault);
                if (issuedPlace == null)
                {
                    return new IssueIssuedVisaCreateResult
                    {
                        Succeeded = false,
                        ErrorMessage = $"{row.PersonName}: visa issued place is required.",
                    };
                }

                visa.VisaIssuedPlace = issuedPlace;
                visa.IssueDate = row.IssueDate.Date;
                visa.StartDate = row.IssueDate.Date;
                visa.ExpirationDate = row.ExpirationDate?.Date;
                visa.BorderZoneLocation = string.IsNullOrWhiteSpace(row.BorderZoneLocation)
                    ? BorderZoneSelectionHelper.ResolveForIssuedVisa(item.Invitation, instance)
                    : row.BorderZoneLocation.Trim();
                BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);

                var copyError = AttachPendingCopy(objectSpace, visa, row);
                if (copyError != null)
                    return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = copyError };

                var periodCaption = row.VisaPeriodId is Guid periodId
                    ? objectSpace.GetObjectByKey<VisaPeriod>(periodId)?.NameTm
                    : item.Invitation.VisaPeriod?.NameTm;

                created.Add(new IssueIssuedVisaCreatedRow
                {
                    VisaId = visa.ID,
                    VisaNumber = visa.VisaNumber,
                    PersonName = row.PersonName,
                    InvitationNumber = row.InvitationNumber,
                    VisaTypeCaption = visa.VisaType?.NameTm ?? string.Empty,
                    VisaCategoryCaption = visa.VisaCategory?.NameTm ?? string.Empty,
                    VisaPeriodCaption = periodCaption ?? string.Empty,
                    PassportNumber = passport.PassportNumber?.Trim() ?? row.PassportNumber,
                    IssueDate = visa.IssueDate,
                    ExpirationDate = visa.ExpirationDate,
                    BorderZoneCaption = visa.BorderZoneLocation_NameTm,
                });
            }

            objectSpace.CommitChanges();
            return new IssueIssuedVisaCreateResult { Succeeded = true, Rows = created };
    }

    private static IssueIssuedVisaCreateResult CreateFromRoster(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedVisaComposeDraft draft)
    {
        var selected = draft.AllPeople
            .Where(p => p.Include && !p.AlreadyIssued)
            .ToList();
        if (selected.Count == 0)
        {
            var message = !draft.AllPeople.Any()
                ? "Cannot create visas — no people on this case."
                : "Cannot create visas — every person on this case already has an issued visa.";
            return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = message };
        }

        var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in selected)
        {
            var error = ValidateRow(objectSpace, instance, row, numbers, excludeVisaId: null);
            if (error != null)
                return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = error };

            if (row.PendingCopyBytes is { Length: > 0 })
            {
                var copyError = IssueIssuedHeaderComposeService.ValidateDocumentBytes(
                    objectSpace,
                    row.PendingCopyFileName ?? "visa-copy.pdf",
                    row.PendingCopyBytes);
                if (!string.IsNullOrEmpty(copyError))
                    return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = $"{row.PersonName}: {copyError}" };
            }
        }

        var issuedOnCase = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.IssuingApplicationProfileInstance != null
                && v.IssuingApplicationProfileInstance.ID == instance.ID)
            .ToList();

        var created = new List<IssueIssuedVisaCreatedRow>();
        foreach (var row in selected)
        {
            var person = objectSpace.GetObjectByKey<Person>(row.PersonId);
            if (person == null || !ApplicationRosterHelper.IsPersonOnApplication(instance, person))
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = $"{row.PersonName} is not on this case.",
                };
            }

            if (issuedOnCase.Any(v => v.Passport?.Person != null && v.Passport.Person.ID == person.ID))
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = $"{row.PersonName} already has a visa on this case.",
                };
            }

            var passport = row.PassportId is Guid pid
                ? objectSpace.GetObjectByKey<Passport>(pid)
                : ApplicationProfileInstancePersonValidItems.ResolvePassport(person);
            if (passport == null)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = $"{row.PersonName} has no passport.",
                };
            }

            var visa = objectSpace.CreateObject<Visa>();
            visa.IssuingApplicationProfileInstance = instance;
            visa.IssuingInvitationItem = null;
            visa.Passport = passport;
            visa.PathAIssuingLinksApplied = true;
            visa.VisaNumber = row.VisaNumber.Trim();
            visa.ProcessNumber = visa.VisaNumber;
            visa.VisaType = row.VisaTypeId is Guid typeId
                ? objectSpace.GetObjectByKey<VisaType>(typeId)
                : instance.VisaType;
            visa.VisaCategory = row.VisaCategoryId is Guid catId
                ? objectSpace.GetObjectByKey<VisaCategory>(catId)
                : instance.VisaCategory;
            var issuedPlace = row.VisaIssuedPlaceId is Guid placeId
                ? objectSpace.GetObjectByKey<VisaIssuedPlace>(placeId)
                : objectSpace.GetObjectsQuery<VisaIssuedPlace>().FirstOrDefault(p => p.IsDefault);
            if (issuedPlace == null)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = $"{row.PersonName}: visa issued place is required.",
                };
            }

            visa.VisaIssuedPlace = issuedPlace;
            visa.IssueDate = row.IssueDate.Date;
            visa.StartDate = row.IssueDate.Date;
            visa.ExpirationDate = row.ExpirationDate?.Date;
            visa.BorderZoneLocation = string.IsNullOrWhiteSpace(row.BorderZoneLocation)
                ? BorderZoneSelectionHelper.ResolveForIssuedVisa(null, instance)
                : row.BorderZoneLocation.Trim();
            BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);

            var copyError = AttachPendingCopy(objectSpace, visa, row);
            if (copyError != null)
                return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = copyError };

            issuedOnCase.Add(visa);

            var periodCaption = row.VisaPeriodId is Guid periodId
                ? objectSpace.GetObjectByKey<VisaPeriod>(periodId)?.NameTm
                : instance.VisaPeriod?.NameTm;

            created.Add(new IssueIssuedVisaCreatedRow
            {
                VisaId = visa.ID,
                VisaNumber = visa.VisaNumber,
                PersonName = row.PersonName,
                InvitationNumber = string.Empty,
                VisaTypeCaption = visa.VisaType?.NameTm ?? string.Empty,
                VisaCategoryCaption = visa.VisaCategory?.NameTm ?? string.Empty,
                VisaPeriodCaption = periodCaption ?? string.Empty,
                PassportNumber = passport.PassportNumber?.Trim() ?? row.PassportNumber,
                IssueDate = visa.IssueDate,
                ExpirationDate = visa.ExpirationDate,
                BorderZoneCaption = visa.BorderZoneLocation_NameTm,
            });
        }

        objectSpace.CommitChanges();
        return new IssueIssuedVisaCreateResult { Succeeded = true, Rows = created };
    }

    public static IssueIssuedVisaCreateResult Update(IObjectSpace objectSpace, IssueIssuedVisaComposeDraft draft)
    {
        if (objectSpace == null || draft == null || draft.ExistingVisaId is not Guid visaId || visaId == Guid.Empty)
        {
            return new IssueIssuedVisaCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Compose session is not available.",
            };
        }

        try
        {
            var instance = objectSpace.GetObjectByKey<ApplicationProfileInstance>(draft.ApplicationProfileInstanceId);
            var visa = objectSpace.GetObjectByKey<Visa>(visaId);
            if (instance == null || visa == null || !CanOpenInSlot(instance)
                || visa.IssuingApplicationProfileInstance == null
                || visa.IssuingApplicationProfileInstance.ID != instance.ID)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This visa is not issued by this application.",
                };
            }

            var row = draft.AllPeople.FirstOrDefault();
            if (row == null)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "Visa details are missing.",
                };
            }

            var error = ValidateRow(objectSpace, instance, row, new HashSet<string>(StringComparer.OrdinalIgnoreCase), visaId);
            if (error != null)
                return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = error };

            var previousNumber = visa.VisaNumber?.Trim() ?? string.Empty;
            visa.VisaNumber = row.VisaNumber.Trim();
            if (string.IsNullOrWhiteSpace(visa.ProcessNumber)
                || string.Equals(visa.ProcessNumber.Trim(), previousNumber, StringComparison.OrdinalIgnoreCase))
            {
                visa.ProcessNumber = visa.VisaNumber;
            }

            visa.VisaType = row.VisaTypeId is Guid typeId
                ? objectSpace.GetObjectByKey<VisaType>(typeId)
                : visa.VisaType;
            visa.VisaCategory = row.VisaCategoryId is Guid catId
                ? objectSpace.GetObjectByKey<VisaCategory>(catId)
                : visa.VisaCategory;
            visa.VisaIssuedPlace = row.VisaIssuedPlaceId is Guid placeId
                ? objectSpace.GetObjectByKey<VisaIssuedPlace>(placeId)
                : visa.VisaIssuedPlace;
            visa.IssueDate = row.IssueDate.Date;
            visa.ExpirationDate = row.ExpirationDate?.Date;
            visa.BorderZoneLocation = string.IsNullOrWhiteSpace(row.BorderZoneLocation)
                ? BorderZoneSelectionHelper.ResolveForIssuedVisa(visa.IssuingInvitationItem?.Invitation, instance)
                : row.BorderZoneLocation.Trim();
            BorderZoneSelectionHelper.ApplyDefaultIfEmpty(visa);

            objectSpace.CommitChanges();
            return new IssueIssuedVisaCreateResult
            {
                Succeeded = true,
                Rows =
                [
                    new IssueIssuedVisaCreatedRow
                    {
                        VisaId = visa.ID,
                        VisaNumber = visa.VisaNumber,
                        PersonName = row.PersonName,
                        InvitationNumber = row.InvitationNumber,
                        VisaTypeCaption = visa.VisaType?.NameTm ?? string.Empty,
                        VisaCategoryCaption = visa.VisaCategory?.NameTm ?? string.Empty,
                        VisaPeriodCaption = row.VisaPeriodId is Guid periodId
                            ? objectSpace.GetObjectByKey<VisaPeriod>(periodId)?.NameTm ?? string.Empty
                            : string.Empty,
                        PassportNumber = row.PassportNumber,
                        IssueDate = visa.IssueDate,
                        ExpirationDate = visa.ExpirationDate,
                        BorderZoneCaption = visa.BorderZoneLocation_NameTm,
                    },
                ],
            };
        }
        catch (Exception ex)
        {
            return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    private static IssueIssuedVisaPersonCardDraft PersonFromVisa(IObjectSpace objectSpace, Visa visa)
    {
        var invitationItem = visa.IssuingInvitationItem;
        var invitation = invitationItem?.Invitation;
        var person = visa.Passport?.Person ?? invitationItem?.Person;
        var issueDate = visa.IssueDate == default ? DateTime.Today : visa.IssueDate.Date;
        return new IssueIssuedVisaPersonCardDraft
        {
            InvitationId = invitation?.ID ?? Guid.Empty,
            InvitationNumber = invitation == null ? string.Empty : FormatInvitationNumber(invitation),
            InvitationIssuedDate = invitation?.IssuedDate.Date ?? default,
            InvitationItemId = invitationItem?.ID ?? Guid.Empty,
            PersonId = person?.ID ?? Guid.Empty,
            PersonName = person?.FullName?.Trim()
                ?? person?.ToString()
                ?? visa.VisaNumber?.Trim()
                ?? string.Empty,
            PassportId = visa.Passport?.ID,
            PassportNumber = visa.Passport?.PassportNumber?.Trim() ?? string.Empty,
            Include = true,
            AlreadyIssued = true,
            IsReady = visa.Passport != null,
            ExistingVisaId = visa.ID,
            ExistingVisaNumber = visa.VisaNumber?.Trim(),
            VisaNumber = visa.VisaNumber?.Trim() ?? string.Empty,
            VisaTypeId = visa.VisaType?.ID,
            VisaCategoryId = visa.VisaCategory?.ID,
            VisaPeriodId = invitation?.VisaPeriod?.ID
                ?? visa.IssuingApplicationProfileInstance?.VisaPeriod?.ID,
            VisaIssuedPlaceId = visa.VisaIssuedPlace?.ID,
            IssueDate = issueDate,
            ExpirationDate = visa.ExpirationDate?.Date,
            BorderZoneLocation = visa.BorderZoneLocation?.Trim() ?? string.Empty,
            Documents = ListDocuments(objectSpace, visa.ID).ToList(),
        };
    }

    private static string? ValidateRow(
        IObjectSpace objectSpace,
        ApplicationProfileInstance instance,
        IssueIssuedVisaPersonCardDraft row,
        HashSet<string> numbersInBatch,
        Guid? excludeVisaId)
    {
        if (string.IsNullOrWhiteSpace(row.VisaNumber))
            return $"{row.PersonName}: visa number is required.";

        var normalized = row.VisaNumber.Trim();
        if (!numbersInBatch.Add(normalized))
            return $"Visa number {normalized} is used more than once in this form.";

        var upper = normalized.ToUpperInvariant();
        var duplicate = objectSpace.GetObjectsQuery<Visa>()
            .Where(v => v.VisaNumber != null);
        if (excludeVisaId is Guid exclude && exclude != Guid.Empty)
            duplicate = duplicate.Where(v => v.ID != exclude);
        if (duplicate.Any(v => v.VisaNumber.Trim().ToUpper() == upper))
            return $"Visa number {normalized} is already in use.";

        if (row.VisaTypeId is not Guid typeId || typeId == Guid.Empty)
            return $"{row.PersonName}: visa type is required.";
        if (row.VisaCategoryId is not Guid catId || catId == Guid.Empty)
            return $"{row.PersonName}: visa category is required.";
        if (row.VisaIssuedPlaceId is not Guid placeId || placeId == Guid.Empty)
            return $"{row.PersonName}: visa issued place is required.";
        if (excludeVisaId is null && (row.VisaPeriodId is not Guid periodId || periodId == Guid.Empty))
            return $"{row.PersonName}: visa period is required.";
        if (row.IssueDate == default)
            return $"{row.PersonName}: issued date is required.";
        if (instance.ApplicationDate != default && !(row.IssueDate.Date > instance.ApplicationDate.Date))
            return $"{row.PersonName}: issued date must be later than the case date ({instance.ApplicationDate:dd MMM yyyy}).";
        if (row.ExpirationDate is not DateTime exp || exp.Date <= row.IssueDate.Date)
            return $"{row.PersonName}: expiration must be later than issued date.";
        if (row.InvitationIssuedDate != default && !(row.IssueDate.Date > row.InvitationIssuedDate.Date))
            return $"{row.PersonName}: issued date must be later than invitation {row.InvitationNumber} ({row.InvitationIssuedDate:dd MMM yyyy}).";

        if (row.PassportId is null || row.PassportId == Guid.Empty)
            return $"{row.PersonName}: passport is required.";

        return null;
    }

    private static DateTime DefaultIssueDate(ApplicationProfileInstance instance, DateTime? invitationIssuedDate)
    {
        var min = DateTime.MinValue;
        if (invitationIssuedDate is DateTime inv && inv != default)
            min = inv.Date.AddDays(1);
        if (instance.ApplicationDate != default)
        {
            var appMin = instance.ApplicationDate.Date.AddDays(1);
            if (appMin > min)
                min = appMin;
        }

        if (min == DateTime.MinValue)
            return DateTime.Today;
        return DateTime.Today > min ? DateTime.Today : min;
    }

    private static string FormatInvitationNumber(Invitation invitation)
    {
        var number = invitation.InvitationNumber?.Trim();
        return string.IsNullOrWhiteSpace(number) ? invitation.ID.ToString("N")[..8] : number;
    }

    private static List<IssueIssuedVisaLookupOption> LoadLookups<T>(IObjectSpace objectSpace)
        where T : LookupBase
    {
        return objectSpace.GetObjectsQuery<T>()
            .OrderBy(x => x.NameTm)
            .Select(x => new IssueIssuedVisaLookupOption
            {
                Id = x.ID,
                Caption = x.NameTm ?? x.Code ?? x.ID.ToString("N"),
            })
            .ToList();
    }

    public static IReadOnlyList<IssueIssuedHeaderDocumentRow> ListDocuments(IObjectSpace objectSpace, Guid visaId)
    {
        if (objectSpace == null || visaId == Guid.Empty)
            return Array.Empty<IssueIssuedHeaderDocumentRow>();

        return objectSpace.GetObjectsQuery<VisaDocument>()
            .Where(d => d.Visa != null && d.Visa.ID == visaId)
            .ToList()
            .Select(d => ToDocumentRow(d))
            .Where(r => r != null)
            .Select(r => r!)
            .ToList();
    }

    public static IssueIssuedVisaCreateResult AddDocument(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid visaId,
        string fileName,
        byte[] content)
    {
        if (objectSpace == null || applicationProfileInstanceId == Guid.Empty || visaId == Guid.Empty)
        {
            return new IssueIssuedVisaCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Upload is not available.",
            };
        }

        var validationError = IssueIssuedHeaderComposeService.ValidateDocumentBytes(objectSpace, fileName, content);
        if (!string.IsNullOrEmpty(validationError))
            return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = validationError };

        try
        {
            var visa = objectSpace.GetObjectByKey<Visa>(visaId);
            if (visa?.IssuingApplicationProfileInstance == null
                || visa.IssuingApplicationProfileInstance.ID != applicationProfileInstanceId)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This visa is not issued by this application.",
                };
            }

            AttachFile(objectSpace, visa, fileName, content);
            objectSpace.CommitChanges();
            return new IssueIssuedVisaCreateResult { Succeeded = true, Rows = Array.Empty<IssueIssuedVisaCreatedRow>() };
        }
        catch (Exception ex)
        {
            return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    public static IssueIssuedVisaCreateResult RemoveDocument(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid visaId,
        Guid documentId)
    {
        if (objectSpace == null || visaId == Guid.Empty || documentId == Guid.Empty)
        {
            return new IssueIssuedVisaCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Remove is not available.",
            };
        }

        try
        {
            var visa = objectSpace.GetObjectByKey<Visa>(visaId);
            if (visa?.IssuingApplicationProfileInstance == null
                || visa.IssuingApplicationProfileInstance.ID != applicationProfileInstanceId)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This visa is not issued by this application.",
                };
            }

            var doc = objectSpace.GetObjectByKey<VisaDocument>(documentId);
            if (doc == null || doc.Visa == null || doc.Visa.ID != visaId)
            {
                return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = "File was not found." };
            }

            var file = doc.File;
            objectSpace.Delete(doc);
            if (file != null)
                objectSpace.Delete(file);
            objectSpace.CommitChanges();
            return new IssueIssuedVisaCreateResult { Succeeded = true, Rows = Array.Empty<IssueIssuedVisaCreatedRow>() };
        }
        catch (Exception ex)
        {
            return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    public static IssueIssuedVisaCreateResult Delete(
        IObjectSpace objectSpace,
        Guid applicationProfileInstanceId,
        Guid visaId)
    {
        if (objectSpace == null || applicationProfileInstanceId == Guid.Empty || visaId == Guid.Empty)
        {
            return new IssueIssuedVisaCreateResult
            {
                Succeeded = false,
                ErrorMessage = "Delete is not available.",
            };
        }

        try
        {
            var visa = objectSpace.GetObjectByKey<Visa>(visaId);
            if (visa == null)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "Visa was not found.",
                };
            }

            if (visa.IssuingApplicationProfileInstance == null
                || visa.IssuingApplicationProfileInstance.ID != applicationProfileInstanceId)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "This visa is not issued by this application.",
                };
            }

            var otherLinks = (visa.ApplicationProfileInstances ?? Array.Empty<ApplicationProfileInstance>())
                .Where(a => a != null && a.ID != applicationProfileInstanceId)
                .ToList();
            if (otherLinks.Count > 0)
            {
                return new IssueIssuedVisaCreateResult
                {
                    Succeeded = false,
                    ErrorMessage = "Cannot delete this visa — it is linked on another application.",
                };
            }

            var invitationItem = visa.IssuingInvitationItem;
            visa.IssuingInvitationItem = null;

            var inputLinks = visa.ApplicationProfileInstances;
            if (inputLinks != null && inputLinks.Count > 0)
            {
                foreach (var instance in inputLinks.ToList())
                    inputLinks.Remove(instance);
            }

            foreach (var doc in (visa.Documents ?? Array.Empty<VisaDocument>()).ToList())
            {
                var file = doc.File;
                objectSpace.Delete(doc);
                if (file != null)
                    objectSpace.Delete(file);
            }

            foreach (var image in (visa.Images ?? Array.Empty<VisaImage>()).ToList())
                objectSpace.Delete(image);

            objectSpace.Delete(visa);
            objectSpace.CommitChanges();
            return new IssueIssuedVisaCreateResult { Succeeded = true, Rows = Array.Empty<IssueIssuedVisaCreatedRow>() };
        }
        catch (Exception ex)
        {
            return new IssueIssuedVisaCreateResult { Succeeded = false, ErrorMessage = ex.Message };
        }
    }

    private static string? AttachPendingCopy(IObjectSpace objectSpace, Visa visa, IssueIssuedVisaPersonCardDraft row)
    {
        if (row.PendingCopyBytes is not { Length: > 0 })
            return null;

        AttachFile(objectSpace, visa, row.PendingCopyFileName ?? "visa-copy.pdf", row.PendingCopyBytes);
        row.PendingCopyBytes = null;
        row.PendingCopyFileName = null;
        return null;
    }

    private static void AttachFile(IObjectSpace objectSpace, Visa visa, string fileName, byte[] content)
    {
        var file = objectSpace.CreateObject<FileData>();
        file.FileName = string.IsNullOrWhiteSpace(fileName) ? "visa-copy.pdf" : Path.GetFileName(fileName.Trim());
        file.Content = content;
        file.Size = content.Length;
        var doc = objectSpace.CreateObject<VisaDocument>();
        doc.Visa = visa;
        doc.File = file;
        visa.Documents ??= new System.Collections.ObjectModel.ObservableCollection<VisaDocument>();
        visa.Documents.Add(doc);
    }

    private static IssueIssuedHeaderDocumentRow? ToDocumentRow(VisaDocument? doc)
    {
        if (doc == null)
            return null;
        var name = doc.File?.FileName?.Trim();
        if (string.IsNullOrWhiteSpace(name) && (doc.File == null || doc.File.Size <= 0))
            return null;
        return new IssueIssuedHeaderDocumentRow
        {
            DocumentId = doc.ID,
            FileName = string.IsNullOrWhiteSpace(name) ? "Attachment" : name,
            SizeBytes = doc.File?.Size ?? 0,
        };
    }
}