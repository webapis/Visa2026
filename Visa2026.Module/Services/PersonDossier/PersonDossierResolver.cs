using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Localization;
using Visa2026.Module.Services;
using Visa2026.Module.Services.ApplicationWorkspace;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.PreviewSlot;

namespace Visa2026.Module.Services.PersonDossier;

/// <summary>
/// Builds the read-only dossier snapshot for one <see cref="Person"/>.
/// </summary>
/// <remarks>
/// Mirrors the shape of <c>PersonLinkedDocumentsResolver</c> (sections -> records) so a dossier row
/// can be aligned with its document-copies row by <c>RecordKey</c>.
/// </remarks>
public static class PersonDossierResolver
{
    private const string CurrentDateFormat = "dd MMM yyyy";

    private const int ExpiringSoonDays = 30;
    private const int ExpiringWatchDays = 90;

    public static PersonDossierSnapshot Resolve(IObjectSpace objectSpace, Person? person)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);

        if (person == null)
            return new PersonDossierSnapshot();

        var target = objectSpace.GetObject(person) ?? person;

        var sections = BuildSections(objectSpace, target)
            .Where(IncludeSection)
            .OrderBy(section => section.SortOrder)
            .ToList();

        return new PersonDossierSnapshot
        {
            PersonId = ResolveId(objectSpace, target) ?? Guid.Empty,
            PersonDisplayName = target.FullName ?? string.Empty,
            PersonalNumber = target.PersonalNumber,
            PersonRole = target.PersonRole,
            PersonRoleLabel = L($"Role.{target.PersonRole}"),
            ProjectContractName = Describe(target.ProjectContract),
            PhotoDataUri = BuildPhotoDataUri(target.Photo),
            IsArchived = target.IsArchived,
            IdentityFields = BuildIdentityFields(target),
            StatusTiles = BuildStatusTiles(target),
            Sections = sections,
        };
    }

    // ---------------------------------------------------------------- identity

    private static IReadOnlyList<PersonDossierField> BuildIdentityFields(Person person)
    {
        var fields = new List<PersonDossierField>
        {
            Field("PersonalNumber", person.PersonalNumber),
            Field("DateOfBirth", FormatDateWithAge(person.DateOfBirth, person.Age)),
            Field("Gender", Describe(person.Gender)),
            Field("Citizenship", Describe(person.Nationality)),
            Field("CountryOfBirth", Describe(person.CountryOfBirth)),
            Field("BirthPlace", person.BirthPlace),
            Field("MaritalStatus", Describe(person.MaritalStatus)),
        };

        if (person.PersonRole == PersonRecordRole.Employee)
        {
            fields.Add(Field("HireDate", FormatDate(person.HireDate)));
            fields.Add(Field("PreviousWorkplacesInTurkmenistan", person.PreviousWorkplacesInTurkmenistan));
            fields.Add(Field("Email", person.Email));
            fields.Add(Field("Subcontractor", Describe(person.Subcontractor)));
        }
        else
        {
            fields.Add(Field("Relationship", Describe(person.Relationship)));
            fields.Add(Field("SponsoringEmployee", person.SponsoringEmployee?.FullName));
        }

        return fields
            .Where(field => !string.IsNullOrWhiteSpace(field.Value))
            .ToList();
    }

    private static PersonDossierField Field(string labelKey, string? value) => new()
    {
        Label = L($"Field.{labelKey}"),
        Value = value?.Trim() ?? string.Empty,
    };

    // ------------------------------------------------------------ status tiles

    private static IReadOnlyList<PersonDossierStatusTile> BuildStatusTiles(Person person)
    {
        var tiles = new List<PersonDossierStatusTile>();

        var passport = PersonCurrentItems.GetCurrentPassport(person);
        tiles.Add(BuildTile(
            "passport",
            passport?.PassportNumber,
            passport?.ExpirationDate,
            passport?.IsCancelled ?? false,
            passport?.DaysRemaining));

        var visa = PersonCurrentItems.GetCurrentVisa(person);
        tiles.Add(BuildTile(
            "visa",
            visa?.VisaNumber,
            visa?.ExpirationDate,
            visa?.IsCancelled ?? false,
            visa?.DaysRemaining));

        if (person.PersonRole != PersonRecordRole.TemporaryVisitor)
        {
            var workPermit = PersonCurrentItems.GetCurrentWorkPermitItem(person);
            tiles.Add(BuildTile(
                "workPermit",
                workPermit?.WorkPermitNumber,
                workPermit?.ExpirationDate,
                workPermit?.IsCancelled ?? false,
                workPermit?.DaysRemaining));
        }

        var address = PersonCurrentItems.GetCurrentAddressOfResidence(person);
        tiles.Add(BuildTile(
            "registration",
            address == null ? null : Describe(address.City) is { Length: > 0 } city ? city : address.DisplayAddress,
            address?.ExpirationDate,
            false,
            address?.DaysRemaining));

        return tiles;
    }

    private static PersonDossierStatusTile BuildTile(
        string tileId, string? value, DateTime? expiration, bool isCancelled, int? daysRemaining)
    {
        var hasRecord = !string.IsNullOrWhiteSpace(value);
        var (statusLabel, statusCss) = hasRecord
            ? Classify(expiration, isCancelled, daysRemaining)
            : (L("Status.Missing"), string.Empty);

        return new PersonDossierStatusTile
        {
            TileId = tileId,
            Label = L($"Tile.{tileId}"),
            Value = hasRecord ? value!.Trim() : L("Status.None"),
            StatusLabel = statusLabel,
            StatusCssClass = statusCss,
        };
    }

    /// <summary>
    /// Maps an expiry date onto the Report Dashboard status vocabulary so colours stay consistent
    /// across the app (st-approved / st-pending / st-expiring; empty class renders gray).
    /// </summary>
    private static (string Label, string CssClass) Classify(
        DateTime? expiration, bool isCancelled, int? daysRemaining)
    {
        if (isCancelled)
            return (L("Status.Cancelled"), string.Empty);

        if (expiration == null)
            return (string.Empty, string.Empty);

        var days = daysRemaining ?? (int)Math.Floor((expiration.Value.Date - DateTime.Today).TotalDays);

        if (days <= 0)
            return (L("Status.Expired"), "st-expiring");

        if (days <= ExpiringSoonDays)
            return (Format("Status.ExpiresInDays", days), "st-expiring");

        if (days <= ExpiringWatchDays)
            return (Format("Status.ExpiresInDays", days), "st-pending");

        return (Format("Status.ValidTo", FormatDate(expiration)), "st-approved");
    }

    // ---------------------------------------------------------------- sections

    /// <summary>
    /// Invitations, Visas, Work permits, Active processes, and Family members stay on the
    /// screen tab strip even when the person has no row in that section. Other empty sections
    /// stay off the page.
    /// </summary>
    internal static bool IncludeSection(PersonDossierSection section) =>
        section.Records.Count > 0
        || section.SectionId is "activeProcesses" or "byApplication" or "visas" or "workPermits"
            or "borderZones" or "workPermitLocations" or "rejections" or "familyMembers";

    private static IEnumerable<PersonDossierSection> BuildSections(IObjectSpace objectSpace, Person person)
    {
        var isEmployee = person.PersonRole == PersonRecordRole.Employee;

        yield return BuildPassports(person);
        yield return BuildActiveProcesses(objectSpace, person);
        yield return BuildByApplication(objectSpace, person);
        yield return BuildVisaCases(objectSpace, person);
        yield return BuildWorkPermits(person);
        yield return BuildBorderZoneCases(objectSpace, person);
        yield return BuildWorkPermitLocationCases(objectSpace, person);
        yield return BuildRejectionCases(objectSpace, person);

        if (isEmployee)
        {
            yield return BuildEducation(person);
            yield return BuildPositionHistory(person);
        }

        yield return BuildAddresses(person);
        yield return BuildFamilyMembers(objectSpace, person);
        yield return BuildTravelHistory(person);
        yield return BuildMedicalRecords(person);

        // Applications (progress track and Issued chips) stays off the dossier for now.
        var rejections = BuildRejections(person);
        if (rejections.Records.Count > 0)
            yield return rejections;
    }

    private static PersonDossierSection BuildPassports(Person person)
    {
        var current = PersonCurrentItems.GetCurrentPassport(person);

        var records = Safe(person.Passports)
            .OrderByDescending(passport => passport.ExpirationDate ?? DateTime.MinValue)
            .Select(passport =>
            {
                var status = Classify(passport.ExpirationDate, passport.IsCancelled, passport.DaysRemaining);
                var hasCopy = HasAttachedFile(passport.Documents);
                return new PersonDossierRecord
                {
                    RecordKey = $"Passport:{passport.ID}",
                    SourceObjectId = passport.ID,
                    SourceObjectType = typeof(Passport),
                    IsCurrent = current != null && current.ID == passport.ID,
                    PersonCopyPersonId = hasCopy ? person.ID : null,
                    PersonCopyRecordKey = hasCopy ? $"Passport:{passport.ID:N}" : null,
                    PersonCopyDisplayName = hasCopy ? passport.PassportNumber : null,
                    UploadDetailType = hasCopy ? null : typeof(Passport),
                    UploadDetailId = hasCopy ? null : passport.ID,
                    Cells =
                    [
                        passport.PassportNumber ?? string.Empty,
                        Describe(passport.PassportType),
                        JoinNonEmpty(passport.Authority, Describe(passport.IssuedCountry)),
                        FormatDate(passport.IssueDate),
                        FormatDate(passport.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("passports", 10,
            ["Number", "Type", "IssuedBy", "IssueDate", "Expiry"], records, hasCopyColumn: true);
    }

    private const int InvitationVisaCellIndex = 5;
    private const int InvitationApplicationCellIndex = 6;

    private static PersonDossierSection BuildInvitations(IObjectSpace objectSpace, Person person)
    {
        var items = Safe(person.InvitationItems).ToList();
        var usedIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList());
        return BuildInvitations(person, usedIds);
    }

    /// <summary>
    /// One row per invitation issued for this person. A used row names the visa and,
    /// on preview, places that visa scan beside its passport.
    /// </summary>
    internal static PersonDossierSection BuildInvitations(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds)
    {
        var usedLabel = L("Status.Used");
        var records = Safe(person.InvitationItems)
            .Where(item => item.Invitation != null)
            .OrderByDescending(item => item.Invitation!.ExpirationDate ?? item.ExpirationDate ?? DateTime.MinValue)
            .ThenByDescending(item => item.Invitation!.IssuedDate)
            .Select(item =>
            {
                var invitation = item.Invitation!;
                var status = ClassifyInvitationItem(item, usedInvitationItemIds);
                var hasCopy = invitation.ID != Guid.Empty
                    && HasAttachedFile(invitation.Documents);
                var visa = item.IssuedVisa;
                var visaStatus = visa == null
                    ? (Label: string.Empty, CssClass: string.Empty)
                    : ClassifyIssuedVisa(visa);
                var application = invitation.ApplicationProfileInstance;
                var applicationNumber = application == null
                    ? string.Empty
                    : (application.FullApplicationNumber ?? application.ApplicationNumber ?? string.Empty).Trim();
                var applicationId = application?.ID ?? Guid.Empty;
                var visaNumber = visa?.VisaNumber?.Trim();
                var used = string.Equals(status.Label, usedLabel, StringComparison.Ordinal);
                return new PersonDossierRecord
                {
                    RecordKey = $"InvitationItem:{item.ID}",
                    SourceObjectId = item.ID,
                    SourceObjectType = typeof(InvitationItem),
                    PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.Invitation : null,
                    PreviewParentId = hasCopy ? invitation.ID : null,
                    AppendIssuedVisaBesidePassport = used,
                    UploadHeaderId = !hasCopy && invitation.ID != Guid.Empty ? invitation.ID : null,
                    UploadIssuedKind = !hasCopy && invitation.ID != Guid.Empty
                        ? IssueIssuedHeaderKind.Invitation
                        : null,
                    UploadApplicationProfileInstanceId = !hasCopy ? applicationId == Guid.Empty ? null : applicationId : null,
                    CompanionCellIndex = visa == null ? null : InvitationVisaCellIndex,
                    CompanionStatusLabel = visaStatus.Label,
                    CompanionStatusCssClass = visaStatus.CssClass,
                    ApplicationLinkCellIndex = applicationId != Guid.Empty && !string.IsNullOrWhiteSpace(applicationNumber)
                        ? InvitationApplicationCellIndex
                        : null,
                    ApplicationLinkId = applicationId == Guid.Empty ? null : applicationId,
                    Cells =
                    [
                        invitation.InvitationNumber ?? string.Empty,
                        Describe(invitation.VisaCategory),
                        Describe(invitation.VisaPeriod),
                        FormatDate(invitation.IssuedDate),
                        FormatDate(invitation.ExpirationDate ?? item.ExpirationDate),
                        string.IsNullOrWhiteSpace(visaNumber) ? "\u2014" : visaNumber,
                        string.IsNullOrWhiteSpace(applicationNumber) ? "\u2014" : applicationNumber,
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("invitations", 25,
            ["Number", "Category", "VisaPeriod", "IssueDate", "Expiry", "VisaIssued", "Application"],
            records,
            hasCopyColumn: true);
    }

    private static PersonDossierSection BuildByApplication(IObjectSpace objectSpace, Person person)
    {
        var items = Safe(person.InvitationItems).ToList();
        var usedIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList());
        var timelines = ApplicationWorkspaceListProgressSteps.LoadTimelines(
            objectSpace,
            CollectInvitationCaseApplications(person));
        return BuildByApplication(person, usedIds, timelines);
    }

    private static PersonDossierSection BuildVisaCases(IObjectSpace objectSpace, Person person)
    {
        var items = Safe(person.InvitationItems).ToList();
        var usedIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList());
        var timelines = ApplicationWorkspaceListProgressSteps.LoadTimelines(
            objectSpace,
            CollectVisaCaseApplications(person));
        return BuildVisaCases(person, usedIds, timelines);
    }

    private static PersonDossierSection BuildActiveProcesses(IObjectSpace objectSpace, Person person)
    {
        var items = Safe(person.InvitationItems).ToList();
        var usedIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList());
        var active = CollectActiveProcessApplications(person);
        var timelines = ApplicationWorkspaceListProgressSteps.LoadTimelines(objectSpace, active);
        return BuildActiveProcesses(person, usedIds, timelines);
    }

    private static PersonDossierSection BuildBorderZoneCases(IObjectSpace objectSpace, Person person) =>
        BuildTrackedCaseSection(objectSpace, person, CollectBorderZoneApplications(person), BuildBorderZoneCases);

    private static PersonDossierSection BuildWorkPermitLocationCases(IObjectSpace objectSpace, Person person) =>
        BuildTrackedCaseSection(objectSpace, person, CollectWorkPermitLocationApplications(person), BuildWorkPermitLocationCases);

    private static PersonDossierSection BuildRejectionCases(IObjectSpace objectSpace, Person person) =>
        BuildTrackedCaseSection(objectSpace, person, CollectRejectionApplications(person), BuildRejectionCases);

    private static PersonDossierSection BuildTrackedCaseSection(
        IObjectSpace objectSpace,
        Person person,
        IReadOnlyList<ApplicationProfileInstance> applications,
        Func<Person, IReadOnlySet<Guid>?, IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>?, PersonDossierSection> build)
    {
        var items = Safe(person.InvitationItems).ToList();
        var usedIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList());
        var timelines = ApplicationWorkspaceListProgressSteps.LoadTimelines(objectSpace, applications);
        return build(person, usedIds, timelines);
    }

    /// <summary>
    /// Invitations tab: one row per case that issues a new invitation, including a case that
    /// has not issued the invitation yet. A visa or work permit from that same case stays on
    /// the row. A visa extension stays on the Visas tab.
    /// </summary>
    internal static PersonDossierSection BuildByApplication(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines = null)
    {
        var buckets = CollectDocumentBuckets(person);
        foreach (var application in Safe(person.ApplicationProfileInstances))
        {
            if (IsInvitationTabCase(application))
                Bucket(buckets, application);
        }

        return CaseSection(
            "byApplication",
            27,
            "ByApplication",
            buckets.Values.Where(bucket => IsInvitationTabCase(bucket.Application)),
            person,
            usedInvitationItemIds,
            timelines);
    }

    /// <summary>
    /// Visas tab: one row per case that issues a visa and does not issue a new invitation.
    /// That includes a visa-only case and a visa-and-work-permit extension. An invitation
    /// case stays on the Invitations tab.
    /// </summary>
    internal static PersonDossierSection BuildVisaCases(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines = null)
    {
        var buckets = CollectDocumentBuckets(person);
        foreach (var application in Safe(person.ApplicationProfileInstances))
        {
            if (IsVisaTabCase(application))
                Bucket(buckets, application);
        }

        return CaseSection(
            "visas",
            28,
            "VisaCase",
            buckets.Values.Where(bucket => IsVisaTabCase(bucket.Application)),
            person,
            usedInvitationItemIds,
            timelines);
    }

    /// <summary>
    /// Active processes: every case for this person that has not reached a last step
    /// (issued, rejected, or cancelled), whatever the application type.
    /// </summary>
    internal static PersonDossierSection BuildActiveProcesses(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines = null)
    {
        var active = Safe(person.ApplicationProfileInstances).Where(IsActiveProcess).ToList();
        var activeIds = active.Select(application => application.ID).ToHashSet();
        var buckets = CollectDocumentBuckets(person);
        foreach (var application in active)
            Bucket(buckets, application);

        return CaseSection(
            "activeProcesses",
            26,
            "ActiveProcess",
            buckets.Values.Where(bucket => activeIds.Contains(bucket.Application.ID)),
            person,
            usedInvitationItemIds,
            timelines);
    }

    private static bool IsActiveProcess(ApplicationProfileInstance? application)
    {
        if (application == null || application.ID == Guid.Empty)
            return false;

        var code = application.LatestProgress?.State?.Code;
        if (string.IsNullOrWhiteSpace(code))
            code = application.LatestPrimaryStateCode;
        return !ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(code, application);
    }

    private static bool IsInvitationTabCase(ApplicationProfileInstance? application) =>
        application != null
        && application.ID != Guid.Empty
        && ApplicationProfileConfigurationResolver.CanIssueInvitation(application);

    private static bool IsVisaTabCase(ApplicationProfileInstance? application) =>
        application != null
        && application.ID != Guid.Empty
        && ApplicationProfileConfigurationResolver.CanIssueVisa(application)
        && !ApplicationProfileConfigurationResolver.CanIssueInvitation(application);

    /// <summary>
    /// Border zones: an invitation or visa case that asks for a border zone, a border-zone
    /// permit case, and a case that already granted places. The same case stays on Invitations
    /// or Visas.
    /// </summary>
    internal static PersonDossierSection BuildBorderZoneCases(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines = null) =>
        GrantCaseSection(
            person,
            usedInvitationItemIds,
            timelines,
            "borderZones",
            31,
            "BorderZoneCase",
            "BorderZone",
            IsBorderZoneTabCase,
            BorderZoneGrantText);

    /// <summary>
    /// Work-permit locations: a case that issues a new work permit, or a separate grant of
    /// additional places. The same case stays on Invitations or Visas.
    /// </summary>
    internal static PersonDossierSection BuildWorkPermitLocationCases(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines = null) =>
        GrantCaseSection(
            person,
            usedInvitationItemIds,
            timelines,
            "workPermitLocations",
            32,
            "WorkPermitLocationCase",
            "WorkPermitLocations",
            IsWorkPermitLocationTabCase,
            WorkPermitLocationGrantText);

    /// <summary>
    /// Rejections: a case that already issued a rejection for this person, and a rejection
    /// application that has not issued it yet. An invitation or visa case stays off this tab
    /// until a rejection exists.
    /// </summary>
    internal static PersonDossierSection BuildRejectionCases(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines = null) =>
        GrantCaseSection(
            person,
            usedInvitationItemIds,
            timelines,
            "rejections",
            33,
            "RejectionCase",
            "Rejection",
            IsRejectionTabCase,
            RejectionGrantText);

    private static PersonDossierSection GrantCaseSection(
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines,
        string sectionId,
        int sortOrder,
        string recordKeyPrefix,
        string extraColumnKey,
        Func<ApplicationProfileInstance?, bool> include,
        Func<Person, IssuedCaseBucket, string?> extraValue)
    {
        var buckets = CollectDocumentBuckets(person);
        foreach (var application in Safe(person.ApplicationProfileInstances))
        {
            if (include(application))
                Bucket(buckets, application);
        }

        foreach (var item in Safe(person.RejectionItems))
        {
            if (include(item.Rejection?.ApplicationProfileInstance))
                Bucket(buckets, item.Rejection!.ApplicationProfileInstance!);
        }

        return CaseSection(
            sectionId,
            sortOrder,
            recordKeyPrefix,
            buckets.Values.Where(bucket => include(bucket.Application)),
            person,
            usedInvitationItemIds,
            timelines,
            extraColumnKey,
            bucket => extraValue(person, bucket));
    }

    private static bool IsBorderZoneTabCase(ApplicationProfileInstance? application) =>
        application != null
        && application.ID != Guid.Empty
        && (ApplicationProfileConfigurationResolver.CanIssueBorderZone(application)
            || application.ApplicationProfile?.RequireBorderZone == true);

    private static bool IsWorkPermitLocationTabCase(ApplicationProfileInstance? application) =>
        application != null
        && application.ID != Guid.Empty
        && (ApplicationProfileConfigurationResolver.CanIssueWorkPermit(application)
            || ApplicationProfileConfigurationResolver.CanIssueWorkPermitLocationGrant(application));

    private static bool IsRejectionTabCase(ApplicationProfileInstance? application)
    {
        if (application == null || application.ID == Guid.Empty)
            return false;

        if (Safe(application.Rejections).Any())
            return true;

        var code = application.ApplicationProfile?.Code;
        if (!string.IsNullOrWhiteSpace(code)
            && code.Contains("seretmezlik", StringComparison.OrdinalIgnoreCase))
            return true;

        return ApplicationProfileConfigurationResolver.CanIssueRejection(application)
            && !ApplicationProfileConfigurationResolver.CanIssueInvitation(application)
            && !ApplicationProfileConfigurationResolver.CanIssueVisa(application)
            && !ApplicationProfileConfigurationResolver.CanIssueWorkPermit(application)
            && !ApplicationProfileConfigurationResolver.CanIssueBorderZone(application)
            && !ApplicationProfileConfigurationResolver.CanIssueWorkPermitLocationGrant(application);
    }

    private static string? BorderZoneGrantText(Person person, IssuedCaseBucket bucket)
    {
        foreach (var zone in Safe(bucket.Application.BorderZones))
        {
            if (string.IsNullOrWhiteSpace(zone.BorderZoneNumber))
                continue;

            foreach (var item in Safe(zone.BorderZoneItems))
            {
                if (!SamePerson(person, item.Person))
                    continue;

                var onItem = MeaningfulBorderZoneLocations(item.BorderZoneLocation);
                if (onItem != null)
                    return onItem;
            }
        }

        var invitationLocations = MeaningfulBorderZoneLocations(bucket.Invitation?.Invitation?.BorderZoneLocation);
        if (invitationLocations != null && !string.IsNullOrWhiteSpace(bucket.Invitation?.Invitation?.InvitationNumber))
            return invitationLocations;

        var visa = bucket.Visa?.Visa;
        var visaLocations = MeaningfulBorderZoneLocations(visa?.BorderZoneLocation);
        if (visaLocations != null && !string.IsNullOrWhiteSpace(visa?.VisaNumber))
            return visaLocations;

        return null;
    }

    private static string? WorkPermitLocationGrantText(Person person, IssuedCaseBucket bucket)
    {
        foreach (var grant in Safe(bucket.Application.WorkPermitLocationGrants))
        {
            foreach (var item in Safe(grant.Items))
            {
                if (!SamePerson(person, item.Person))
                    continue;

                var granted = MeaningfulList(item.GrantedLocations);
                if (granted != null)
                    return granted;
            }
        }

        if (!string.IsNullOrWhiteSpace(bucket.WorkPermit?.WorkPermitNumber))
            return MeaningfulList(bucket.WorkPermit.WorkPermittedLocations);

        return null;
    }

    private static string? RejectionGrantText(Person person, IssuedCaseBucket bucket)
    {
        foreach (var rejection in Safe(bucket.Application.Rejections))
        {
            var items = Safe(rejection.RejectionItems).ToList();
            if (items.Count > 0 && items.All(item => !SamePerson(person, item.Person)))
                continue;

            var number = rejection.RejectedDocNumber?.Trim();
            if (!string.IsNullOrWhiteSpace(number))
                return number;

            var reason = rejection.Reason?.Trim();
            if (!string.IsNullOrWhiteSpace(reason))
                return reason;

            if (rejection.Date != default)
                return FormatDate(rejection.Date);
        }

        return null;
    }

    private static bool SamePerson(Person person, Person? other) =>
        other != null
        && (ReferenceEquals(person, other)
            || (person.ID != Guid.Empty && person.ID == other.ID));

    private static string? MeaningfulList(string? stored)
    {
        var trimmed = stored?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static PersonDossierSection CaseSection(
        string sectionId,
        int sortOrder,
        string recordKeyPrefix,
        IEnumerable<IssuedCaseBucket> buckets,
        Person person,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        IReadOnlyDictionary<Guid, IReadOnlyList<ApplicationWorkspaceCaseProgressStep>>? timelines,
        string? extraColumnKey = null,
        Func<IssuedCaseBucket, string?>? extraValue = null)
    {
        var usedLabel = L("Status.Used");
        var records = buckets
            .OrderByDescending(bucket => bucket.Application.ApplicationDate)
            .ThenByDescending(bucket =>
                bucket.Application.FullApplicationNumber ?? bucket.Application.ApplicationNumber ?? string.Empty,
                StringComparer.OrdinalIgnoreCase)
            .Select(bucket =>
            {
                var application = bucket.Application;
                var applicationNumber = (application.FullApplicationNumber ?? application.ApplicationNumber ?? string.Empty).Trim();
                IReadOnlyList<ApplicationWorkspaceCaseProgressStep>? timeline = null;
                if (timelines != null)
                    timelines.TryGetValue(application.ID, out timeline);
                var approvalLetters = ReadApprovalLetters(timeline);
                var passportChip = PassportChip(person, CasePassport(person, bucket));
                var invitationChip = InvitationChip(bucket.Invitation, usedInvitationItemIds, usedLabel);
                var visaChip = VisaChip(person, bucket.Visa);
                var workPermitChip = WorkPermitChip(bucket.WorkPermit);
                var cells = new List<string>
                {
                    string.IsNullOrWhiteSpace(applicationNumber) ? "\u2014" : applicationNumber,
                    passportChip?.Number ?? "\u2014",
                    invitationChip?.Number ?? "\u2014",
                    visaChip?.Number ?? "\u2014",
                    workPermitChip?.Number ?? "\u2014",
                };
                var chips = new List<PersonDossierIssuedChip?>
                {
                    null, passportChip, invitationChip, visaChip, workPermitChip,
                };
                if (extraColumnKey != null)
                {
                    var extra = extraValue?.Invoke(bucket);
                    cells.Add(string.IsNullOrWhiteSpace(extra) ? "\u2014" : extra);
                    chips.Add(null);
                }

                return new PersonDossierRecord
                {
                    RecordKey = $"{recordKeyPrefix}:{application.ID}",
                    SourceObjectId = application.ID,
                    SourceObjectType = typeof(ApplicationProfileInstance),
                    FirstCellDetail = application.ApplicationProfile?.Name ?? string.Empty,
                    FirstCellNote = FormatDate(application.ApplicationDate),
                    ApplicationLinkCellIndex = string.IsNullOrWhiteSpace(applicationNumber) ? null : 0,
                    ApplicationLinkId = string.IsNullOrWhiteSpace(applicationNumber) ? null : application.ID,
                    HasApprovalLetters = approvalLetters.HasLetters,
                    MissingApprovalSteps = approvalLetters.MissingSteps,
                    ProgressSteps = ToDossierProgressSteps(timeline),
                    Cells = cells,
                    CellChips = chips,
                };
            })
            .ToList();

        var columnKeys = new List<string> { "Application", "Passport", "Invitation", "Visa", "WorkPermit" };
        if (extraColumnKey != null)
            columnKeys.Add(extraColumnKey);

        return Section(
            sectionId,
            sortOrder,
            columnKeys.ToArray(),
            records,
            hideStatusColumn: true);
    }

    private static Dictionary<Guid, IssuedCaseBucket> CollectDocumentBuckets(Person person)
    {
        var buckets = new Dictionary<Guid, IssuedCaseBucket>();

        foreach (var item in Safe(person.InvitationItems))
        {
            var application = item.Invitation?.ApplicationProfileInstance;
            if (application == null || application.ID == Guid.Empty || item.Invitation == null)
                continue;
            if (string.IsNullOrWhiteSpace(item.Invitation.InvitationNumber))
                continue;

            var bucket = Bucket(buckets, application);
            if (bucket.Invitation == null || LaterInvitation(item, bucket.Invitation))
                bucket.Invitation = item;
        }

        foreach (var passport in Safe(person.Passports))
        {
            foreach (var visa in Safe(passport.Visas))
            {
                var application = visa.IssuingApplicationProfileInstance;
                if (application == null || application.ID == Guid.Empty)
                    continue;
                if (string.IsNullOrWhiteSpace(visa.VisaNumber))
                    continue;

                var bucket = Bucket(buckets, application);
                if (bucket.Visa == null || LaterVisa(visa, bucket.Visa.Value.Visa))
                    bucket.Visa = (passport, visa);
            }
        }

        foreach (var item in Safe(person.WorkPermitItems))
        {
            var application = item.WorkPermit?.ApplicationProfileInstance;
            if (application == null || application.ID == Guid.Empty)
                continue;
            if (string.IsNullOrWhiteSpace(item.WorkPermitNumber))
                continue;

            var bucket = Bucket(buckets, application);
            if (bucket.WorkPermit == null || LaterWorkPermit(item, bucket.WorkPermit))
                bucket.WorkPermit = item;
        }

        return buckets;
    }

    private static IssuedCaseBucket Bucket(
        Dictionary<Guid, IssuedCaseBucket> buckets,
        ApplicationProfileInstance application)
    {
        if (!buckets.TryGetValue(application.ID, out var bucket))
        {
            bucket = new IssuedCaseBucket(application);
            buckets[application.ID] = bucket;
        }

        return bucket;
    }

    private static bool LaterInvitation(InvitationItem candidate, InvitationItem current) =>
        IsLater(
            candidate.Invitation?.ExpirationDate ?? candidate.ExpirationDate,
            current.Invitation?.ExpirationDate ?? current.ExpirationDate,
            candidate.Invitation?.IssuedDate ?? DateTime.MinValue,
            current.Invitation?.IssuedDate ?? DateTime.MinValue);

    private static bool LaterVisa(Visa candidate, Visa current) =>
        IsLater(
            candidate.ExpirationDate,
            current.ExpirationDate,
            candidate.IssueDate,
            current.IssueDate);

    private static bool LaterWorkPermit(WorkPermitItem candidate, WorkPermitItem current) =>
        IsLater(
            candidate.ExpirationDate,
            current.ExpirationDate,
            candidate.StartDate,
            current.StartDate);

    private static bool IsLater(
        DateTime? candidateExpiry,
        DateTime? currentExpiry,
        DateTime candidateIssued,
        DateTime currentIssued)
    {
        var candidateKey = candidateExpiry ?? DateTime.MinValue;
        var currentKey = currentExpiry ?? DateTime.MinValue;
        if (candidateKey != currentKey)
            return candidateKey > currentKey;
        return candidateIssued > currentIssued;
    }

    /// <summary>
    /// Passport recorded on this person's invitation or work permit for the case,
    /// otherwise the passport the case's visa was issued in.
    /// </summary>
    private static Passport? CasePassport(Person person, IssuedCaseBucket bucket)
    {
        if (BelongsToPerson(person, bucket.Invitation?.Passport))
            return bucket.Invitation!.Passport;
        if (BelongsToPerson(person, bucket.WorkPermit?.Passport))
            return bucket.WorkPermit!.Passport;
        if (bucket.Visa is { } pair && BelongsToPerson(person, pair.Passport))
            return pair.Passport;

        return Safe(bucket.Application.Passports)
            .Where(passport => BelongsToPerson(person, passport))
            .OrderByDescending(passport => passport.ExpirationDate ?? DateTime.MinValue)
            .FirstOrDefault();
    }

    private static bool BelongsToPerson(Person person, Passport? passport)
    {
        if (passport == null || string.IsNullOrWhiteSpace(passport.PassportNumber))
            return false;
        if (passport.Person == null)
            return true;
        if (person.ID != Guid.Empty && passport.Person.ID != Guid.Empty)
            return passport.Person.ID == person.ID;
        return ReferenceEquals(passport.Person, person) || passport.Person.ID == person.ID;
    }

    private static PersonDossierIssuedChip? PassportChip(Person person, Passport? passport)
    {
        if (passport == null || string.IsNullOrWhiteSpace(passport.PassportNumber))
            return null;

        var status = Classify(passport.ExpirationDate, passport.IsCancelled, passport.DaysRemaining);
        var hasCopy = HasAttachedFile(passport.Documents);
        return new PersonDossierIssuedChip
        {
            Number = passport.PassportNumber.Trim(),
            StatusLabel = status.Label,
            StatusCssClass = status.CssClass,
            PersonCopyPersonId = hasCopy ? person.ID : null,
            PersonCopyRecordKey = hasCopy ? $"Passport:{passport.ID:N}" : null,
            PersonCopyDisplayName = hasCopy ? passport.PassportNumber : null,
        };
    }

    private static PersonDossierIssuedChip? InvitationChip(
        InvitationItem? item,
        IReadOnlySet<Guid>? usedInvitationItemIds,
        string usedLabel)
    {
        var invitation = item?.Invitation;
        if (item == null || invitation == null || string.IsNullOrWhiteSpace(invitation.InvitationNumber))
            return null;

        var status = ClassifyInvitationItem(item, usedInvitationItemIds);
        var hasCopy = invitation.ID != Guid.Empty && HasAttachedFile(invitation.Documents);
        return new PersonDossierIssuedChip
        {
            Number = invitation.InvitationNumber.Trim(),
            StatusLabel = status.Label,
            StatusCssClass = status.CssClass,
            PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.Invitation : null,
            PreviewParentId = hasCopy ? invitation.ID : null,
            AppendIssuedVisaBesidePassport = string.Equals(status.Label, usedLabel, StringComparison.Ordinal),
        };
    }

    private static PersonDossierIssuedChip? VisaChip(Person person, (Passport Passport, Visa Visa)? pair)
    {
        if (pair == null)
            return null;

        var (passport, visa) = pair.Value;
        if (string.IsNullOrWhiteSpace(visa.VisaNumber))
            return null;

        var status = ClassifyIssuedVisa(visa);
        var hasCopy = HasAttachedFile(visa.Documents);
        return new PersonDossierIssuedChip
        {
            Number = visa.VisaNumber.Trim(),
            StatusLabel = status.Label,
            StatusCssClass = status.CssClass,
            PersonCopyPersonId = hasCopy ? person.ID : null,
            PersonCopyRecordKey = hasCopy ? $"Passport:{passport.ID:N}/Visa:{visa.ID:N}" : null,
            PersonCopyDisplayName = hasCopy ? visa.VisaNumber : null,
        };
    }

    private static PersonDossierIssuedChip? WorkPermitChip(WorkPermitItem? item)
    {
        var permit = item?.WorkPermit;
        if (item == null || string.IsNullOrWhiteSpace(item.WorkPermitNumber))
            return null;

        var status = Classify(item.ExpirationDate, item.IsCancelled, item.DaysRemaining);
        var hasCopy = permit != null && permit.ID != Guid.Empty && HasAttachedFile(permit.Documents);
        return new PersonDossierIssuedChip
        {
            Number = item.WorkPermitNumber.Trim(),
            StatusLabel = status.Label,
            StatusCssClass = status.CssClass,
            PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.WorkPermit : null,
            PreviewParentId = hasCopy ? permit!.ID : null,
        };
    }

    private sealed class IssuedCaseBucket(ApplicationProfileInstance application)
    {
        public ApplicationProfileInstance Application { get; } = application;

        public InvitationItem? Invitation { get; set; }

        public (Passport Passport, Visa Visa)? Visa { get; set; }

        public WorkPermitItem? WorkPermit { get; set; }
    }

    private static PersonDossierSection BuildWorkPermits(Person person)
    {
        var current = PersonCurrentItems.GetCurrentWorkPermitItem(person);

        var records = Safe(person.WorkPermitItems)
            .OrderByDescending(item => item.ExpirationDate)
            .Select(item =>
            {
                var status = Classify(item.ExpirationDate, item.IsCancelled, item.DaysRemaining);
                var permit = item.WorkPermit;
                var hasCopy = permit != null
                    && permit.ID != Guid.Empty
                    && HasAttachedFile(permit.Documents);
                return new PersonDossierRecord
                {
                    RecordKey = $"WorkPermitItem:{item.ID}",
                    SourceObjectId = item.ID,
                    SourceObjectType = typeof(WorkPermitItem),
                    IsCurrent = current != null && current.ID == item.ID,
                    PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.WorkPermit : null,
                    PreviewParentId = hasCopy ? permit!.ID : null,
                    UploadHeaderId = !hasCopy && permit != null && permit.ID != Guid.Empty ? permit.ID : null,
                    UploadIssuedKind = !hasCopy && permit != null && permit.ID != Guid.Empty
                        ? IssueIssuedHeaderKind.WorkPermit
                        : null,
                    UploadApplicationProfileInstanceId = !hasCopy ? permit?.ApplicationProfileInstance?.ID : null,
                    Cells =
                    [
                        item.WorkPermitNumber ?? string.Empty,
                        item.ASNumber ?? string.Empty,
                        FormatDate(item.StartDate),
                        FormatDate(item.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("workPermits", 30,
            ["Number", "ASNumber", "ValidFrom", "Expiry"], records, hasCopyColumn: true);
    }

    private static PersonDossierSection BuildEducation(Person person)
    {
        var records = Safe(person.Educations)
            .Select(education =>
            {
                var hasCopy = HasAttachedFile(education.Documents);
                return new PersonDossierRecord
                {
                RecordKey = $"Education:{education.ID}",
                SourceObjectId = education.ID,
                SourceObjectType = typeof(Education),
                PersonCopyPersonId = hasCopy ? person.ID : null,
                PersonCopyRecordKey = hasCopy ? $"Education:{education.ID:N}" : null,
                PersonCopyDisplayName = hasCopy ? Describe(education.EducationInstitution) : null,
                UploadDetailType = hasCopy ? null : typeof(Education),
                UploadDetailId = hasCopy ? null : education.ID,
                Cells =
                [
                    Describe(education.EducationLevel),
                    Describe(education.EducationInstitution),
                    Describe(education.Specialty),
                    Describe(education.EducationCountry),
                    education.GraduationYear ?? string.Empty,
                ],
                };
            })
            .ToList();

        return Section("education", 40,
            ["Level", "Institution", "Specialty", "Country", "GraduationYear"], records, hasCopyColumn: true);
    }

    private static PersonDossierSection BuildPositionHistory(Person person)
    {
        var records = Safe(person.PositionHistory)
            .OrderByDescending(history => history.StartDate)
            .Select(history => new PersonDossierRecord
            {
                RecordKey = $"EmployeePositionHistory:{history.ID}",
                SourceObjectId = history.ID,
                SourceObjectType = typeof(EmployeePositionHistory),
                IsCurrent = history.EndDate == null,
                Cells =
                [
                    Describe(history.Position),
                    Describe(history.Department),
                    FormatDate(history.StartDate),
                    history.EndDate == null ? L("Status.Present") : FormatDate(history.EndDate),
                ],
            })
            .ToList();

        return Section("positionHistory", 50,
            ["Position", "Department", "From", "To"], records);
    }

    private static PersonDossierSection BuildAddresses(Person person)
    {
        var current = PersonCurrentItems.GetCurrentAddressOfResidence(person);

        var records = Safe(person.AddressesOfResidence)
            .Select(address =>
            {
                var status = Classify(address.ExpirationDate, false, address.DaysRemaining);
                return new PersonDossierRecord
                {
                    RecordKey = $"AddressOfResidence:{address.ID}",
                    SourceObjectId = address.ID,
                    SourceObjectType = typeof(AddressOfResidence),
                    IsCurrent = current != null && current.ID == address.ID,
                    Cells =
                    [
                        address.Type == null
                            ? string.Empty
                            : LOr($"ResidenceType.{address.Type}", address.Type.ToString()!),
                        Describe(address.City),
                        address.FullAddress ?? string.Empty,
                        FormatDate(address.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("addresses", 60,
            ["Type", "City", "Address", "Expiry"], records);
    }

    private static PersonDossierSection BuildTravelHistory(Person person)
    {
        var records = Safe(person.TravelHistories)
            .OrderByDescending(travel => travel.TravelDate)
            .Select(travel => new PersonDossierRecord
            {
                RecordKey = $"TravelHistory:{travel.ID}",
                SourceObjectId = travel.ID,
                SourceObjectType = typeof(TravelHistory),
                Cells =
                [
                    FormatDate(travel.TravelDate),
                    travel.MovementType == null
                        ? string.Empty
                        : LOr($"MovementType.{travel.MovementType}", travel.MovementType.ToString()!),
                    travel.TravelType == null
                        ? string.Empty
                        : LOr($"TravelType.{travel.TravelType}", travel.TravelType.ToString()!),
                    JoinNonEmpty(
                        Describe(travel.CheckPoint),
                        Describe(travel.Country),
                        Describe(travel.City)),
                ],
            })
            .ToList();

        return Section("travel", 70,
            ["Date", "Movement", "Type", "Place"], records);
    }

    private static PersonDossierSection BuildMedicalRecords(Person person)
    {
        var records = Safe(person.MedicalRecords)
            .OrderByDescending(record => record.IssueDate)
            .Select(record =>
            {
                var status = Classify(record.ExpirationDate, false, record.DaysRemaining);
                return new PersonDossierRecord
                {
                    RecordKey = $"MedicalRecord:{record.ID}",
                    SourceObjectId = record.ID,
                    SourceObjectType = typeof(MedicalRecord),
                    Cells =
                    [
                        record.DocumentNumber ?? string.Empty,
                        FormatDate(record.IssueDate),
                        FormatDate(record.ExpirationDate),
                    ],
                    StatusLabel = status.Label,
                    StatusCssClass = status.CssClass,
                };
            })
            .ToList();

        return Section("medical", 80,
            ["Number", "IssueDate", "Expiry"], records);
    }

    internal static PersonDossierSection BuildFamilyMembers(IObjectSpace objectSpace, Person person)
    {
        var members = Safe(person.FamilyMembers).ToList();
        var usedIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            members.SelectMany(member => Safe(member.InvitationItems))
                .Select(item => item.ID)
                .Where(id => id != Guid.Empty)
                .ToList());
        var usedLabel = L("Status.Used");
        var records = members
            .Select(member =>
            {
                var passport = PassportChip(member, CurrentOrLatestPassport(member));
                var invitation = InvitationChip(LatestInvitationItem(member), usedIds, usedLabel);
                var visa = VisaChip(member, CurrentOrLatestVisa(member));
                var proof = RelationshipDocumentChips(member);
                return new PersonDossierRecord
                {
                    RecordKey = $"FamilyMember:{member.ID}",
                    SourceObjectId = member.ID,
                    SourceObjectType = typeof(Person),
                    Cells =
                    [
                        member.FullName ?? string.Empty,
                        Describe(member.Relationship),
                        FormatDate(member.DateOfBirth),
                        Describe(member.Nationality),
                        passport?.Number ?? "\u2014",
                        invitation?.Number ?? "\u2014",
                        visa?.Number ?? "\u2014",
                        proof.Count == 0 ? "\u2014" : proof[0].Number,
                    ],
                    CellChips = [null, null, null, null, passport, invitation, visa, null],
                    MultiChipCellIndex = proof.Count == 0 ? null : 7,
                    MultiChips = proof,
                };
            })
            .ToList();

        return Section("familyMembers", 90,
            ["Name", "Relationship", "DateOfBirth", "Citizenship", "Passport", "Invitation", "Visa", "RelationshipDocument"],
            records,
            nestedSections:
            [
                MergeFamilyCases(objectSpace, members, "familyActiveProcesses", "activeProcesses", BuildActiveProcesses),
                MergeFamilyCases(objectSpace, members, "familyInvitations", "byApplication", BuildByApplication),
                MergeFamilyCases(objectSpace, members, "familyVisas", "visas", BuildVisaCases),
                MergeFamilyCases(objectSpace, members, "familyBorderZones", "borderZones", BuildBorderZoneCases),
            ]);
    }

    /// <summary>
    /// One tab under Family members. Rows are that member's own cases, with the member's name.
    /// The label reuses the employee tab caption.
    /// </summary>
    private static PersonDossierSection MergeFamilyCases(
        IObjectSpace objectSpace,
        IReadOnlyList<Person> members,
        string sectionId,
        string labelSectionId,
        Func<IObjectSpace, Person, PersonDossierSection> build)
    {
        var records = new List<PersonDossierRecord>();
        PersonDossierSection? template = null;
        IReadOnlyList<Person> people = members.Count == 0
            ? new List<Person> { new Person() }
            : members;
        foreach (var member in people)
        {
            var built = build(objectSpace, member);
            template ??= built;
            if (members.Count == 0)
                break;

            var name = member.FullName ?? string.Empty;
            foreach (var record in built.Records)
            {
                record.MemberName = name;
                records.Add(record);
            }
        }

        return new PersonDossierSection
        {
            SectionId = sectionId,
            SectionLabel = L($"Section.{labelSectionId}"),
            SortOrder = template?.SortOrder ?? 0,
            ColumnHeaders = template?.ColumnHeaders ?? Array.Empty<string>(),
            Records = records,
            HideStatusColumn = true,
            HasMemberColumn = true,
        };
    }

    private static Passport? CurrentOrLatestPassport(Person person) =>
        PersonCurrentItems.GetCurrentPassport(person)
        ?? Safe(person.Passports)
            .OrderByDescending(passport => passport.ExpirationDate ?? DateTime.MinValue)
            .FirstOrDefault();

    private static InvitationItem? LatestInvitationItem(Person person) =>
        Safe(person.InvitationItems)
            .Where(item => !string.IsNullOrWhiteSpace(item.Invitation?.InvitationNumber))
            .OrderByDescending(item => item.Invitation?.ExpirationDate ?? item.ExpirationDate ?? DateTime.MinValue)
            .FirstOrDefault();

    private static (Passport Passport, Visa Visa)? CurrentOrLatestVisa(Person person)
    {
        var current = PersonCurrentItems.GetCurrentVisa(person);
        if (current?.Passport != null && !string.IsNullOrWhiteSpace(current.VisaNumber))
            return (current.Passport, current);

        var latest = Safe(person.Passports)
            .SelectMany(passport => Safe(passport.Visas).Select(visa => (passport, visa)))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.visa.VisaNumber))
            .OrderByDescending(pair => pair.visa.ExpirationDate ?? DateTime.MinValue)
            .FirstOrDefault();
        return latest.visa == null ? null : latest;
    }

    private static IReadOnlyList<PersonDossierIssuedChip> RelationshipDocumentChips(Person member)
    {
        if (member.ID == Guid.Empty)
            return Array.Empty<PersonDossierIssuedChip>();

        var document = Safe(member.FamilyRelationDocuments)
            .FirstOrDefault(item => item.File != null && item.ID != Guid.Empty);
        if (document == null)
            return Array.Empty<PersonDossierIssuedChip>();

        var label = VisaUiMessages.Get("PersonDocumentCopies.Section.FamilyRelationDocuments");
        return
        [
            new PersonDossierIssuedChip
            {
                Number = label,
                PersonCopyPersonId = member.ID,
                PersonCopySectionId = "FamilyRelationDocuments",
                PersonCopyRecordKey = $"FamilyRelationDocument:{document.ID:N}",
                PersonCopyDisplayName = label,
            },
        ];
    }

    private static PersonDossierSection BuildApplications(IObjectSpace objectSpace, Person person)
    {
        var personId = ResolveId(objectSpace, person) ?? person.ID;
        var exclusions = LoadSeretmezlikByInstance(objectSpace, personId);

        var apps = Safe(person.ApplicationProfileInstances)
            .OrderByDescending(app => app.ApplicationDate)
            .ToList();
        var timelines = ApplicationWorkspaceListProgressSteps.LoadTimelines(objectSpace, apps);
        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(objectSpace, apps);
        var invitationItems = Safe(person.InvitationItems).ToList();
        var usedInvitationIds = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(
            objectSpace,
            invitationItems.Select(item => item.ID).Where(id => id != Guid.Empty).ToList());
        var locationGrantItemsByApp = LoadLocationGrantItemsByApplication(objectSpace, personId);
        var borderZoneItemsByApp = LoadBorderZoneItemsByApplication(objectSpace, personId);

        var records = apps
            .Select(app =>
            {
                if (!exclusions.TryGetValue(app.ID, out var letter)
                    && TryFindSeretmezlikOnInstance(app, personId, out var fromNav))
                {
                    letter = fromNav;
                    exclusions[app.ID] = letter;
                }

                timelines.TryGetValue(app.ID, out var timeline);
                var excluded = exclusions.ContainsKey(app.ID);
                var compactSteps = ApplicationWorkspaceListProgressSteps.FromTimeline(
                    timeline ?? Array.Empty<ApplicationWorkspaceCaseProgressStep>());
                var coverageChips = ApplicationWorkspaceIssuedResultListCoverage.ForDisplay(
                    app.ListViewResultCoverageChips);
                var resultChips = ApplicationWorkspaceListProgressSteps.ShouldShowResultChips(
                    compactSteps,
                    coverageChips)
                    ? coverageChips
                    : Array.Empty<ApplicationWorkspaceIssuedResultListCoverage.Chip>();
                locationGrantItemsByApp.TryGetValue(app.ID, out var locationGrantItem);
                borderZoneItemsByApp.TryGetValue(app.ID, out var borderZoneItem);
                var borderZoneLocations = borderZoneItem == null
                    ? null
                    : ResolveGrantedBorderZoneLocations(objectSpace, app, personId, borderZoneItem);
                return new PersonDossierRecord
                {
                    RecordKey = $"Application:{app.ID}",
                    SourceObjectId = app.ID,
                    SourceObjectType = typeof(ApplicationProfileInstance),
                    Cells =
                    [
                        app.FullApplicationNumber ?? app.ApplicationNumber ?? string.Empty,
                    ],
                    FirstCellDetail = app.ApplicationProfile?.Name ?? Describe(app.ApplicationType),
                    FirstCellNote = FormatDate(app.ApplicationDate),
                    StatusLabel = excluded
                        ? FormatExcludedStatus(letter.LetterNumber, letter.LetterDate)
                        : string.Empty,
                    StatusCssClass = excluded ? "st-expiring" : string.Empty,
                    ApplicationGroupId = PersonDossierApplicationGroups.Resolve(app.ApplicationProfile),
                    ProgressSteps = ToDossierProgressSteps(timeline),
                    ResultChips = resultChips,
                    IssuedOutcome = ResolveIssuedOutcome(
                        timeline,
                        invitationItems.FirstOrDefault(item => item.Invitation?.ApplicationProfileInstance?.ID == app.ID),
                        Safe(person.RejectionItems).FirstOrDefault(item => item.Rejection?.ApplicationProfileInstance?.ID == app.ID),
                        usedInvitationIds,
                        locationGrantItem,
                        borderZoneItem,
                        borderZoneLocations),
                };
            })
            .ToList();

        return Section("applications", 100,
            ["ApplicationProfile"], records,
            hasProgressColumn: true,
            hasIssuedOutcomeColumn: true,
            applicationGroups: BuildApplicationGroups(records));
    }

    /// <summary>
    /// Ministry approval letters on the progress track, in process order.
    /// A step counts when it is a ministry leg with a letter file. Missing steps are ministry
    /// decisions that still have no letter.
    /// </summary>
    internal static (bool HasLetters, IReadOnlyList<string> MissingSteps) ReadApprovalLetters(
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep>? steps)
    {
        if (steps == null || steps.Count == 0)
            return (false, Array.Empty<string>());

        var hasLetters = false;
        var missing = new List<string>();
        foreach (var step in steps)
        {
            if (IsMinistryApprovalStep(step) && HasMinistryLetterFile(step))
                hasLetters = true;
            if (step.MissingMinistryLetter)
            {
                var label = ApplicationProfileLocalization.ProgressStepLabel(step.Label);
                if (!string.IsNullOrWhiteSpace(label))
                    missing.Add(label);
            }
        }

        return (hasLetters, missing);
    }

    private static bool IsMinistryApprovalStep(ApplicationWorkspaceCaseProgressStep step) =>
        step.Key.StartsWith("leg-", StringComparison.OrdinalIgnoreCase)
        && step.FileSlot == ApplicationProfileInstanceProgressFiles.MinistryLetter;

    private static bool HasMinistryLetterFile(ApplicationWorkspaceCaseProgressStep step) =>
        step.Files is { Count: > 0 } || !string.IsNullOrWhiteSpace(step.MinistryLetterFileName);

    private static List<ApplicationProfileInstance> CollectInvitationCaseApplications(Person person)
    {
        var map = new Dictionary<Guid, ApplicationProfileInstance>();
        void Add(ApplicationProfileInstance? application)
        {
            if (IsInvitationTabCase(application))
                map[application!.ID] = application;
        }

        foreach (var item in Safe(person.InvitationItems))
            Add(item.Invitation?.ApplicationProfileInstance);
        foreach (var application in Safe(person.ApplicationProfileInstances))
            Add(application);
        return map.Values.ToList();
    }

    private static List<ApplicationProfileInstance> CollectVisaCaseApplications(Person person)
    {
        var map = new Dictionary<Guid, ApplicationProfileInstance>();
        void Add(ApplicationProfileInstance? application)
        {
            if (IsVisaTabCase(application))
                map[application!.ID] = application;
        }

        foreach (var passport in Safe(person.Passports))
        {
            foreach (var visa in Safe(passport.Visas))
                Add(visa.IssuingApplicationProfileInstance);
        }

        foreach (var item in Safe(person.WorkPermitItems))
            Add(item.WorkPermit?.ApplicationProfileInstance);
        foreach (var application in Safe(person.ApplicationProfileInstances))
            Add(application);
        return map.Values.ToList();
    }

    private static List<ApplicationProfileInstance> CollectActiveProcessApplications(Person person) =>
        Safe(person.ApplicationProfileInstances).Where(IsActiveProcess).ToList();

    private static List<ApplicationProfileInstance> CollectBorderZoneApplications(Person person) =>
        Safe(person.ApplicationProfileInstances).Where(IsBorderZoneTabCase).ToList();

    private static List<ApplicationProfileInstance> CollectWorkPermitLocationApplications(Person person) =>
        Safe(person.ApplicationProfileInstances).Where(IsWorkPermitLocationTabCase).ToList();

    private static List<ApplicationProfileInstance> CollectRejectionApplications(Person person)
    {
        var map = new Dictionary<Guid, ApplicationProfileInstance>();
        void Add(ApplicationProfileInstance? application)
        {
            if (IsRejectionTabCase(application))
                map[application!.ID] = application;
        }

        foreach (var application in Safe(person.ApplicationProfileInstances))
            Add(application);
        foreach (var item in Safe(person.RejectionItems))
            Add(item.Rejection?.ApplicationProfileInstance);
        return map.Values.ToList();
    }

    /// <summary>Maps the workspace progress track onto dossier rows (screen and paper share this).</summary>
    internal static IReadOnlyList<PersonDossierProgressStep> ToDossierProgressSteps(
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep>? steps)
    {
        if (steps == null || steps.Count == 0)
            return Array.Empty<PersonDossierProgressStep>();

        var result = new List<PersonDossierProgressStep>(steps.Count);
        for (var i = 0; i < steps.Count; i++)
        {
            var step = steps[i];
            var isCurrent = string.Equals(step.State, "current", StringComparison.OrdinalIgnoreCase);
            var isOffice = string.Equals(
                step.Key,
                ApplicationWorkspaceProgressTimeline.OfficeKey,
                StringComparison.OrdinalIgnoreCase);
            var fileName = step.MinistryLetterFileName ?? string.Empty;
            result.Add(new PersonDossierProgressStep
            {
                Label = ApplicationProfileLocalization.ProgressStepLabel(step.Label),
                Date = step.Date ?? string.Empty,
                StatusLabel = ApplicationWorkspaceListProgressSteps.Badge(step),
                ResultNumber = step.ResultNumber ?? string.Empty,
                IsOfficeFile = isOffice,
                LetterFileName = fileName,
                LetterProgressId = step.ProgressId,
                MissingLetter = step.MissingMinistryLetter
                    || (isOffice && !isCurrent && string.IsNullOrWhiteSpace(fileName)),
                IsCurrentStep = isCurrent,
                Tone = ApplicationWorkspaceListProgressSteps.Tone(ToCompact(step)),
                Glyph = ApplicationWorkspaceListProgressSteps.Glyph(ToCompact(step), i + 1),
                ConnectorDone = string.Equals(step.State, "done", StringComparison.OrdinalIgnoreCase),
            });
        }

        return result;
    }

    private static ApplicationWorkspaceListProgressSteps.Step ToCompact(ApplicationWorkspaceCaseProgressStep step) =>
        new(
            step.Key,
            step.Label,
            step.State,
            step.OutcomeKind,
            step.Date ?? string.Empty,
            step.CurrentStateLabel ?? string.Empty);

    /// <summary>Same wording as People &amp; links Seretmezlik badge (localized).</summary>
    internal static string FormatExcludedStatus(string? letterNumber, DateTime letterDate) =>
        Format(
            "Status.Excluded",
            letterNumber ?? string.Empty,
            letterDate.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));

    private static bool TryFindSeretmezlikOnInstance(
        ApplicationProfileInstance app,
        Guid personId,
        out (string LetterNumber, DateTime LetterDate) letter)
    {
        letter = default;
        if (app.Exclusions == null || personId == Guid.Empty)
            return false;

        foreach (var exclusion in app.Exclusions.OrderBy(e => e.LetterDate))
        {
            if (exclusion?.People == null)
                continue;
            foreach (var row in exclusion.People)
            {
                var rowPersonId = row.PersonId != Guid.Empty
                    ? row.PersonId
                    : row.Person?.ID ?? Guid.Empty;
                if (rowPersonId != personId)
                    continue;
                letter = (exclusion.LetterNumber ?? string.Empty, exclusion.LetterDate);
                return true;
            }
        }

        return false;
    }

    /// <summary>Seretmezlik letter № + date per instance for this person (earliest letter wins).</summary>
    private static Dictionary<Guid, (string LetterNumber, DateTime LetterDate)> LoadSeretmezlikByInstance(
        IObjectSpace objectSpace,
        Guid personId)
    {
        var map = new Dictionary<Guid, (string LetterNumber, DateTime LetterDate)>();
        if (personId == Guid.Empty)
            return map;

        var people = objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusionPerson>()
            .Where(p => p.PersonId == personId)
            .ToList();
        if (people.Count == 0)
            return map;

        var exclusionIds = people.Select(p => p.ExclusionId).Distinct().ToList();
        var letters = objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusion>()
            .Where(e => exclusionIds.Contains(e.ID))
            .ToList()
            .ToDictionary(e => e.ID);

        foreach (var row in people.OrderBy(p =>
                     letters.TryGetValue(p.ExclusionId, out var letter) ? letter.LetterDate : DateTime.MaxValue))
        {
            if (!letters.TryGetValue(row.ExclusionId, out var letter))
                continue;
            if (letter.ApplicationProfileInstanceId == Guid.Empty
                || map.ContainsKey(letter.ApplicationProfileInstanceId))
                continue;
            map[letter.ApplicationProfileInstanceId] = (letter.LetterNumber ?? string.Empty, letter.LetterDate);
        }

        return map;
    }

    /// <summary>
    /// InvitationItem dossier status — mutually exclusive:
    /// Cancelled → Changed (per InvitationItem) → Used → Expired → Valid to {expiry}.
    /// </summary>
    internal static (string Label, string CssClass) ClassifyInvitationItem(
        InvitationItem item,
        IReadOnlySet<Guid>? usedInvitationItemIds = null)
    {
        if (IssuedDocumentLifecycle.IsCancelled(item))
            return (L("Status.Cancelled"), "st-expiring");

        if (IssuedDocumentLifecycle.IsChanged(item))
            return (L("Status.Changed"), "st-expiring");

        var used = item.IssuedVisa != null
            || (usedInvitationItemIds != null && item.ID != Guid.Empty && usedInvitationItemIds.Contains(item.ID));
        if (used)
            return (L("Status.Used"), "st-approved");

        var expiration = item.Invitation?.ExpirationDate ?? item.ExpirationDate;
        if (expiration is { } expiry && expiry.Date < DateTime.Today)
            return (L("Status.Expired"), "st-expiring");

        if (expiration is { } validTo && validTo != default)
            return (Format("Status.ValidTo", FormatDate(validTo)), "st-approved");

        return (string.Empty, string.Empty);
    }

    /// <summary>Visa state on a used invitation row. Same expiry pills as the Visas section, after cancelled and changed.</summary>
    internal static (string Label, string CssClass) ClassifyIssuedVisa(Visa visa)
    {
        if (IssuedDocumentLifecycle.IsCancelled(visa))
            return (L("Status.Cancelled"), "st-expiring");

        if (IssuedDocumentLifecycle.IsChanged(visa))
            return (L("Status.Changed"), "st-expiring");

        return Classify(visa.ExpirationDate, false, visa.DaysRemaining);
    }

    /// <summary>
    /// Invitation, location grant (WPL), border zone, or rejection for this person — only after Migration service is finished.
    /// A rejection on that step replaces the invitation / grant.
    /// </summary>
    internal static PersonDossierIssuedOutcome? ResolveIssuedOutcome(
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep>? timeline,
        InvitationItem? invitationItem,
        RejectionItem? rejectionItem,
        IReadOnlySet<Guid>? usedInvitationItemIds = null,
        WorkPermitLocationGrantItem? locationGrantItem = null,
        BorderZoneItem? borderZoneItem = null,
        string? borderZoneLocations = null)
    {
        var migration = timeline?.FirstOrDefault(step =>
            string.Equals(step.Key, ApplicationWorkspaceProgressTimeline.MigrationKey, StringComparison.OrdinalIgnoreCase));
        if (migration == null
            || string.Equals(migration.State, "current", StringComparison.OrdinalIgnoreCase)
            || string.Equals(migration.State, "pending", StringComparison.OrdinalIgnoreCase))
            return null;

        var rejected = string.Equals(migration.OutcomeKind, "rejected", StringComparison.OrdinalIgnoreCase);
        if (rejected)
            return BuildRejectionOutcome(rejectionItem);

        return BuildInvitationOutcome(invitationItem, usedInvitationItemIds)
            ?? BuildLocationGrantOutcome(locationGrantItem)
            ?? BuildBorderZoneOutcome(borderZoneItem, borderZoneLocations)
            ?? BuildRejectionOutcome(rejectionItem);
    }

    private static PersonDossierIssuedOutcome? BuildInvitationOutcome(
        InvitationItem? item,
        IReadOnlySet<Guid>? usedInvitationItemIds)
    {
        var invitation = item?.Invitation;
        if (item == null || invitation == null || string.IsNullOrWhiteSpace(invitation.InvitationNumber))
            return null;

        var status = ClassifyInvitationItem(item, usedInvitationItemIds);
        var hasCopy = invitation.ID != Guid.Empty
            && Safe(invitation.Documents).Any(document => document.File != null);
        return new PersonDossierIssuedOutcome
        {
            Number = invitation.InvitationNumber,
            StatusLabel = status.Label,
            StatusCssClass = status.CssClass,
            PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.Invitation : null,
            PreviewParentId = hasCopy ? invitation.ID : null,
            AppendIssuedVisaBesidePassport = string.Equals(status.Label, L("Status.Used"), StringComparison.Ordinal),
        };
    }

    /// <summary>
    /// Places granted on this border zone. The item column is often empty: Join stores the
    /// person's zones on the case requested-location row, and the case summary is the fallback.
    /// </summary>
    private static string? ResolveGrantedBorderZoneLocations(
        IObjectSpace objectSpace,
        ApplicationProfileInstance app,
        Guid personId,
        BorderZoneItem item)
    {
        var onItem = MeaningfulBorderZoneLocations(item.BorderZoneLocation);
        if (onItem != null)
            return onItem;

        var requested = CasePersonRequestedLocation.ResolveForPrefill(
            objectSpace,
            app.ID,
            personId,
            ApplicationProfileInstancePersonRequestedLocationKind.BorderZone,
            app.BorderZoneLocation);
        return MeaningfulBorderZoneLocations(requested);
    }

    private static string? MeaningfulBorderZoneLocations(string? stored)
    {
        var trimmed = stored?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || BorderZoneSelectionHelper.IsNoneValue(trimmed))
            return null;
        return trimmed;
    }

    private static PersonDossierIssuedOutcome? BuildBorderZoneOutcome(
        BorderZoneItem? item,
        string? grantedLocations = null)
    {
        var zone = item?.BorderZone;
        var number = zone?.BorderZoneNumber?.Trim();
        if (zone == null || string.IsNullOrWhiteSpace(number))
            return null;

        var hasCopy = zone.ID != Guid.Empty
            && Safe(zone.Documents).Any(document => document.File != null);
        var locations = MeaningfulBorderZoneLocations(item?.BorderZoneLocation)
            ?? MeaningfulBorderZoneLocations(grantedLocations)
            ?? string.Empty;
        return new PersonDossierIssuedOutcome
        {
            Number = number,
            StatusLabel = ApplicationProfileLocalization.Msg("ApplicationProfileInstance.Workspace.Issued"),
            StatusCssClass = "st-approved",
            Detail = locations,
            PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.BorderZone : null,
            PreviewParentId = hasCopy ? zone.ID : null,
        };
    }

    private static Dictionary<Guid, BorderZoneItem> LoadBorderZoneItemsByApplication(
        IObjectSpace objectSpace,
        Guid personId)
    {
        var map = new Dictionary<Guid, BorderZoneItem>();
        if (personId == Guid.Empty)
            return map;

        var rows = objectSpace.GetObjectsQuery<BorderZoneItem>()
            .Where(i => i.Person != null
                && i.Person.ID == personId
                && i.BorderZone != null
                && i.BorderZone.ApplicationProfileInstance != null)
            .OrderByDescending(i => i.BorderZone!.StartDate)
            .ToList();

        foreach (var row in rows)
        {
            var appId = row.BorderZone?.ApplicationProfileInstance?.ID ?? Guid.Empty;
            if (appId == Guid.Empty)
                continue;
            map.TryAdd(appId, row);
        }

        return map;
    }
    private static PersonDossierIssuedOutcome? BuildLocationGrantOutcome(WorkPermitLocationGrantItem? item)
    {
        var grant = item?.Grant;
        var number = grant?.GrantNumber?.Trim();
        if (grant == null || string.IsNullOrWhiteSpace(number))
            return null;

        var hasCopy = grant.ID != Guid.Empty
            && Safe(grant.Documents).Any(document => document.File != null);
        var grantedLocations = item?.GrantedLocations?.Trim() ?? string.Empty;
        return new PersonDossierIssuedOutcome
        {
            Number = number,
            StatusLabel = ApplicationProfileLocalization.Msg("ApplicationProfileInstance.Workspace.Issued"),
            StatusCssClass = "st-approved",
            Detail = grantedLocations,
            PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.WorkPermitLocationGrant : null,
            PreviewParentId = hasCopy ? grant.ID : null,
        };
    }

    private static PersonDossierIssuedOutcome? BuildRejectionOutcome(RejectionItem? item)
    {
        var rejection = item?.Rejection;
        var number = rejection?.RejectedDocNumber;
        if (rejection == null || string.IsNullOrWhiteSpace(number))
            return null;

        var hasCopy = rejection.ID != Guid.Empty
            && Safe(rejection.Documents).Any(document => document.File != null);
        return new PersonDossierIssuedOutcome
        {
            Number = number,
            StatusLabel = L("Status.Rejected"),
            StatusCssClass = "st-expiring",
            PreviewFamily = hasCopy ? HeaderDocumentCopiesFamily.Rejection : null,
            PreviewParentId = hasCopy ? rejection.ID : null,
        };
    }

    private static Dictionary<Guid, WorkPermitLocationGrantItem> LoadLocationGrantItemsByApplication(
        IObjectSpace objectSpace,
        Guid personId)
    {
        var map = new Dictionary<Guid, WorkPermitLocationGrantItem>();
        if (personId == Guid.Empty)
            return map;

        var rows = objectSpace.GetObjectsQuery<WorkPermitLocationGrantItem>()
            .Where(i => i.Person != null
                && i.Person.ID == personId
                && i.Grant != null
                && i.Grant.ApplicationProfileInstance != null)
            .OrderByDescending(i => i.Grant!.IssuedDate)
            .ToList();

        foreach (var row in rows)
        {
            var appId = row.Grant?.ApplicationProfileInstance?.ID ?? Guid.Empty;
            if (appId == Guid.Empty)
                continue;
            map.TryAdd(appId, row);
        }

        return map;
    }

    private static PersonDossierSection BuildRejections(Person person)
    {
        var applicationIds = Safe(person.ApplicationProfileInstances)
            .Select(app => app.ID)
            .Where(id => id != Guid.Empty)
            .ToHashSet();
        var records = Safe(person.RejectionItems)
            .Where(item =>
            {
                var applicationId = item.Rejection?.ApplicationProfileInstance?.ID ?? Guid.Empty;
                return applicationId == Guid.Empty || !applicationIds.Contains(applicationId);
            })
            .Select(item => new PersonDossierRecord
            {
                RecordKey = $"RejectionItem:{item.ID}",
                SourceObjectId = item.ID,
                SourceObjectType = typeof(RejectionItem),
                Cells =
                [
                    item.Rejection?.RejectedDocNumber ?? string.Empty,
                    FormatDate(item.Rejection?.Date),
                    JoinNonEmpty(item.Reason, item.Rejection?.Reason),
                ],
                StatusCssClass = "st-expiring",
            })
            .ToList();

        return Section("rejections", 120,
            ["Document", "Date", "Reason"], records);
    }

    // ----------------------------------------------------------------- helpers

    private static bool HasAttachedFile<TDocument>(IEnumerable<TDocument>? documents)
        where TDocument : DocumentBase =>
        documents != null && documents.Any(document => document?.File != null);

    private static PersonDossierSection Section(
        string sectionId, int sortOrder, string[] columnKeys, List<PersonDossierRecord> records,
        bool hasCopyColumn = false,
        bool hasProgressColumn = false,
        bool hasIssuedOutcomeColumn = false,
        IReadOnlyList<PersonDossierApplicationGroup>? applicationGroups = null,
        bool hideStatusColumn = false,
        IReadOnlyList<PersonDossierSection>? nestedSections = null) => new()
        {
            SectionId = sectionId,
            SectionLabel = L($"Section.{sectionId}"),
            SortOrder = sortOrder,
            ColumnHeaders = columnKeys.Select(key => L($"Column.{key}")).ToList(),
            Records = records,
            HasCopyColumn = hasCopyColumn,
            HasProgressColumn = hasProgressColumn,
            HasIssuedOutcomeColumn = hasIssuedOutcomeColumn,
            ApplicationGroups = applicationGroups ?? Array.Empty<PersonDossierApplicationGroup>(),
            HideStatusColumn = hideStatusColumn,
            NestedSections = nestedSections ?? Array.Empty<PersonDossierSection>(),
        };

    private static List<PersonDossierApplicationGroup> BuildApplicationGroups(
        IReadOnlyList<PersonDossierRecord> records)
    {
        var groups = new List<PersonDossierApplicationGroup>(PersonDossierApplicationGroups.ButtonOrder.Length);
        foreach (var groupId in PersonDossierApplicationGroups.ButtonOrder)
        {
            var count = groupId == PersonDossierApplicationGroups.All
                ? records.Count
                : records.Count(record =>
                    string.Equals(record.ApplicationGroupId, groupId, StringComparison.Ordinal));
            var always = groupId == PersonDossierApplicationGroups.Invitation
                || groupId == PersonDossierApplicationGroups.All;
            if (!always && count == 0)
                continue;

            groups.Add(new PersonDossierApplicationGroup
            {
                GroupId = groupId,
                Label = L(PersonDossierApplicationGroups.LabelSuffix(groupId)),
                Count = count,
                AlwaysVisible = always,
            });
        }

        return groups;
    }

    private static IEnumerable<T> Safe<T>(IList<T>? source) where T : class =>
        source == null ? Enumerable.Empty<T>() : source.Where(item => item != null);

    private static Guid? ResolveId(IObjectSpace objectSpace, Person person)
    {
        var key = objectSpace.GetKeyValue(person);
        return key switch
        {
            Guid guid => guid,
            null => null,
            _ => Guid.TryParse(Convert.ToString(key, CultureInfo.InvariantCulture), out var parsed)
                ? parsed
                : null,
        };
    }

    private static string? BuildPhotoDataUri(byte[]? photo) =>
        photo is { Length: > 0 }
            ? $"data:image/png;base64,{Convert.ToBase64String(photo)}"
            : null;

    /// <summary>Lookup catalogs render via <c>LocalizedDisplayName</c>; everything else via ToString.</summary>
    private static string Describe(object? value) => value switch
    {
        null => string.Empty,
        LookupBase lookup => lookup.LocalizedDisplayName ?? string.Empty,
        _ => value.ToString() ?? string.Empty,
    };

    private static string JoinNonEmpty(params string?[] parts) =>
        string.Join(" - ", parts.Where(part => !string.IsNullOrWhiteSpace(part)).Select(part => part!.Trim()));

    private static string FormatDate(DateTime? value) =>
        value == null || value.Value == default
            ? string.Empty
            : value.Value.ToString(CurrentDateFormat, CultureInfo.CurrentUICulture);

    private static string FormatDateWithAge(DateTime value, int age)
    {
        var formatted = FormatDate(value);
        return string.IsNullOrEmpty(formatted) ? string.Empty : $"{formatted} ({age})";
    }

    private static string L(string suffix) =>
        VisaUiMessages.Get($"PersonDossier.{suffix}");

    /// <summary>
    /// Localized text for enum-backed labels, falling back to the enum name so an unseeded key
    /// never leaks a raw resource id into the UI.
    /// </summary>
    private static string LOr(string suffix, string fallback)
    {
        var key = $"PersonDossier.{suffix}";
        var value = VisaUiMessages.Get(key);
        return string.Equals(value, key, StringComparison.Ordinal) ? fallback : value;
    }

    private static string Format(string suffix, params object[] args) =>
        VisaUiMessages.Format($"PersonDossier.{suffix}", args);
}
