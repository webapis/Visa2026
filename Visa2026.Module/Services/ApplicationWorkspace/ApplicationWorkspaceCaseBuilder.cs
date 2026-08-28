using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonRoster;

namespace Visa2026.Module.Services.ApplicationWorkspace;

internal static class ApplicationWorkspaceCaseBuilder
{
    private static readonly string[] ProgressStepKeys = ["office", "ministry", "migration", "complete"];

    private static readonly string[] ProgressStepLabels =
    [
        "Office preparation",
        "Ministry review",
        "Migration service",
        "Complete",
    ];

    private static readonly string[] LinkedTones = ["blue", "purple", "green", "orange", "teal"];

    public static ApplicationWorkspaceCaseView Build(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationWorkspaceTab> tabs,
        ApplicationProfileInstanceProgressSlaResult sla,
        ApplicationWorkspaceCaseChrome chrome,
        IObjectSpace? objectSpace = null) =>
        BuildCore(application, profile, tabs, sla, chrome, objectSpace);

    public static ApplicationWorkspaceCaseView BuildFromSnapshot(ApplicationWorkspaceSnapshot snapshot)
    {
        var chrome = snapshot.CaseChrome;
        if (string.IsNullOrWhiteSpace(chrome.ProfileTemplateName))
        {
            chrome = new ApplicationWorkspaceCaseChrome
            {
                DisplayNumber = chrome.DisplayNumber,
                ProcessNumber = chrome.ProcessNumber,
                TemplateFamilyKey = chrome.TemplateFamilyKey,
                TemplateFamilyLabel = chrome.TemplateFamilyLabel,
                StartedOn = chrome.StartedOn,
                CurrentStep = chrome.CurrentStep,
                ProjectName = chrome.ProjectName,
                SlaDaysRemaining = chrome.SlaDaysRemaining,
                PeopleNames = chrome.PeopleNames,
                MergedFromCount = chrome.MergedFromCount,
                ProfileTemplateName = snapshot.Profile.Title,
            };
        }

        return BuildCore(null, null, snapshot.Tabs, default, chrome, null);
    }

    private static ApplicationWorkspaceCaseView BuildCore(
        ApplicationProfileInstance? application,
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationWorkspaceTab> tabs,
        ApplicationProfileInstanceProgressSlaResult sla,
        ApplicationWorkspaceCaseChrome chrome,
        IObjectSpace? objectSpace)
    {
        var tabMap = tabs.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);
        var rosterPeople = application?.People?.OrderBy(p => p.LastName).ThenBy(p => p.FirstName).ToList() ?? [];
        var rosterLinks = application?.PersonResolvedLinks?.ToList() ?? [];
        var people = BuildPeople(tabMap, chrome.PeopleNames, application, rosterPeople, rosterLinks);
        var linkedSummary = BuildLinkedSummary(application, rosterLinks, people);
        var progressSteps = application != null
            ? ApplicationWorkspaceProgressTimeline.Build(application, profile, sla, objectSpace)
            : BuildProgressStepsFromChrome(chrome);
        var slaDashboard = application != null
            ? ApplicationWorkspaceSlaDashboardBuilder.Build(application, profile, sla, chrome, progressSteps)
            : BuildSlaFromChrome(chrome, progressSteps);
        var syncedChrome = ApplicationWorkspaceSlaDashboardBuilder.WithHeaderRemaining(chrome, slaDashboard);

        var headerFields = application != null
            ? ApplicationWorkspaceCaseHeaderFieldsHelper.Build(application, profile, objectSpace)
            : Array.Empty<ApplicationWorkspaceCaseHeaderField>();

