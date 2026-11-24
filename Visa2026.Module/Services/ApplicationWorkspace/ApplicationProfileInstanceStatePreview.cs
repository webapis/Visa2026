using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.HeaderLinkedDocuments;
using Visa2026.Module.Services.OfficerShell;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Read-only A4 state sheet for one Application Profile instance.
/// The list keeps identity columns, people names, color tabs, and row color.
/// </summary>
public static partial class ApplicationProfileInstanceStatePreview
{
    public enum Layout
    {
        ProgressOnly = 0,
        InvitationBorderZoneWorkPermitVisa = 1,
        InvitationBorderZoneVisa = 2,
        WorkPermitLocationVisaBorderZone = 3,
        BorderZone = 4,
        WorkPermitLocations = 5,
    }

    public enum SectionId
    {
        Invitation,
        WorkPermit,
        Visa,
        BorderZone,
        Location,
        RenewedInvitations,
        ChangedInvitation,
        CancelledInvitation,
        CancelledWorkPermit,
    }

    public readonly record struct ProduceFlags(
        bool Invitation,
        bool WorkPermit,
        bool Visa,
        bool BorderZone,
        bool Location);

    public readonly record struct SectionPlan(SectionId Id, bool NestBorderZone, bool NestLocations);

    public static Layout LayoutFor(string? profileCode)
    {
        if (string.IsNullOrWhiteSpace(profileCode))
            return Layout.ProgressOnly;

        return profileCode.Trim() switch
        {
            "get_invitation_wp" or "extend_visa_wp" => Layout.InvitationBorderZoneWorkPermitVisa,
            "get_invitation" or "get_invitation_fm" or "get_invitation_service_passport"
                or "get_invitation_according_to_wp" => Layout.InvitationBorderZoneVisa,
            "workpermit_extension" or "visa_ext_according_to_wp" => Layout.WorkPermitLocationVisaBorderZone,
            "get_border_zone" => Layout.BorderZone,
            "change_workpermit" => Layout.WorkPermitLocations,
            _ => Layout.ProgressOnly,
        };
    }

    public static IReadOnlyList<SectionPlan> SectionsFor(Layout layout, ProduceFlags flags)
    {
        var invitation = false;
        var workPermit = false;
        var visa = false;
        var borderZone = false;
        var location = false;
        var borderUnderInvitation = false;
        var borderUnderVisa = false;
        var locationsUnderWorkPermit = false;

        switch (layout)
        {
            case Layout.InvitationBorderZoneWorkPermitVisa:
                invitation = flags.Invitation;
                workPermit = flags.WorkPermit;
                visa = flags.Visa;
                borderUnderInvitation = flags.BorderZone && invitation;
                borderUnderVisa = flags.BorderZone && !invitation && visa;
                borderZone = flags.BorderZone && !invitation && !visa;
                locationsUnderWorkPermit = flags.Location && workPermit;
                location = flags.Location && !workPermit;
                break;
            case Layout.InvitationBorderZoneVisa:
                invitation = flags.Invitation;
                visa = flags.Visa;
                borderUnderInvitation = flags.BorderZone && invitation;
                borderUnderVisa = flags.BorderZone && !invitation && visa;
                borderZone = flags.BorderZone && !invitation && !visa;
                break;
            case Layout.WorkPermitLocationVisaBorderZone:
                workPermit = flags.WorkPermit;
                visa = flags.Visa;
                locationsUnderWorkPermit = flags.Location && workPermit;
                location = flags.Location && !workPermit;
                borderUnderVisa = flags.BorderZone && visa;
                borderZone = flags.BorderZone && !visa;
                break;
            case Layout.BorderZone:
                borderZone = flags.BorderZone;
                break;
            case Layout.WorkPermitLocations:
                location = flags.Location;
                break;
        }

        var plans = new List<SectionPlan>(5);
        if (invitation)
            plans.Add(new SectionPlan(SectionId.Invitation, borderUnderInvitation, false));
        if (workPermit)
            plans.Add(new SectionPlan(SectionId.WorkPermit, false, locationsUnderWorkPermit));
        if (visa)
            plans.Add(new SectionPlan(SectionId.Visa, borderUnderVisa, false));
        if (borderZone)
            plans.Add(new SectionPlan(SectionId.BorderZone, false, false));
        if (location)
            plans.Add(new SectionPlan(SectionId.Location, false, false));
        return plans;
    }

