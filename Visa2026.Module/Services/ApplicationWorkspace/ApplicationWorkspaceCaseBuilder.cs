using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

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
            ? BuildProgressSteps(application, chrome, sla, objectSpace)
            : BuildProgressStepsFromChrome(chrome);
        var slaDashboard = application != null
            ? BuildSla(application, profile, sla, chrome, progressSteps)
            : BuildSlaFromChrome(chrome, progressSteps);

        return new ApplicationWorkspaceCaseView
        {
            Chrome = chrome,
            SummaryTiles = application != null
                ? BuildSummaryTiles(application, chrome)
                : BuildSummaryTilesFromChrome(chrome),
            LinkedRecordTiles = BuildLinkedTiles(application, rosterLinks, tabs),
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
            CaseDaysRemaining = remaining,
            TotalSlaDays = 45,
            ElapsedDays = 33,
            CurrentStepDaysRemaining = current?.SlaDaysRemaining ?? 8,
            CurrentStepDueDate = current?.SlaTargetDate ?? "22 Aug 2026",
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

    private static IReadOnlyList<ApplicationWorkspaceCaseSummaryTile> BuildSummaryTiles(
        ApplicationProfileInstance application,
        ApplicationWorkspaceCaseChrome chrome) =>
    [
        Tile("Visa type", FormatLookup(application.VisaType?.Code, application.VisaType?.NameTm), "blue", "🛂"),
        Tile("Category", FormatLookup(application.VisaCategory?.Code, application.VisaCategory?.NameTm), "purple", "◆"),
        Tile("Period", FormatLookup(application.VisaPeriod?.Code, application.VisaPeriod?.NameTm), "green", "📅"),
        Tile("Project / Contract", chrome.ProjectName, "orange", "💼"),
        Tile("Entry checkpoint", ResolveEntryCheckpoint(application), "blue", "📍"),
    ];

    private static ApplicationWorkspaceCaseSummaryTile Tile(string label, string value, string tone, string glyph) =>
        new()
        {
            Label = label,
            Value = string.IsNullOrWhiteSpace(value) ? "—" : value,
            Tone = tone,
            Glyph = glyph,
        };

    private static string FormatLookup(string? code, string? name) =>
        !string.IsNullOrWhiteSpace(code) ? code.Trim()
        : !string.IsNullOrWhiteSpace(name) ? name.Trim()
        : "—";

    private static string ResolveEntryCheckpoint(ApplicationProfileInstance application)
    {
        var border = application.BorderZoneLocation_NameTm;
        if (string.IsNullOrWhiteSpace(border) || border == "Ýok")
            return "—";

        var first = border.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? border : first;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseLinkedTile> BuildLinkedTiles(
        ApplicationProfileInstance? application,
        IReadOnlyList<ApplicationProfileInstancePersonResolvedLink> rosterLinks,
        IReadOnlyList<ApplicationWorkspaceTab> tabs)
    {
        if (application != null && rosterLinks.Count > 0)
        {
            var tiles = new List<ApplicationWorkspaceCaseLinkedTile>();
            var toneIndex = 0;
            foreach (var def in ApplicationWorkspaceLinkedRecordsCatalog.Definitions)
            {
                if (!ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, def.Kind))
                    continue;

                var count = ApplicationWorkspaceLinkedRecordsCatalog.CountResolved(rosterLinks, def.Kind);
                if (count == 0)
                    continue;

                tiles.Add(new ApplicationWorkspaceCaseLinkedTile
                {
                    TabKey = def.TabKey,
                    Label = def.Label,
                    Count = count,
                    Tone = LinkedTones[toneIndex % LinkedTones.Length],
                    Glyph = def.Glyph,
                });
                toneIndex++;
            }

            return tiles;
        }

        var fallback = new List<ApplicationWorkspaceCaseLinkedTile>();
        var fallbackTone = 0;
        foreach (var tab in tabs.Where(t => t.Visible && t.Key != "person" && t.Rows.Count > 0))
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
                return invitations
                    .OrderByDescending(i => i.IssuedDate)
                    .Select(i => Row(i.ID, i.InvitationNumber, FormatIssuedDate(i.IssuedDate)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.WorkPermit:
                var permits = objectSpace != null
                    ? objectSpace.GetObjectsQuery<WorkPermit>()
                        .Where(w => w.ApplicationProfileInstance != null
                            && w.ApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.WorkPermits?.ToList() ?? [];
                return permits
                    .OrderByDescending(w => w.IssuedDate)
                    .Select(w => Row(w.ID, w.WorkPermitNumber, FormatIssuedDate(w.IssuedDate)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.BorderZone:
                var zones = objectSpace != null
                    ? objectSpace.GetObjectsQuery<BorderZone>()
                        .Where(z => z.ApplicationProfileInstance != null
                            && z.ApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.BorderZones?.ToList() ?? [];
                return zones
                    .OrderByDescending(z => z.StartDate)
                    .Select(z => Row(z.ID, z.BorderZoneNumber, FormatIssuedDate(z.StartDate)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.Rejection:
                var rejections = objectSpace != null
                    ? objectSpace.GetObjectsQuery<Rejection>()
                        .Where(r => r.ApplicationProfileInstance != null
                            && r.ApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.Rejections?.ToList() ?? [];
                return rejections
                    .OrderByDescending(r => r.Date)
                    .Select(r => Row(r.ID, r.RejectedDocNumber, FormatIssuedDate(r.Date)))
                    .ToList();
            case ApplicationWorkspaceIssuedRecordsCatalog.IssuedVisa:
                var visas = objectSpace != null
                    ? objectSpace.GetObjectsQuery<Visa>()
                        .Where(v => v.IssuingApplicationProfileInstance != null
                            && v.IssuingApplicationProfileInstance.ID == application.ID)
                        .ToList()
                    : application.IssuedVisas?.ToList() ?? [];
                return visas
                    .OrderByDescending(v => v.IssueDate)
                    .Select(v => Row(v.ID, v.VisaNumber, FormatIssuedDate(v.IssueDate)))
                    .ToList();
            default:
                return Array.Empty<ApplicationWorkspaceCaseIssuedRow>();
        }
    }

    private static ApplicationWorkspaceCaseIssuedRow Row(Guid id, string? title, string subtitle) =>
        new()
        {
            Id = id,
            Title = string.IsNullOrWhiteSpace(title) ? "—" : title.Trim(),
            Subtitle = subtitle,
        };

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

        foreach (var def in ApplicationWorkspaceLinkedRecordsCatalog.Definitions)
        {
            if (application != null && !ApplicationWorkspaceLinkedRecordsCatalog.IsConfigured(application, def.Kind))
                continue;

            if (application == null
                && (!tabs.TryGetValue(def.TabKey, out var hiddenTab) || !hiddenTab.Visible))
            {
                continue;
            }

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
                State = count > 0 ? "valid" : "empty",
                Glyph = def.Glyph,
            });
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

    private static IReadOnlyList<ApplicationWorkspaceCaseProgressStep> BuildProgressSteps(
        ApplicationProfileInstance application,
        ApplicationWorkspaceCaseChrome chrome,
        ApplicationProfileInstanceProgressSlaResult sla,
        IObjectSpace? objectSpace)
    {
        var currentIndex = ResolveProgressIndex(chrome.CurrentStep);
        var history = application.ProgressHistory?
            .OrderBy(p => p.Order)
            .ToList() ?? [];
        var latest = ApplicationProfileInstanceProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        var advanceOptions = BuildAdvanceOptions(application, latest, objectSpace);
        var canAdvance = advanceOptions.Count > 0;
        var advanceBlockedReason = canAdvance
            ? string.Empty
            : (ApplicationProfileInstanceProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code)
                ? "This application has reached a terminal progress state."
                : "No further progress steps are available for this route.");

        var steps = new List<ApplicationWorkspaceCaseProgressStep>();
        for (var i = 0; i < ProgressStepLabels.Length; i++)
        {
            var state = i < currentIndex ? "done" : i == currentIndex ? "current" : "pending";
            var date = history.ElementAtOrDefault(i)?.Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture) ?? string.Empty;
            int? daysLeft = null;
            string slaTarget = string.Empty;
            if (state == "current" && sla.MaxDaysInReview is int maxDays && sla.WorkingDaysInCurrentStep is int elapsed)
            {
                daysLeft = Math.Max(0, maxDays - elapsed);
                slaTarget = application.LatestProgress?.Date != default
                    ? application.LatestProgress.Date.AddDays(maxDays).ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                    : string.Empty;
            }

            steps.Add(new ApplicationWorkspaceCaseProgressStep
            {
                Key = ProgressStepKeys[i],
                Label = ProgressStepLabels[i],
                Date = date,
                State = state,
                CurrentStateLabel = state == "current" ? chrome.CurrentStep : string.Empty,
                SlaTargetDate = slaTarget,
                SlaDaysRemaining = daysLeft,
                ProgressId = state == "current" ? latest?.ID : null,
                OfficerNotes = state == "current" ? latest?.Description ?? string.Empty : string.Empty,
                MinistryLetterFileName = state == "current" ? latest?.MinistryLetterFileName ?? string.Empty : string.Empty,
                ShowMinistryLetterUpload = state == "current" && latest?.IsMinistryDecisionStep == true,
                CanAdvance = state == "current" && canAdvance,
                AdvanceBlockedReason = state == "current" ? advanceBlockedReason : string.Empty,
                AdvanceOptions = state == "current" ? advanceOptions : Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>(),
            });
        }

        return steps;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseProgressAdvanceOption> BuildAdvanceOptions(
        ApplicationProfileInstance application,
        ApplicationProfileInstanceProgress? latest,
        IObjectSpace? objectSpace)
    {
        var codes = ApplicationProfileInstanceProgressTransitionHelper.GetAllowedNextStateCodes(application, latest);
        if (codes.Count == 0)
            return Array.Empty<ApplicationWorkspaceCaseProgressAdvanceOption>();

        return codes
            .Select(code => new ApplicationWorkspaceCaseProgressAdvanceOption
            {
                StateCode = code,
                Label = ResolveStateLabel(objectSpace, code),
            })
            .ToList();
    }

    private static string ResolveStateLabel(IObjectSpace? objectSpace, string stateCode)
    {
        if (objectSpace != null)
        {
            var state = objectSpace.GetObjectsQuery<ApplicationState>()
                .FirstOrDefault(s => s.Code == stateCode);
            if (state != null)
            {
                return state.LocalizedDisplayName
                    ?? state.NameTm
                    ?? state.Code
                    ?? stateCode;
            }
        }

        return stateCode;
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
            .Take(3)
            .ToList() ?? [];

        foreach (var row in history)
        {
            items.Add(new ApplicationWorkspaceCaseActivity
            {
                Title = $"Progress: {row.State?.LocalizedDisplayName ?? row.State?.NameTm ?? row.State?.Code ?? "—"}",
                Subtitle = row.Date == default
                    ? string.Empty
                    : row.Date.ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture),
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

    private static ApplicationWorkspaceCaseSlaDashboard BuildSla(
        ApplicationProfileInstance application,
        ApplicationProfile? profile,
        ApplicationProfileInstanceProgressSlaResult sla,
        ApplicationWorkspaceCaseChrome chrome,
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep> progressSteps)
    {
        var totalSla = ApplicationProfileConfigurationResolver.GetMigrationSlaMaxDays(application);
        if (totalSla <= 0)
            totalSla = sla.MaxDaysInReview ?? 0;

        var elapsed = sla.WorkingDaysInCurrentStep ?? 0;
        if (application.ApplicationDate != default && totalSla > 0)
        {
            elapsed = Math.Max(elapsed, (int)(DateTime.Today - application.ApplicationDate.Date).TotalDays);
        }

        var remaining = chrome.SlaDaysRemaining;
        if (totalSla > 0 && remaining == null)
            remaining = Math.Max(0, totalSla - elapsed);

        var currentStep = progressSteps.FirstOrDefault(s => s.State == "current");
        var deadlines = BuildDeadlines(application, progressSteps);

        var alert = currentStep?.SlaDaysRemaining is int stepDays && stepDays <= 10
            ? $"Ministry review deadline approaching. Due in {stepDays} days on {currentStep.SlaTargetDate}. Ensure all reviews and required actions are completed on time."
            : string.Empty;

        return new ApplicationWorkspaceCaseSlaDashboard
        {
            CaseDaysRemaining = remaining,
            TotalSlaDays = totalSla,
            ElapsedDays = elapsed,
            CurrentStepDaysRemaining = currentStep?.SlaDaysRemaining,
            CurrentStepDueDate = currentStep?.SlaTargetDate ?? string.Empty,
            StartedOn = chrome.StartedOn,
            MinistryDueDate = currentStep?.SlaTargetDate ?? string.Empty,
            ExpectedCompletionDate = deadlines.LastOrDefault()?.DueDate ?? string.Empty,
            MigrationSlaLabel = totalSla > 0 ? $"{totalSla} days" : "—",
            ProfileSlaSource = profile?.Name ?? "Profile template",
            AlertMessage = alert,
            Deadlines = deadlines,
        };
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseSlaDeadline> BuildDeadlines(
        ApplicationProfileInstance application,
        IReadOnlyList<ApplicationWorkspaceCaseProgressStep> progressSteps)
    {
        var history = application.ProgressHistory?
            .OrderBy(p => p.Order)
            .ToList() ?? [];

        var deadlines = new List<ApplicationWorkspaceCaseSlaDeadline>();
        for (var i = 0; i < progressSteps.Count; i++)
        {
            var step = progressSteps[i];
            var historyRow = history.ElementAtOrDefault(i);
            var due = historyRow?.Date != default
                ? historyRow.Date.ToString("dd MMM yyyy", CultureInfo.InvariantCulture)
                : step.SlaTargetDate;

            var status = step.State switch
            {
                "done" => "completed",
                "current" => "inprogress",
                _ => "pending",
            };

            deadlines.Add(new ApplicationWorkspaceCaseSlaDeadline
            {
                Step = step.Label,
                DueDate = due,
                DaysLeft = status == "completed"
                    ? "—"
                    : step.SlaDaysRemaining?.ToString(CultureInfo.InvariantCulture) ?? "—",
                Status = status,
                IsCurrent = step.State == "current",
            });
        }

        return deadlines;
    }
}