        return new ApplicationWorkspaceCaseView
        {
            Chrome = syncedChrome,
            HeaderFields = headerFields,
            SummaryTiles = headerFields.Count > 0
                ? headerFields.Select(field => Tile(field.Label, field.DisplayValue, field.Tone, field.Glyph, field.FillState, field.Key)).ToList()
                : application != null
                    ? Array.Empty<ApplicationWorkspaceCaseSummaryTile>()
                    : BuildSummaryTilesFromChrome(chrome),
            LinkedRecordTiles = BuildLinkedTiles(application, rosterLinks, tabs, rosterPeople.Count),
            IssuedRecordTiles = BuildIssuedTiles(application, objectSpace),
            ProgressSteps = progressSteps,
            People = people,
            Activities = application != null
                ? BuildActivities(application, chrome)
                : BuildActivitiesFromChrome(chrome),
            PeopleSummary = BuildPeopleSummary(people),
            LinkedRecordsSummary = linkedSummary,
            Sla = slaDashboard,
        };
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseSummaryTile> BuildSummaryTilesFromChrome(
        ApplicationWorkspaceCaseChrome chrome) =>
    [
        Tile("Visa type", "WP", "blue", "🛂"),
        Tile("Category", "B", "purple", "◆"),
        Tile("Period", "6 months", "green", "📅"),
        Tile("Project / Contract", chrome.ProjectName, "orange", "💼"),
        Tile("Entry checkpoint", "Ashgabat", "blue", "📍"),
    ];