    public static Sheet? Load(IObjectSpace? objectSpace, Guid instanceId)
    {
        if (objectSpace == null || instanceId == Guid.Empty)
            return null;

        var application = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(row => row.ID == instanceId)
            .Include(row => row.ApplicationProfile)
            .Include(row => row.ApplicationType!)
            .Include(row => row.VisaPeriod)
            .Include(row => row.VisaType)
            .Include(row => row.Urgency)
            .Include(row => row.ProjectContract)
            .Include(row => row.ApprovalLegSnapshots)
            .AsSplitQuery()
            .FirstOrDefault();
        if (application == null)
            return null;

        var batch = new List<ApplicationProfileInstance> { application };
        ApplicationWorkspaceIssuedResultListCoverage.ApplyTo(objectSpace, batch);
        ApplicationWorkspaceListProgressSteps.ApplyTo(objectSpace, batch);
        ApplicationProfileInstanceCompletionPercentage.ApplyTo(application);

        var layout = LayoutFor(application.ApplicationProfile?.Code);
        var flags = new ProduceFlags(
            ApplicationProfileConfigurationResolver.CanIssueInvitation(application),
            ApplicationProfileConfigurationResolver.CanIssueWorkPermit(application),
            ApplicationProfileConfigurationResolver.CanIssueVisa(application),
            ApplicationProfileConfigurationResolver.CanIssueBorderZone(application),
            ApplicationProfileConfigurationResolver.CanIssueWorkPermitLocationGrant(application));
        var plans = SectionsFor(layout, flags);

        var people = PeopleOf(application);
        var excluded = ApplicationProfileInstanceExclusionQueries.GetExcludedPeople(objectSpace, application.ID);
        var excludedIds = excluded.Count == 0 ? null : new HashSet<Guid>(excluded.Keys);
        var invitations = InvitationRows(objectSpace, application);
        var workPermits = WorkPermitRows(objectSpace, application);
        var visas = VisaRows(application);
        var borderZones = BorderZoneRows(application);
        var locations = LocationRows(application);

        var sections = new List<Section>(plans.Count);
        foreach (var plan in plans)
        {
            var rows = plan.Id switch
            {
                SectionId.Invitation => Merge(people, invitations, plan.NestBorderZone ? borderZones : null, excludedIds),
                SectionId.WorkPermit => Merge(people, workPermits, plan.NestLocations ? locations : null, excludedIds),
                SectionId.Visa => Merge(people, visas, plan.NestBorderZone ? borderZones : null, excludedIds),
                SectionId.BorderZone => Merge(people, borderZones, null, excludedIds),
                SectionId.Location => Merge(people, locations, null, excludedIds),
                _ => Array.Empty<Row>(),
            };
            IReadOnlyList<HeaderGroup>? groups = null;
            IReadOnlyList<string>? notIssued = null;
            if (plan.Id is SectionId.Invitation or SectionId.WorkPermit)
            {
                var grouped = GroupHeaders(
                    rows,
                    periodOnHeader: plan.Id == SectionId.Invitation,
                    keepDocumentInForceWhenMixed: plan.Id == SectionId.WorkPermit);
                groups = grouped.Groups;
                notIssued = grouped.NotIssuedNames;
            }
            else if (plan.Id == SectionId.Visa)
            {
                var split = SplitNotIssued(rows);
                rows = split.Issued;
                notIssued = split.NotIssuedNames;
            }

            sections.Add(new Section(plan.Id, plan.NestBorderZone, plan.NestLocations, rows, groups, notIssued));
        }

        AppendRelatedSections(objectSpace, application, sections);

        // Same clock as the list and Application result. Null until every work permit is in.
        var visaWindow = VisaIssuePassportWait.TryLoad(objectSpace, application);

        return new Sheet(
            Number: DisplayNumber(application),
            ProfileName: application.ApplicationProfile?.Name ?? string.Empty,
            ProcessNumber: application.ProcessNumber?.Trim() ?? string.Empty,
            Date: FormatDate(application.ApplicationDate),
            VisaType: application.VisaType?.ToString() ?? string.Empty,
            VisaPeriod: application.VisaPeriod?.ToString() ?? string.Empty,
            Project: application.ProjectContract?.ToString() ?? string.Empty,
            Urgency: application.Urgency?.ToString() ?? string.Empty,
            PersonCount: people.Count,
            CompletionPercent: application.CompletionPercentage,
            Steps: application.ListViewProgressSteps,
            Seretmezlik: SeretmezlikRows(objectSpace, application.ID, excluded, people),
            Sections: sections,
            VisaPassportWait: visaWindow);
    }

    public sealed record Sheet(
        string Number,
        string ProfileName,
        string ProcessNumber,
        string Date,
        string VisaType,
        string VisaPeriod,
        string Project,
        string Urgency,
        int PersonCount,
        int CompletionPercent,
        IReadOnlyList<ApplicationWorkspaceListProgressSteps.Step> Steps,
        IReadOnlyList<SeretmezlikLine> Seretmezlik,
        IReadOnlyList<Section> Sections,
        VisaIssuePassportWait? VisaPassportWait = null);

    public sealed record Section(
        SectionId Id,
        bool NestBorderZone,
        bool NestLocations,
        IReadOnlyList<Row> Rows,
        IReadOnlyList<HeaderGroup>? Groups = null,
        IReadOnlyList<string>? NotIssuedNames = null)
    {
        public bool GroupsByHeader => IsGroupedSection(Id);
    }

