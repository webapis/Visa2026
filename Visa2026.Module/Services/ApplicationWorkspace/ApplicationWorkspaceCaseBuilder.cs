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

    private static readonly (string Key, string Label, string Glyph)[] PersonRecordTypes =
    [
        ("passport", "Passport", "🛂"),
        ("education", "Education", "🎓"),
        ("position", "Position", "💼"),
        ("address", "Address", "📍"),
        ("travel", "Travel history", "✈"),
        ("medical", "Medical", "🩺"),
        ("wp", "Work permit", "📄"),
        ("inv", "Invitation", "✉"),
        ("salary", "Salary", "💰"),
        ("bz", "Border zone", "🚧"),
    ];

    public static ApplicationWorkspaceCaseView Build(
        Application application,
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationWorkspaceTab> tabs,
        ApplicationProgressSlaResult sla,
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
        Application? application,
        ApplicationProfile? profile,
        IReadOnlyList<ApplicationWorkspaceTab> tabs,
        ApplicationProgressSlaResult sla,
        ApplicationWorkspaceCaseChrome chrome,
        IObjectSpace? objectSpace)
    {
        var tabMap = tabs.ToDictionary(t => t.Key, StringComparer.OrdinalIgnoreCase);
        var people = BuildPeople(tabMap, chrome.PeopleNames);
        var linkedSummary = BuildLinkedSummary(people);
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
            LinkedRecordTiles = BuildLinkedTiles(tabs),
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
                new() { Step = "Application received", DueDate = chrome.StartedOn, DaysLeft = "—", Status = "completed" },
                new() { Step = "Document check", DueDate = "14 Aug 2026", DaysLeft = "2", Status = "completed" },
                new() { Step = "Ministry review", DueDate = "22 Aug 2026", DaysLeft = "8", Status = "inprogress", IsCurrent = true },
                new() { Step = "Decision", DueDate = "05 Sep 2026", DaysLeft = "22", Status = "pending" },
                new() { Step = "Finalization", DueDate = "15 Sep 2026", DaysLeft = "35", Status = "pending" },
            ],
        };
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseSummaryTile> BuildSummaryTiles(
        Application application,
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

    private static string ResolveEntryCheckpoint(Application application)
    {
        var border = application.BorderZoneLocation_NameTm;
        if (string.IsNullOrWhiteSpace(border) || border == "Ýok")
            return "—";

        var first = border.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? border : first;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseLinkedTile> BuildLinkedTiles(
        IReadOnlyList<ApplicationWorkspaceTab> tabs)
    {
        var tiles = new List<ApplicationWorkspaceCaseLinkedTile>();
        var toneIndex = 0;
        foreach (var tab in tabs.Where(t => t.Visible && t.Key != "person" && t.Rows.Count > 0))
        {
            tiles.Add(new ApplicationWorkspaceCaseLinkedTile
            {
                TabKey = tab.Key,
                Label = tab.Label,
                Count = tab.Rows.Count,
                Tone = LinkedTones[toneIndex % LinkedTones.Length],
                Glyph = GlyphForTab(tab.Key),
            });
            toneIndex++;
        }

        return tiles;
    }

    private static string GlyphForTab(string key) => key switch
    {
        "passport" => "🛂",
        "visa" => "💳",
        "education" => "🎓",
        "position" => "💼",
        "travel" => "✈",
        _ => "📎",
    };

    private static IReadOnlyList<ApplicationWorkspaceCasePerson> BuildPeople(
        IReadOnlyDictionary<string, ApplicationWorkspaceTab> tabs,
        IReadOnlyList<string> peopleNames)
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
                ApplicationPersonId = i < personTab.RowApplicationPersonIds.Count
                    ? personTab.RowApplicationPersonIds[i]
                    : Guid.Empty,
                Name = name,
                RoleLabel = FormatRoleLabel(role),
                PassportNumber = FirstCellForPerson(tabs, "passport", name, 1),
                VisaNumber = FirstCellForPerson(tabs, "visa", name, 1),
                Records = BuildPersonRecords(tabs, name),
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
                    Records = BuildPersonRecords(tabs, name),
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
        IReadOnlyDictionary<string, ApplicationWorkspaceTab> tabs,
        string personName)
    {
        var records = new List<ApplicationWorkspaceCasePersonRecord>();
        foreach (var (key, label, glyph) in PersonRecordTypes)
        {
            if (!tabs.TryGetValue(key, out var tab) || !tab.Visible)
                continue;

            var count = tab.Rows.Count(r => r.Count > 0 && string.Equals(r[0], personName, StringComparison.Ordinal));
            records.Add(new ApplicationWorkspaceCasePersonRecord
            {
                Key = key,
                Label = label,
                Count = count,
                State = count > 0 ? "valid" : "empty",
                Glyph = glyph,
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

    private static Dictionary<string, int> BuildLinkedSummary(IReadOnlyList<ApplicationWorkspaceCasePerson> people)
    {
        var totals = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var person in people)
        {
            foreach (var record in person.Records.Where(r => r.State == "valid"))
            {
                totals[record.Label] = totals.GetValueOrDefault(record.Label) + Math.Max(record.Count, 1);
            }
        }

        return totals;
    }

    private static IReadOnlyList<ApplicationWorkspaceCaseProgressStep> BuildProgressSteps(
        Application application,
        ApplicationWorkspaceCaseChrome chrome,
        ApplicationProgressSlaResult sla,
        IObjectSpace? objectSpace)
    {
        var currentIndex = ResolveProgressIndex(chrome.CurrentStep);
        var history = application.ProgressHistory?
            .OrderBy(p => p.Order)
            .ToList() ?? [];
        var latest = ApplicationProgressHelper.GetLatest(application.ProgressHistory, objectSpace);
        var advanceOptions = BuildAdvanceOptions(application, latest, objectSpace);
        var canAdvance = advanceOptions.Count > 0;
        var advanceBlockedReason = canAdvance
            ? string.Empty
            : (ApplicationProgressTransitionHelper.IsTerminalStateCode(latest?.State?.Code)
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
        Application application,
        ApplicationProgress? latest,
        IObjectSpace? objectSpace)
    {
        var codes = ApplicationProgressTransitionHelper.GetAllowedNextStateCodes(application, latest);
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
        Application application,
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
        Application application,
        ApplicationProfile? profile,
        ApplicationProgressSlaResult sla,
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
        Application application,
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