    private static IReadOnlyList<ApplicationWorkspaceCaseProgressStep> BuildProgressStepsFromChrome(
        ApplicationWorkspaceCaseChrome chrome)
    {
        var currentIndex = ResolveProgressIndex(chrome.CurrentStep);
        var steps = new List<ApplicationWorkspaceCaseProgressStep>();
        for (var i = 0; i < ProgressStepLabels.Length; i++)
        {
            var state = i < currentIndex ? "done" : i == currentIndex ? "current" : "pending";
            steps.Add(new ApplicationWorkspaceCaseProgressStep
            {
                Key = ProgressStepKeys[i],
                Label = ProgressStepLabels[i],
                Date = state != "pending" ? chrome.StartedOn : string.Empty,
                State = state,
                CurrentStateLabel = state == "current" ? chrome.CurrentStep : string.Empty,
                SlaDaysRemaining = state == "current" ? chrome.SlaDaysRemaining : null,
                SlaTargetDate = state == "current" ? "19 Aug 2026" : string.Empty,
                OutcomeKind = state == "done" ? "ok" : state == "current" ? "current" : "pending",
            });
        }

        return steps;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseActivity> BuildActivitiesFromChrome(
        ApplicationWorkspaceCaseChrome chrome)
    {
        var items = new List<ApplicationWorkspaceCaseActivity>();
        if (chrome.MergedFromCount is int merged && merged > 1)
        {
            items.Add(new ApplicationWorkspaceCaseActivity
            {
                Title = $"Merged {merged} profiles",
                Subtitle = chrome.StartedOn,
            });
        }

        if (!string.IsNullOrWhiteSpace(chrome.ProcessNumber))
        {
            items.Add(new ApplicationWorkspaceCaseActivity
            {
                Title = "Number assigned",
                Subtitle = chrome.ProcessNumber,
            });
        }

        items.Add(new ApplicationWorkspaceCaseActivity
        {
            Title = $"Progress: {chrome.CurrentStep}",
            Subtitle = chrome.StartedOn,
        });
        return items;
    }

    private static ApplicationWorkspaceCaseSlaDashboard BuildSlaFromChrome(
        ApplicationWorkspaceCaseChrome chrome,
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep> progressSteps)
    {
        var remaining = chrome.SlaDaysRemaining ?? 12;
        var current = progressSteps.FirstOrDefault(s => s.State == "current");
        return new ApplicationWorkspaceCaseSlaDashboard
        {
            IsTerminal = false,
            ProcessOutcome = "inprocess",
            CaseStatus = remaining <= 10 ? "Due soon" : "On track",
            CaseDaysRemaining = remaining,
            TotalSlaDays = 45,
            ElapsedDays = 33,
            CurrentStepDaysRemaining = current?.SlaDaysRemaining ?? remaining,
            CurrentStepDueDate = current?.SlaTargetDate ?? "22 Aug 2026",
            CurrentStepLabel = current?.Label ?? "Current step",
            StartedOn = chrome.StartedOn,
            MinistryDueDate = "22 Aug 2026",
            ExpectedCompletionDate = "15 Sep 2026",
            MigrationSlaLabel = "45 days",
            ProfileSlaSource = chrome.ProfileTemplateName,
            AlertMessage = "Ministry review deadline approaching. Due in 8 days on 22 Aug 2026. Ensure all reviews and required actions are completed on time.",
            Deadlines =
            [
                new() { Step = "ApplicationProfileInstance received", DueDate = chrome.StartedOn, DaysLeft = "—", Status = "completed" },
                new() { Step = "Document check", DueDate = "14 Aug 2026", DaysLeft = "2", Status = "completed" },
                new() { Step = "Ministry review", DueDate = "22 Aug 2026", DaysLeft = "8", Status = "inprogress", IsCurrent = true },
                new() { Step = "Decision", DueDate = "05 Sep 2026", DaysLeft = "22", Status = "pending" },
                new() { Step = "Finalization", DueDate = "15 Sep 2026", DaysLeft = "35", Status = "pending" },
            ],
        };
    }

    private static ApplicationWorkspaceCaseSummaryTile Tile(
        string label,
        string value,
        string tone,
        string glyph,
        ApplicationWorkspaceCaseSummaryFillState fillState = ApplicationWorkspaceCaseSummaryFillState.Default,
        string key = "") =>
        new()
        {
            Key = key,
            Label = label,
            Value = string.IsNullOrWhiteSpace(value) ? "—" : value,
            Tone = tone,
            Glyph = glyph,
            FillState = fillState,
        };

    private static IReadOnlyList<ApplicationWorkspaceCaseLinkedTile> BuildLinkedTiles(
        ApplicationProfileInstance? application,
        IReadOnlyList<ApplicationProfileInstancePersonResolvedLink> rosterLinks,
        IReadOnlyList<ApplicationWorkspaceTab> tabs,
        int personCount)
    {
        if (application != null)
        {
            var tiles = new List<ApplicationWorkspaceCaseLinkedTile>();
            var toneIndex = 0;
            var people = Math.Max(personCount, 1);
            foreach (var def in ApplicationWorkspaceLinkedRecordsCatalog.Definitions)
            {
                if (!ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, def.Kind))
                    continue;

                var perPerson = Math.Max(
                    ApplicationProfilePersonLastCount.For(application, def.Kind),
                    1);
                var expected = perPerson * people;
                var count = ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(rosterLinks, def.Kind);
                tiles.Add(new ApplicationWorkspaceCaseLinkedTile
                {
                    TabKey = def.TabKey,
                    Label = def.Label,
                    Count = count,
                    ExpectedCount = expected,
                    Tone = LinkedTones[toneIndex % LinkedTones.Length],
                    Glyph = def.Glyph,
                });
                toneIndex++;
            }

            return tiles;
        }

        var fallback = new List<ApplicationWorkspaceCaseLinkedTile>();
        var fallbackTone = 0;
        foreach (var tab in tabs.Where(t => t.Visible && t.Key != "person"))
        {
            fallback.Add(new ApplicationWorkspaceCaseLinkedTile
            {
                TabKey = tab.Key,
                Label = tab.Label,
                Count = tab.Rows.Count,
                Tone = LinkedTones[fallbackTone % LinkedTones.Length],
                Glyph = ApplicationWorkspaceLinkedRecordsCatalog.GlyphForTabKey(tab.Key),
            });
            fallbackTone++;
        }

        return fallback;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseIssuedTile> BuildIssuedTiles(
        ApplicationProfileInstance? application,
        IObjectSpace? objectSpace)
    {
        if (application == null)
            return Array.Empty<ApplicationWorkspaceCaseIssuedTile>();

        var tiles = new List<ApplicationWorkspaceCaseIssuedTile>();
        foreach (var def in ApplicationWorkspaceIssuedRecordsCatalog.Definitions)
        {
            if (!def.IsVisible(application))
                continue;

            var rows = LoadIssuedRows(application, objectSpace, def.Key);
            tiles.Add(new ApplicationWorkspaceCaseIssuedTile
            {
                Key = def.Key,
                Label = def.Label,
                Count = rows.Count,
                Tone = def.Tone,
                Glyph = def.Glyph,
                AddCaption = def.AddCaption,
                NewCaption = def.NewCaption,
                PanelTitle = def.PanelTitle,
                EmptyHint = def.EmptyHint,
                Rows = rows,
            });
        }

        return tiles;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseIssuedRow> LoadIssuedRows(
        ApplicationProfileInstance application,
        IObjectSpace? objectSpace,
        string key)
    {
        switch (key)
        {
            case ApplicationWorkspaceIssuedRecordsCatalog.Invitation:
                var invitations = objectSpace != null
                    ? objectSpace.GetObjectsQuery<Invitation>()
                        .Where(i => i.ApplicationProfileInstance != null
                            && i.ApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.Invitations?.ToList() ?? [];
                var invitationCopyIds = HeaderIdsWithCopies(
                    objectSpace,
                    () => objectSpace!.GetObjectsQuery<InvitationDocument>()
                        .Where(d => d.Invitation != null
                            && d.Invitation.ApplicationProfileInstance != null
                            && d.Invitation.ApplicationProfileInstance.ID == application.ID
                            && d.File != null)
                        .Select(d => d.Invitation.ID)
                        .ToList(),
                    invitations.SelectMany(i => i.Documents ?? Array.Empty<InvitationDocument>()));
                return invitations
                    .OrderByDescending(i => i.IssuedDate)
                    .Select(i => Row(i.ID, i.InvitationNumber, FormatIssuedDate(i.IssuedDate), invitationCopyIds.Contains(i.ID)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit:
                var permits = objectSpace != null
                    ? objectSpace.GetObjectsQuery<WorkPermit>()
                        .Where(w => w.ApplicationProfileInstance != null
                            && w.ApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.WorkPermits?.ToList() ?? [];
                var permitCopyIds = HeaderIdsWithCopies(
                    objectSpace,
                    () => objectSpace!.GetObjectsQuery<WorkPermitDocument>()
                        .Where(d => d.WorkPermit != null
                            && d.WorkPermit.ApplicationProfileInstance != null
                            && d.WorkPermit.ApplicationProfileInstance.ID == application.ID
                            && d.File != null)
                        .Select(d => d.WorkPermit.ID)
                        .ToList(),
                    permits.SelectMany(w => w.Documents ?? Array.Empty<WorkPermitDocument>()));
                return permits
                    .OrderByDescending(w => w.IssuedDate)
                    .Select(w => Row(w.ID, w.WorkPermitNumber, FormatIssuedDate(w.IssuedDate), permitCopyIds.Contains(w.ID)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.BorderZone:
                var zones = objectSpace != null
                    ? objectSpace.GetObjectsQuery<BorderZone>()
                        .Where(z => z.ApplicationProfileInstance != null
                            && z.ApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.BorderZones?.ToList() ?? [];
                var zoneCopyIds = HeaderIdsWithCopies(
                    objectSpace,
                    () => objectSpace!.GetObjectsQuery<BorderZoneDocument>()
                        .Where(d => d.BorderZone != null
                            && d.BorderZone.ApplicationProfileInstance != null
                            && d.BorderZone.ApplicationProfileInstance.ID == application.ID
                            && d.File != null)
                        .Select(d => d.BorderZone.ID)
                        .ToList(),
                    zones.SelectMany(z => z.Documents ?? Array.Empty<BorderZoneDocument>()));
                return zones
                    .OrderByDescending(z => z.StartDate)
                    .Select(z => Row(z.ID, z.BorderZoneNumber, FormatIssuedDate(z.StartDate), zoneCopyIds.Contains(z.ID)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.Rejection:
                var rejections = objectSpace != null
                    ? objectSpace.GetObjectsQuery<Rejection>()
                        .Where(r => r.ApplicationProfileInstance != null
                            && r.ApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.Rejections?.ToList() ?? [];
                var rejectionCopyIds = HeaderIdsWithCopies(
                    objectSpace,
                    () => objectSpace!.GetObjectsQuery<RejectionDocument>()
                        .Where(d => d.Rejection != null
                            && d.Rejection.ApplicationProfileInstance != null
                            && d.Rejection.ApplicationProfileInstance.ID == application.ID
                            && d.File != null)
                        .Select(d => d.Rejection.ID)
                        .ToList(),
                    rejections.SelectMany(r => r.Documents ?? Array.Empty<RejectionDocument>()));
                return rejections
                    .OrderByDescending(r => r.Date)
                    .Select(r => Row(r.ID, r.RejectedDocNumber, FormatIssuedDate(r.Date), rejectionCopyIds.Contains(r.ID)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa:
                var visas = objectSpace != null
                    ? objectSpace.GetObjectsQuery<Visa>()
                        .Where(v => v.IssuingApplicationProfileInstance != null
                            && v.IssuingApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.IssuedVisas?.ToList() ?? [];
                var visaCopyIds = HeaderIdsWithCopies(
                    objectSpace,
                    () => objectSpace!.GetObjectsQuery<VisaDocument>()
                        .Where(d => d.Visa != null
                            && d.Visa.IssuingApplicationProfileInstance != null
                            && d.Visa.IssuingApplicationProfileInstance.ID == application.ID
                            && d.File != null)
                        .Select(d => d.Visa.ID)
                        .ToList(),
                    visas.SelectMany(v => v.Documents ?? Array.Empty<VisaDocument>()));
                return visas
                    .OrderByDescending(v => v.IssueDate)
                    .Select(v => Row(v.ID, v.VisaNumber, FormatIssuedDate(v.IssueDate), visaCopyIds.Contains(v.ID)))
                    .ToList();
            default:
                return Array.Empty<ApplicationWorkspaceCaseIssuedRow>();
        }
    }

    private static ApplicationWorkspaceCaseIssuedRow Row(Guid id, string? title, string subtitle, bool hasCopy = false) =>
        new()
        {
            Id = id,
            Title = string.IsNullOrWhiteSpace(title) ? "—" : title.Trim(),
            Subtitle = subtitle,
            HasCopy = hasCopy,
        };

    private static HashSet<Guid> HeaderIdsWithCopies(
        IObjectSpace? objectSpace,
        Func<IEnumerable<Guid>> queryWhenObjectSpace,
        IEnumerable<DocumentBase> fallbackDocuments)
    {
        if (objectSpace != null)
            return queryWhenObjectSpace().ToHashSet();

        return fallbackDocuments
            .Where(d => d?.File != null && d.File.Size > 0)
            .Select(d => d switch
            {
                InvitationDocument inv => inv.Invitation?.ID ?? Guid.Empty,
                WorkPermitDocument wp => wp.WorkPermit?.ID ?? Guid.Empty,
                RejectionDocument rj => rj.Rejection?.ID ?? Guid.Empty,
                BorderZoneDocument bz => bz.BorderZone?.ID ?? Guid.Empty,
                VisaDocument visa => visa.Visa?.ID ?? Guid.Empty,
                _ => Guid.Empty,
            })
            .Where(id => id != Guid.Empty)
            .ToHashSet();
    }

    private static string FormatIssuedDate(DateTime date) =>
        date == default ? string.Empty : date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture);

    private static IReadOnlyList<ApplicationWorkspaceCasePerson> BuildPeople(
        IReadOnlyDictionary<string, ApplicationWorkspaceTab> tabs,
        IReadOnlyList<string> peopleNames,
        ApplicationProfileInstance? application,
        IReadOnlyList<Person> rosterPeople,
        IReadOnlyList<ApplicationProfileInstancePersonResolvedLink> rosterLinks)
    {
        if (!tabs.TryGetValue("person", out var personTab) || personTab.Rows.Count == 0)
            return Array.Empty<ApplicationWorkspaceCasePerson>();

        var people = new List<ApplicationWorkspaceCasePerson>();
        for (var i = 0; i < personTab.Rows.Count; i++)
        {
            var row = personTab.Rows[i];
            var name = row.Count > 0 ? row[0] : "—";
            var role = row.Count > 1 ? row[1] : "—";
            people.Add(new ApplicationWorkspaceCasePerson
            {
                Index = i,
                PersonId = i < personTab.RowPersonIds.Count ? personTab.RowPersonIds[i] : Guid.Empty,
                ApplicationProfileInstancePersonId = i < personTab.RowApplicationProfileInstancePersonIds.Count
                    ? personTab.RowApplicationProfileInstancePersonIds[i]
                    : Guid.Empty,
                Name = name,
                RoleLabel = FormatRoleLabel(role),
                PassportNumber = FirstCellForPerson(tabs, "passport", name, 1),
                VisaNumber = FirstCellForPerson(tabs, "visa", name, 1),
                Records = BuildPersonRecords(application, rosterPeople, rosterLinks, tabs, name, i),
            });
        }

        if (people.Count == 0 && peopleNames.Count > 0)
        {
            for (var i = 0; i < peopleNames.Count; i++)
            {
                var name = peopleNames[i];
                people.Add(new ApplicationWorkspaceCasePerson
                {
                    Index = i,
                    Name = name,
                    RoleLabel = i == 0 ? "Primary applicant" : "Dependent",
                    Records = BuildPersonRecords(application, rosterPeople, rosterLinks, tabs, name, i),
                });
            }
        }

        return people;
    }

    private static string FormatRoleLabel(string role)
    {
        if (string.IsNullOrWhiteSpace(role) || role == "—")
            return "—";

        return role.Contains("Employee", StringComparison.OrdinalIgnoreCase)
            || role.Contains("Primary", StringComparison.OrdinalIgnoreCase)
            ? "Primary applicant"
            : role.Contains("Family", StringComparison.OrdinalIgnoreCase)
                || role.Contains("Dependent", StringComparison.OrdinalIgnoreCase)
                ? "Dependent"
                : role;
    }

    private static string FirstCellForPerson(
        IReadOnlyDictionary<string, ApplicationWorkspaceTab> tabs,
        string tabKey,
        string personName,
        int columnIndex)
    {
        if (!tabs.TryGetValue(tabKey, out var tab))
            return "—";

        var match = tab.Rows.FirstOrDefault(r => r.Count > 0 && string.Equals(r[0], personName, StringComparison.Ordinal));
        return match != null && match.Count > columnIndex ? match[columnIndex] : "—";
    }

    private static IReadOnlyList<ApplicationWorkspaceCasePersonRecord> BuildPersonRecords(
        ApplicationProfileInstance? application,
        IReadOnlyList<Person> rosterPeople,
        IReadOnlyList<ApplicationProfileInstancePersonResolvedLink> rosterLinks,
        IReadOnlyDictionary<string, ApplicationWorkspaceTab> tabs,
        string personName,
        int personIndex)
    {
        var records = new List<ApplicationWorkspaceCasePersonRecord>();
        Person? rosterPerson = personIndex >= 0 && personIndex < rosterPeople.Count
            ? rosterPeople[personIndex]
            : rosterPeople.FirstOrDefault(p =>
                string.Equals(p.FullName, personName, StringComparison.Ordinal));
        var toneIndex = 0;

        foreach (var def in ApplicationWorkspaceLinkedRecordsCatalog.Definitions)
        {
            if (application != null && !ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, def.Kind))
                continue;

            if (application == null
                && (!tabs.TryGetValue(def.TabKey, out var hiddenTab) || !hiddenTab.Visible))
            {
                continue;
            }

            var expected = Math.Max(
                ApplicationProfilePersonLastCount.For(application, def.Kind),
                1);
            var count = rosterPerson != null
                ? ApplicationWorkspaceLinkedRecordsCatalog.CountResolvedForPerson(rosterLinks, rosterPerson.ID, def.Kind)
                : tabs.TryGetValue(def.TabKey, out var tab)
                    ? tab.Rows.Count(r => r.Count > 0 && string.Equals(r[0], personName, StringComparison.Ordinal))
                    : 0;

            records.Add(new ApplicationWorkspaceCasePersonRecord
            {
                Key = def.PersonRecordKey,
                Label = def.Label,
                Count = count,
                ExpectedCount = expected,
                State = count >= expected ? "valid" : "empty",
                Glyph = def.Glyph,
                Tone = LinkedTones[toneIndex % LinkedTones.Length],
            });
            toneIndex++;
        }

        return records;
    }

    private static ApplicationWorkspaceCasePeopleSummary BuildPeopleSummary(
        IReadOnlyList<ApplicationWorkspaceCasePerson> people)
    {
        var primary = people.Count(p =>
            p.RoleLabel.Contains("Primary", StringComparison.OrdinalIgnoreCase)
            || p.RoleLabel.Contains("Employee", StringComparison.OrdinalIgnoreCase));
        var dependents = people.Count - primary;
        return new ApplicationWorkspaceCasePeopleSummary
        {
            Total = people.Count,
            Primary = Math.Max(primary, people.Count > 0 ? 1 : 0),
            Dependents = Math.Max(0, dependents),
            Sponsors = 0,
        };
    }

    private static Dictionary<string, int> BuildLinkedSummary(
        ApplicationProfileInstance? application,
        IReadOnlyList<ApplicationProfileInstancePersonResolvedLink> rosterLinks,
        IReadOnlyList<ApplicationWorkspaceCasePerson> people)
    {
        if (application != null && rosterLinks.Count > 0)
        {
            var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var def in ApplicationWorkspaceLinkedRecordsCatalog.Definitions)
            {
                if (!ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, def.Kind))
                    continue;

                var count = ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(rosterLinks, def.Kind);
                if (count > 0)
                    totals[def.Label] = count;
            }

            return totals;
        }

        var fallback = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var person in people)
        {
            foreach (var record in person.Records.Where(r => r.State == "valid"))
            {
                fallback[record.Label] = fallback.GetValueOrDefault(record.Label) + Math.Max(record.Count, 1);
            }
        }

        return fallback;
    }

    private static int ResolveProgressIndex(string? stepLabel)
    {
        var step = stepLabel?.ToLowerInvariant() ?? string.Empty;
        if (step.Contains("office")) return 0;
        if (step.Contains("ministry") || step.Contains("review") || step.Contains("awaiting")) return 1;
        if (step.Contains("migration")) return 2;
        if (step.Contains("complete") || step.Contains("issued")) return 3;
        return 1;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseActivity> BuildActivities(
        ApplicationProfileInstance application,
        ApplicationWorkspaceCaseChrome chrome)
    {
        var items = new List<ApplicationWorkspaceCaseActivity>();
        if (chrome.MergedFromCount is int merged && merged > 1)
        {
            items.Add(new ApplicationWorkspaceCaseActivity
            {
                Title = $"Merged {merged} profiles",
                Subtitle = chrome.StartedOn,
            });
        }

        if (!string.IsNullOrWhiteSpace(chrome.ProcessNumber))
        {
            items.Add(new ApplicationWorkspaceCaseActivity
            {
                Title = "Number assigned",
                Subtitle = chrome.ProcessNumber,
            });
        }

        var history = application.ProgressHistory?
            .OrderByDescending(p => p.Order)
            .ThenByDescending(p => p.Date)
            .ThenByDescending(p => p.ID)
            .ToList() ?? [];

        foreach (var row in history)
        {
            var label = ApplicationWorkspaceProgressTimeline.FormatProfileStateLabel(row.State?.Code);
            if (string.IsNullOrWhiteSpace(label))
                label = row.State?.Code ?? "—";

            items.Add(new ApplicationWorkspaceCaseActivity
            {
                Title = label,
                Subtitle = FormatActivityDate(row.Date),
            });
        }

        if (items.Count == 0)
        {
            items.Add(new ApplicationWorkspaceCaseActivity
            {
                Title = chrome.CurrentStep,
                Subtitle = "Latest progress",
            });
        }

        return items;
    }

    private static string FormatActivityDate(DateTime date)
    {
        if (date == default)
            return string.Empty;

        return date.TimeOfDay == TimeSpan.Zero
            ? date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
            : date.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);
    }
}