    /// <summary>One issued invitation or work permit, with its people listed once underneath.</summary>
    public sealed record HeaderGroup(
        string Number,
        string Period,
        bool PeriodOnHeader,
        Guid PreviewParentId,
        HeaderDocumentCopiesFamily? PreviewFamily,
        Guid PreviewPersonId,
        string? InvitationLifeKey,
        IReadOnlyList<GroupPerson> People,
        string? RelatedApplicationNumber = null,
        Guid RelatedApplicationId = default)
    {
        public bool CanPreview => PreviewParentId != Guid.Empty && PreviewFamily.HasValue;
    }

    public sealed record GroupPerson(
        string PersonName,
        string Period,
        string StateKey,
        string Tone,
        IReadOnlyList<Row> Nested,
        string? SuccessorNumber = null,
        Guid SuccessorDocumentId = default,
        string? SuccessorLifeKey = null,
        Guid SuccessorPersonId = default,
        string? SuccessorInvitationNumber = null,
        Guid SuccessorApplicationId = default,
        string? RelatedLineTitleKey = null,
        bool StrikeName = false);

    public readonly record struct HeaderGrouping(
        IReadOnlyList<HeaderGroup> Groups,
        IReadOnlyList<string> NotIssuedNames);

    /// <summary>
    /// Invitation and work-permit rows share a header. The number is kept once.
    /// People who were not issued are returned separately, in list order.
    /// </summary>
    public static HeaderGrouping GroupHeaders(
        IReadOnlyList<Row> rows,
        bool periodOnHeader,
        bool keepDocumentInForceWhenMixed = false)
    {
        var groups = new Dictionary<Guid, GroupBuilder>();
        var order = new List<GroupBuilder>();
        var notIssued = new List<string>();
        PersonBuilder? lastPerson = null;

        foreach (var row in rows)
        {
            if (row.Nested)
            {
                lastPerson?.Nested.Add(row);
                continue;
            }

            lastPerson = null;
            if (!row.CanPreview)
            {
                if (!string.IsNullOrWhiteSpace(row.PersonName))
                    notIssued.Add(row.PersonName.Trim());
                continue;
            }

            if (!groups.TryGetValue(row.PreviewParentId, out var group))
            {
                group = new GroupBuilder(row);
                groups[row.PreviewParentId] = group;
                order.Add(group);
            }
            else if (IsBlank(group.Period) && !IsBlank(row.Period))
            {
                group.Period = row.Period;
            }

            lastPerson = new PersonBuilder(row);
            group.People.Add(lastPerson);
        }

        order.Sort((a, b) => string.Compare(a.Number, b.Number, StringComparison.CurrentCultureIgnoreCase));
        var built = new List<HeaderGroup>(order.Count);
        foreach (var group in order)
        {
            group.People.Sort((a, b) => string.Compare(a.PersonName, b.PersonName, StringComparison.CurrentCultureIgnoreCase));
            var life = SharedLife(group.People);
            if (keepDocumentInForceWhenMixed
                && string.IsNullOrEmpty(life)
                && group.People.Any(person =>
                    string.IsNullOrEmpty(person.InvitationLifeKey)
                    || string.Equals(person.InvitationLifeKey, InvitationLaterLife.InForce, StringComparison.Ordinal)))
                life = InvitationLaterLife.InForce;
            var personId = group.People.Count == 1 ? group.People[0].PersonId : Guid.Empty;
            built.Add(new HeaderGroup(
                group.Number,
                group.Period,
                periodOnHeader,
                group.PreviewParentId,
                group.PreviewFamily,
                personId,
                life,
                group.People.Select(person => person.ToPerson()).ToList()));
        }

        return new HeaderGrouping(built, notIssued);
    }

    /// <summary>
    /// Issued visa rows stay in the table. People with no visa yet are names only.
    /// A nested border zone stays on the table with the person it belongs to.
    /// </summary>
    public static (IReadOnlyList<Row> Issued, IReadOnlyList<string> NotIssuedNames) SplitNotIssued(IReadOnlyList<Row> rows)
    {
        var issued = new List<Row>();
        var notIssued = new List<string>();
        Row? waiting = null;
        var waitingNested = new List<Row>();

        void FlushWaiting()
        {
            if (waiting == null)
                return;
            if (waitingNested.Count == 0)
            {
                if (!string.IsNullOrWhiteSpace(waiting.PersonName))
                    notIssued.Add(waiting.PersonName.Trim());
            }
            else
            {
                issued.Add(waiting);
                issued.AddRange(waitingNested);
            }

            waiting = null;
            waitingNested.Clear();
        }

        foreach (var row in rows)
        {
            if (row.Nested)
            {
                if (waiting != null)
                    waitingNested.Add(row);
                else
                    issued.Add(row);
                continue;
            }

            FlushWaiting();
            if (row.CanPreview)
                issued.Add(row);
            else
                waiting = row;
        }

        FlushWaiting();
        return (issued, notIssued);
    }

    private static string? SharedLife(IReadOnlyList<PersonBuilder> people)
    {
        string? life = null;
        foreach (var person in people)
        {
            if (life == null)
            {
                life = person.InvitationLifeKey;
                continue;
            }

            if (!string.Equals(life, person.InvitationLifeKey, StringComparison.Ordinal))
                return null;
        }

        return life;
    }

    private static bool IsBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "—";

    private sealed class GroupBuilder
    {
        public GroupBuilder(Row row)
        {
            Number = string.IsNullOrWhiteSpace(row.Document) ? "—" : row.Document.Trim();
            Period = row.Period;
            PreviewParentId = row.PreviewParentId;
            PreviewFamily = row.PreviewFamily;
        }

        public string Number { get; }
        public string Period { get; set; }
        public Guid PreviewParentId { get; }
        public HeaderDocumentCopiesFamily? PreviewFamily { get; }
        public List<PersonBuilder> People { get; } = new();
    }

    private sealed class PersonBuilder
    {
        public PersonBuilder(Row row)
        {
            PersonName = row.PersonName;
            Period = row.Period;
            StateKey = row.StateKey;
            Tone = row.Tone;
            PersonId = row.PreviewPersonId;
            InvitationLifeKey = row.InvitationLifeKey;
            SuccessorNumber = row.SuccessorNumber;
            SuccessorDocumentId = row.SuccessorDocumentId;
            SuccessorLifeKey = row.SuccessorLifeKey;
            SuccessorInvitationNumber = row.SuccessorInvitationNumber;
            SuccessorApplicationId = row.SuccessorApplicationId;
            RelatedLineTitleKey = row.RelatedLineTitleKey;
            StrikeName = row.StrikeName;
        }

        public string PersonName { get; }
        public string Period { get; }
        public string StateKey { get; }
        public string Tone { get; }
        public Guid PersonId { get; }
        public string? InvitationLifeKey { get; }
        public string? SuccessorNumber { get; }
        public Guid SuccessorDocumentId { get; }
        public string? SuccessorLifeKey { get; }
        public string? SuccessorInvitationNumber { get; }
        public Guid SuccessorApplicationId { get; }
        public string? RelatedLineTitleKey { get; }
        public bool StrikeName { get; }
        public List<Row> Nested { get; } = new();

        public GroupPerson ToPerson() =>
            new(
                PersonName,
                Period,
                StateKey,
                Tone,
                Nested,
                SuccessorNumber,
                SuccessorDocumentId,
                SuccessorLifeKey,
                PersonId,
                SuccessorInvitationNumber,
                SuccessorApplicationId,
                RelatedLineTitleKey,
                StrikeName);
    }

    public sealed record Row(
        string PersonName,
        string Document,
        string Period,
        string StateKey,
        string Tone,
        bool Nested,
        Guid PreviewParentId = default,
        HeaderDocumentCopiesFamily? PreviewFamily = null,
        Guid PreviewPersonId = default,
        string? InvitationLifeKey = null,
        string? SuccessorNumber = null,
        Guid SuccessorDocumentId = default,
        string? SuccessorLifeKey = null,
        string? SuccessorInvitationNumber = null,
        Guid SuccessorApplicationId = default,
        string? RelatedLineTitleKey = null,
        bool StrikeName = false)
    {
        /// <summary>An issued header opens its copies. A row that was not issued stays plain text.</summary>
        public bool CanPreview => PreviewParentId != Guid.Empty && PreviewFamily.HasValue;
    }

    public sealed record SeretmezlikLine(string PersonName, string LetterNumber, string Period, string? Organization = null);

    /// <summary>
    /// Submit-to label for a Seretmezlik letter: ministry short name, then ministry name, then migration service.
    /// </summary>
    public static string? DestinationLabel(string? ministryShortName, string? ministryName, string? migrationServiceName)
    {
        var shortName = ministryShortName?.Trim();
        if (!string.IsNullOrWhiteSpace(shortName))
            return shortName;
        var name = ministryName?.Trim();
        if (!string.IsNullOrWhiteSpace(name))
            return name;
        var migration = migrationServiceName?.Trim();
        return string.IsNullOrWhiteSpace(migration) ? null : migration;
    }

    /// <summary>Letter number, with the Submit-to organization after it when the letter has one.</summary>
    public static string SeretmezlikLetterText(string? letterNumber, string? organization)
    {
        var number = string.IsNullOrWhiteSpace(letterNumber) ? "—" : letterNumber.Trim();
        if (string.IsNullOrWhiteSpace(organization))
            return number;
        return number + " · " + organization.Trim();
    }

    private sealed record Doc(
        Guid PersonId,
        string PersonName,
        string Document,
        string Period,
        string StateKey,
        string Tone,
        Guid PreviewParentId = default,
        HeaderDocumentCopiesFamily? PreviewFamily = null,
        string? InvitationLifeKey = null,
        string? SuccessorNumber = null,
        Guid SuccessorDocumentId = default,
        string? SuccessorLifeKey = null,
        string? SuccessorInvitationNumber = null,
        Guid SuccessorApplicationId = default,
        string? RelatedLineTitleKey = null,
        bool StrikeName = false);

    private static List<(Guid Id, string Name)> PeopleOf(ApplicationProfileInstance application)
    {
        var people = new List<(Guid Id, string Name)>();
        if (application.People == null)
            return people;

        foreach (var person in application.People)
        {
            if (person == null || person.ID == Guid.Empty)
                continue;
            people.Add((person.ID, NameOf(person)));
        }

        people.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        return people;
    }

    /// <summary>A Seretmezlik person is already listed on that band, not as “not issued”.</summary>
    public static bool ShowAsNotIssued(Guid personId, IReadOnlySet<Guid>? excludedPersonIds) =>
        excludedPersonIds == null || personId == Guid.Empty || !excludedPersonIds.Contains(personId);

    private static IReadOnlyList<SeretmezlikLine> SeretmezlikRows(
        IObjectSpace objectSpace,
        Guid instanceId,
        IReadOnlyDictionary<Guid, ApplicationProfileInstanceExcludedPerson> excluded,
        IReadOnlyList<(Guid Id, string Name)> people)
    {
        if (excluded.Count == 0)
            return Array.Empty<SeretmezlikLine>();

        var organizations = OrganizationsByLetterNumber(objectSpace, instanceId);

        var names = people.ToDictionary(person => person.Id, person => person.Name);
        var missing = excluded.Keys.Where(id => !names.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var extra = objectSpace.GetObjectsQuery<Person>()
                .Where(person => missing.Contains(person.ID))
                .ToList();
            foreach (var person in extra)
                names[person.ID] = NameOf(person);
        }

        var lines = new List<SeretmezlikLine>();
        foreach (var pair in excluded.OrderBy(pair => names.TryGetValue(pair.Key, out var name) ? name : string.Empty, StringComparer.CurrentCultureIgnoreCase))
        {
            var personName = names.TryGetValue(pair.Key, out var known) ? known : "—";
            foreach (var letter in pair.Value.AllLetters())
            {
                var number = string.IsNullOrWhiteSpace(letter.LetterNumber) ? "—" : letter.LetterNumber.Trim();
                organizations.TryGetValue(number, out var organization);
                lines.Add(new SeretmezlikLine(personName, number, FormatDate(letter.LetterDate), organization));
            }
        }

        return lines;
    }

    /// <summary>
    /// Letter number → Submit-to organization for Seretmezlik cases aimed at this application.
    /// The number key matches <see cref="ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber"/>.
    /// </summary>
    private static Dictionary<string, string> OrganizationsByLetterNumber(IObjectSpace objectSpace, Guid instanceId)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (objectSpace == null || instanceId == Guid.Empty)
            return map;

        var cases = objectSpace.GetObjectsQuery<ApplicationProfileInstance>()
            .Where(instance => instance.TargetApplicationProfileInstanceId == instanceId)
            .ToList();
        if (cases.Count == 0)
            return map;

        var ministryIds = cases
            .Select(instance => instance.SubmitToApprovingMinistryId)
            .Where(id => id is Guid value && value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();
        var migrationIds = cases
            .Select(instance => instance.SubmitToMigrationServiceId)
            .Where(id => id is Guid value && value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        var ministries = ministryIds.Count == 0
            ? new Dictionary<Guid, (string? ShortName, string? Name)>()
            : objectSpace.GetObjectsQuery<ApprovingMinistry>()
                .Where(ministry => ministryIds.Contains(ministry.ID))
                .Select(ministry => new { ministry.ID, ministry.ShortNameTm, ministry.NameTm })
                .ToDictionary(ministry => ministry.ID, ministry => ((string?)ministry.ShortNameTm, (string?)ministry.NameTm));
        var migrations = migrationIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : objectSpace.GetObjectsQuery<MigrationService>()
                .Where(service => migrationIds.Contains(service.ID))
                .Select(service => new { service.ID, service.NameTm })
                .ToDictionary(service => service.ID, service => (string?)service.NameTm);

        foreach (var seretmezlik in cases)
        {
            string? shortName = null;
            string? ministryName = null;
            string? migrationName = null;
            if (seretmezlik.SubmitToApprovingMinistryId is Guid ministryId
                && ministries.TryGetValue(ministryId, out var ministry))
            {
                shortName = ministry.Item1;
                ministryName = ministry.Item2;
            }

            if (seretmezlik.SubmitToMigrationServiceId is Guid migrationId
                && migrations.TryGetValue(migrationId, out var migration))
                migrationName = migration;

            var organization = DestinationLabel(shortName, ministryName, migrationName);
            if (string.IsNullOrWhiteSpace(organization))
                continue;

            var number = ApplicationProfileInstanceExclusionQueries.FormatSeretmezlikLetterNumber(seretmezlik);
            if (string.IsNullOrWhiteSpace(number))
                continue;
            map[number.Trim()] = organization;
        }

        return map;
    }

    private static List<Doc> InvitationRows(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        var items = new List<InvitationItem>();
        if (application.Invitations != null)
        {
            foreach (var header in application.Invitations)
            {
                if (header?.InvitationItems == null)
                    continue;
                items.AddRange(header.InvitationItems.Where(item => item != null));
            }
        }

        var itemIds = items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList();
        var used = IssuedDocumentLifecycle.LoadUsedInvitationItemIds(objectSpace, itemIds);
        var cancelled = CompletedInvitationItems(objectSpace, itemIds, ApplicationProfileActionFamily.Cancellation);
        var changed = CompletedInvitationItems(objectSpace, itemIds, ApplicationProfileActionFamily.Change);
        var successors = Successors(application);
        var cancellations = LoadCancellationLinks(objectSpace, application.ID, itemIds);

        var rows = new List<Doc>(items.Count);
        foreach (var item in items)
        {
            var person = item.Person;
            if (person == null || person.ID == Guid.Empty)
                continue;

            var header = item.Invitation;
            var parentId = header?.ID ?? Guid.Empty;
            var usedNow = item.IssuedVisa != null || used.Contains(item.ID);
            var life = InvitationLaterLife.Resolve(
                cancelled.Contains(item.ID) || IssuedDocumentLifecycle.IsCancelled(item),
                changed.Contains(item.ID) || IssuedDocumentLifecycle.IsChanged(item),
                usedNow,
                InvitationLaterLife.IsExpired(item.ExpirationDate));
            var (key, tone) = Life(life);
            string? successorNumber = null;
            var successorDocumentId = Guid.Empty;
            string? successorLife = null;
            string? successorInvitationNumber = null;
            var successorApplicationId = Guid.Empty;
            string? relatedLineTitleKey = null;
            if (string.Equals(life, InvitationLaterLife.Changed, StringComparison.Ordinal)
                && successors.TryGetValue(person.ID, out var successor)
                && successor.DocumentId != Guid.Empty)
            {
                successorNumber = string.IsNullOrWhiteSpace(successor.ApplicationNumber)
                    ? null
                    : successor.ApplicationNumber.Trim();
                successorDocumentId = successor.DocumentId;
                successorLife = string.IsNullOrEmpty(successor.State)
                    ? InvitationLaterLife.InForce
                    : successor.State;
                successorInvitationNumber = string.IsNullOrWhiteSpace(successor.InvitationNumber)
                    ? null
                    : successor.InvitationNumber.Trim();
                successorApplicationId = successor.ApplicationId;
                relatedLineTitleKey = RelatedSuccessorTitleKey;
            }
            else if (string.Equals(life, InvitationLaterLife.Cancelled, StringComparison.Ordinal)
                && cancellations.TryGetValue(person.ID, out var cancel)
                && cancel.ApplicationId != Guid.Empty)
            {
                successorNumber = string.IsNullOrWhiteSpace(cancel.ApplicationNumber)
                    ? null
                    : cancel.ApplicationNumber.Trim();
                successorApplicationId = cancel.ApplicationId;
                relatedLineTitleKey = RelatedCancelledInvitationTitleKey;
            }

            rows.Add(new Doc(
                person.ID,
                NameOf(person),
                Text(header?.InvitationNumber),
                FormatPeriod(header?.IssuedDate, header?.ExpirationDate),
                key,
                tone,
                parentId,
                parentId == Guid.Empty ? null : HeaderDocumentCopiesFamily.Invitation,
                parentId == Guid.Empty ? null : life ?? InvitationLaterLife.InForce,
                successorNumber,
                successorDocumentId,
                successorLife,
                successorInvitationNumber,
                successorApplicationId,
                relatedLineTitleKey,
                StrikeNameForLife(life)));
        }

        return rows;
    }

    private static HashSet<Guid> CompletedInvitationItems(
        IObjectSpace objectSpace,
        IReadOnlyCollection<Guid> itemIds,
        ApplicationProfileActionFamily family)
    {
        if (itemIds.Count == 0)
            return [];

        var issued = ApplicationProfileInstanceProgressStateCodes.ProcessIssued;
        return objectSpace.GetObjectsQuery<InvitationItem>()
            .Where(item => itemIds.Contains(item.ID)
                && item.ApplicationProfileInstances.Any(instance =>
                    instance.ApplicationProfile != null
                    && instance.ApplicationProfile.ActionFamily == family
                    && instance.LatestPrimaryStateCode == issued))
            .Select(item => item.ID)
            .ToHashSet();
    }

    private static Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement> Successors(
        ApplicationProfileInstance application)
    {
        var successors = new Dictionary<Guid, ApplicationListLinkedPeople.InvitationReplacement>();
        foreach (var person in application.ListViewLinkedPeople)
        {
            if (person.PersonId == Guid.Empty)
                continue;
            foreach (var mark in person.Marks)
            {
                if (mark.Replacement != null && mark.Replacement.DocumentId != Guid.Empty)
                    successors[person.PersonId] = mark.Replacement;
            }
        }

        return successors;
    }

    private static List<Doc> WorkPermitRows(IObjectSpace objectSpace, ApplicationProfileInstance application)
    {
        var items = new List<WorkPermitItem>();
        if (application.WorkPermits != null)
        {
            foreach (var header in application.WorkPermits)
            {
                if (header?.WorkPermitItems == null)
                    continue;
                foreach (var item in header.WorkPermitItems)
                {
                    if (item != null)
                        items.Add(item);
                }
            }
        }

        var itemIds = items.Select(item => item.ID).Where(id => id != Guid.Empty).ToList();
        var cancellations = LoadWorkPermitCancellations(objectSpace, itemIds);
        var cancelled = cancellations.ItemIds;

        var rows = new List<Doc>(items.Count);
        foreach (var item in items)
        {
            var person = item.Person;
            if (person == null || person.ID == Guid.Empty)
                continue;

            var header = item.WorkPermit;
            // Work permits are not changeable — only cancelled (and expiry) later life.
            var life = InvitationLaterLife.Resolve(
                cancelled.Contains(item.ID) || IssuedDocumentLifecycle.IsCancelled(item),
                changed: false,
                used: false,
                expired: InvitationLaterLife.IsExpired(item.ExpirationDate));
            var (key, tone) = Life(life);
            var parentId = header?.ID ?? Guid.Empty;
            string? successorNumber = null;
            var successorApplicationId = Guid.Empty;
            string? relatedLineTitleKey = null;
            var isCancelled = string.Equals(life, InvitationLaterLife.Cancelled, StringComparison.Ordinal);
            if (isCancelled
                && cancellations.ByPerson.TryGetValue(person.ID, out var cancel)
                && cancel.ApplicationId != Guid.Empty)
            {
                successorNumber = string.IsNullOrWhiteSpace(cancel.ApplicationNumber)
                    ? null
                    : cancel.ApplicationNumber.Trim();
                successorApplicationId = cancel.ApplicationId;
                relatedLineTitleKey = RelatedCancelledWorkPermitTitleKey;
            }
            rows.Add(new Doc(
                person.ID,
                NameOf(person),
                Text(header?.WorkPermitNumber),
                FormatPeriod(item.StartDate, item.ExpirationDate),
                key,
                tone,
                parentId,
                parentId == Guid.Empty ? null : HeaderDocumentCopiesFamily.WorkPermit,
                parentId == Guid.Empty ? null : life ?? InvitationLaterLife.InForce,
                successorNumber,
                SuccessorDocumentId: default,
                SuccessorLifeKey: null,
                SuccessorInvitationNumber: null,
                successorApplicationId,
                relatedLineTitleKey,
                StrikeName: isCancelled));
        }

        return rows;
    }

    private static List<Doc> VisaRows(ApplicationProfileInstance application)
    {
        var rows = new List<Doc>();
        if (application.IssuedVisas == null)
            return rows;

        foreach (var visa in application.IssuedVisas)
        {
            var person = visa?.Passport?.Person;
            if (visa == null || person == null || person.ID == Guid.Empty)
                continue;

            var life = InvitationLaterLife.Resolve(
                IssuedDocumentLifecycle.IsCancelled(visa),
                IssuedDocumentLifecycle.IsChanged(visa),
                used: false,
                expired: InvitationLaterLife.IsExpired(visa.ExpirationDate));
            var (key, tone) = Life(life);
            var number = string.IsNullOrWhiteSpace(visa.VisaNumber)
                ? visa.VisaCategory?.ToString() ?? visa.VisaType?.ToString()
                : visa.VisaNumber;
            var parentId = visa.ID;
            rows.Add(new Doc(
                person.ID,
                NameOf(person),
                Text(number),
                FormatPeriod(visa.StartDate, visa.ExpirationDate),
                key,
                tone,
                parentId,
                parentId == Guid.Empty ? null : HeaderDocumentCopiesFamily.Visa));
        }

        return rows;
    }

    private static List<Doc> BorderZoneRows(ApplicationProfileInstance application)
    {
        var rows = new List<Doc>();
        if (application.BorderZones == null)
            return rows;

        foreach (var zone in application.BorderZones)
        {
            if (zone?.BorderZoneItems == null)
                continue;
            foreach (var item in zone.BorderZoneItems)
            {
                var person = item?.Person;
                if (item == null || person == null || person.ID == Guid.Empty)
                    continue;

                var life = InvitationLaterLife.Resolve(
                    IssuedDocumentLifecycle.IsCancelled(item) || IssuedDocumentLifecycle.IsCancelled(zone),
                    IssuedDocumentLifecycle.IsChanged(item),
                    used: false,
                    expired: InvitationLaterLife.IsExpired(zone.ExpirationDate));
                var (key, tone) = Life(life);
                var label = string.IsNullOrWhiteSpace(item.BorderZoneLocation)
                    ? zone.BorderZoneNumber
                    : item.BorderZoneLocation;
                var parentId = zone.ID;
                rows.Add(new Doc(
                    person.ID,
                    NameOf(person),
                    Text(label),
                    FormatPeriod(zone.StartDate, zone.ExpirationDate),
                    key,
                    tone,
                    parentId,
                    parentId == Guid.Empty ? null : HeaderDocumentCopiesFamily.BorderZone));
            }
        }

        return rows;
    }

    private static List<Doc> LocationRows(ApplicationProfileInstance application)
    {
        var rows = new List<Doc>();
        if (application.WorkPermitLocationGrants == null)
            return rows;

        foreach (var grant in application.WorkPermitLocationGrants)
        {
            if (grant?.Items == null)
                continue;
            foreach (var item in grant.Items)
            {
                var person = item?.Person;
                if (item == null || person == null || person.ID == Guid.Empty)
                    continue;

                var label = string.IsNullOrWhiteSpace(item.GrantedLocations)
                    ? grant.GrantNumber
                    : item.GrantedLocations;
                var parentId = grant.ID;
                rows.Add(new Doc(
                    person.ID,
                    NameOf(person),
                    Text(label),
                    FormatDate(grant.IssuedDate),
                    "ApplicationProfileInstance.List.Mark.InvitationInForce",
                    "inforce",
                    parentId,
                    parentId == Guid.Empty ? null : HeaderDocumentCopiesFamily.WorkPermitLocationGrant));
            }
        }

        return rows;
    }

    private static IReadOnlyList<Row> Merge(
        IReadOnlyList<(Guid Id, string Name)> people,
        IReadOnlyList<Doc> documents,
        IReadOnlyList<Doc>? nested,
        IReadOnlySet<Guid>? excludedPersonIds)
    {
        var order = new List<(Guid Id, string Name)>(people);
        var seen = new HashSet<Guid>(people.Select(person => person.Id));
        foreach (var doc in documents.Concat(nested ?? Array.Empty<Doc>()))
        {
            if (doc.PersonId == Guid.Empty || !seen.Add(doc.PersonId))
                continue;
            order.Add((doc.PersonId, doc.PersonName));
        }

        order.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        var rows = new List<Row>();
        foreach (var person in order)
        {
            var own = documents.Where(doc => doc.PersonId == person.Id).ToList();
            if (own.Count == 0)
            {
                if (!ShowAsNotIssued(person.Id, excludedPersonIds))
                    continue;
                rows.Add(new Row(
                    person.Name,
                    "—",
                    "—",
                    "ApplicationProfileInstance.List.Mark.NotIssuedTitle",
                    "waiting",
                    Nested: false));
            }
            else
            {
                foreach (var doc in own)
                    rows.Add(IssuedRow(person.Name, doc, nested: false));
            }

            if (nested == null)
                continue;

            foreach (var child in nested.Where(doc => doc.PersonId == person.Id))
                rows.Add(IssuedRow(string.Empty, child, nested: true));
        }

        return rows;
    }

    private static Row IssuedRow(string personName, Doc doc, bool nested) =>
        new(
            personName,
            doc.Document,
            doc.Period,
            doc.StateKey,
            doc.Tone,
            nested,
            doc.PreviewParentId,
            doc.PreviewFamily,
            doc.PersonId,
            doc.InvitationLifeKey,
            doc.SuccessorNumber,
            doc.SuccessorDocumentId,
            doc.SuccessorLifeKey,
            doc.SuccessorInvitationNumber,
            doc.SuccessorApplicationId,
            doc.RelatedLineTitleKey,
            doc.StrikeName);

    /// <summary>Pill for a shared invitation or work-permit state. In force when the life is empty.</summary>
    public static (string Key, string Tone) DocumentLife(string? life) => Life(life);

    private static (string Key, string Tone) Life(string? life) =>
        life switch
        {
            InvitationLaterLife.Used => ("ApplicationProfileInstance.List.Mark.InvitationUsedTitle", "used"),
            InvitationLaterLife.ExpiredUnused => ("ApplicationProfileInstance.List.Mark.DocumentExpiredTitle", "expired"),
            InvitationLaterLife.Cancelled => ("ApplicationProfileInstance.List.Mark.InvitationCancelledTitle", "cancelled"),
            InvitationLaterLife.Changed => ("ApplicationProfileInstance.List.Mark.InvitationChangedTitle", "changed"),
            _ => ("ApplicationProfileInstance.List.Mark.InvitationInForce", "inforce"),
        };

    private static string DisplayNumber(ApplicationProfileInstance application) =>
        !string.IsNullOrWhiteSpace(application.FullApplicationNumber)
            ? application.FullApplicationNumber.Trim()
            : application.ApplicationNumber?.Trim() ?? string.Empty;

    private static string NameOf(Person person) =>
        string.IsNullOrWhiteSpace(person.FullName) ? "—" : person.FullName.Trim();

    private static string Text(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string FormatPeriod(DateTime? start, DateTime? end)
    {
        var left = FormatDate(start);
        var right = FormatDate(end);
        if (left == "—" && right == "—")
            return "—";
        if (left == "—")
            return right;
        if (right == "—")
            return left;
        return left + " – " + right;
    }

    private static string FormatPeriod(DateTime start, DateTime end) =>
        FormatPeriod(Present(start), Present(end));

    private static string FormatDate(DateTime? value) =>
        value is DateTime date && date != default
            ? date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)
            : "—";

    private static string FormatDate(DateTime value) => FormatDate(Present(value));

    private static DateTime? Present(DateTime value) =>
        value == default ? null : value;
}
