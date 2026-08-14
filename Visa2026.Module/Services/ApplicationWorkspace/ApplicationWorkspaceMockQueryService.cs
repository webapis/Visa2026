using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;

namespace Visa2026.Module.Services.ApplicationWorkspace;

/// <summary>
/// Prototype mock — layout per docs/prototypes/process-started-application-profile-workspace-mockup.png.
/// Replace with <see cref="ApplicationWorkspaceQueryService"/> when domain M2M ships.
/// </summary>
public sealed class ApplicationWorkspaceMockQueryService : IApplicationWorkspaceQueryService
{

    private static readonly IReadOnlyList<string> LinkContext =
    [
        "Person",
        "Passport",
        "Visa",
        "AddressOfResidence",
        "WorkPermitItem",
        "InvitationItem",
        "BorderZoneItem",
        "Education",
        "EmployeeSalary",
        "EmployeePositionHistory",
        "MedicalRecord",
        "TravelHistory",
        "RejectionItem",
    ];

    public ApplicationWorkspaceSnapshot Load(IObjectSpace objectSpace, Guid applicationId)
    {
        var tabs = BuildTabs();
        var caseChrome = new ApplicationWorkspaceCaseChrome
        {
            DisplayNumber = "2026-0147",
            ProcessNumber = "2026-0147",
            TemplateFamilyKey = "inv",
            TemplateFamilyLabel = "Visa extension",
            StartedOn = "10 Aug 2026",
            CurrentStep = "Ministry review",
            ProjectName = "Plant Expansion 2026",
            SlaDaysRemaining = 12,
            PeopleNames = ["Maksat Orazow", "Döwran Ataýew", "Aýgul Berdiýewa"],
            MergedFromCount = 3,
            ProfileTemplateName = "Visa extension",
        };

        var snapshot = new ApplicationWorkspaceSnapshot
        {
            ApplicationProfileInstanceId = applicationId == Guid.Empty ? Guid.Parse("11111111-1111-1111-1111-111111111111") : applicationId,
            Header = new ApplicationWorkspaceHeader
            {
                ApplicationNumber = "2026-0147",
                ApplicationDate = "10.08.2026",
                Urgency = "Normal",
                ProgressStep = 2,
                ProgressTotalSteps = 4,
                SlaDaysElapsed = 5,
                SlaDaysTotal = 10,
            },
            ProgressHistory =
            [
                new() { State = "Office preparation", Date = "10.08.2026", Description = "Draft package" },
                new() { State = "Ministry review", Date = "11.08.2026", Description = "Under review" },
            ],
            Profile = new ApplicationWorkspaceProfileSummary
            {
                ProfileId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                Title = "Visa extension",
                Code = "VISA_EXT",
                Chips =
                [
                    "Related to: Issuance",
                    "Via ministry",
                    "For: Employee",
                ],
            },
            ProfileRail = Array.Empty<ApplicationWorkspaceProfileRailItem>(),
            LinkContextItems = LinkContext,
            Tabs = tabs,
            CaseChrome = caseChrome,
            IsPrototypeMock = true,
        };

        return new ApplicationWorkspaceSnapshot
        {
            ApplicationProfileInstanceId = snapshot.ApplicationProfileInstanceId,
            Header = snapshot.Header,
            ProgressHistory = snapshot.ProgressHistory,
            Profile = snapshot.Profile,
            ProfileRail = snapshot.ProfileRail,
            LinkContextItems = snapshot.LinkContextItems,
            Tabs = snapshot.Tabs,
            CaseChrome = snapshot.CaseChrome,
            CaseView = ApplicationWorkspaceCaseBuilder.BuildFromSnapshot(snapshot),
            IsPrototypeMock = snapshot.IsPrototypeMock,
        };
    }

    private static IReadOnlyList<ApplicationWorkspaceTab> BuildTabs() =>
    [
        Tab("person", "Person", ["Person", "Role", "Personal №", "From view"],
            [
                ["Maksat Orazow", "Employee", "I-123456", "vw_app_persons"],
                ["Döwran Ataýew", "FamilyMember", "I-654321", "vw_app_persons"],
                ["Aýgul Berdiýewa", "FamilyMember", "I-789012", "vw_app_persons"],
            ],
            sqlViewHint: "vw_app_persons"),
        Tab("passport", "Passport", ["Person", "Passport №", "Issued", "Expires"],
            [
                ["Maksat Orazow", "TM1234567", "12.01.2022", "12.01.2032"],
                ["Döwran Ataýew", "TM2345678", "03.05.2021", "03.05.2031"],
                ["Aýgul Berdiýewa", "TM3456789", "08.11.2020", "08.11.2030"],
            ]),
        Tab("visa", "Visa", ["Person", "Visa №", "Type", "Valid to"],
            [
                ["Maksat Orazow", "VISA-2026-0147-01", "WP", "01.02.2027"],
                ["Döwran Ataýew", "VISA-2026-0147-02", "WP", "01.02.2027"],
                ["Aýgul Berdiýewa", "VISA-2026-0147-03", "WP", "01.02.2027"],
            ]),
        Tab("education", "Education", ["Person", "Institution", "Level", "Year"],
            [["Berdiýew A.A.", "TDU", "Bachelor", "2014"]]),
        Tab("address", "Address", ["Person", "City", "Address"],
            [["Berdiýew A.A.", "Aşgabat", "…"]]),
        Tab("wp", "Work permit", ["Person", "WP item", "Location", "Valid to"],
            [["Berdiýew A.A.", "WP-7788", "Aşgabat", "01.02.2027"]]),
        Tab("inv", "Invitation", ["Person", "Invitation item", "Status"],
            [["Berdiýew A.A.", "INV-2201", "Current"]]),
        Tab("bz", "Border zone", ["Person", "Border zone item", "Zones"],
            emptyMessage: "No linked border-zone items"),
        Tab("position", "Position", ["Person", "Position", "From"],
            [["Berdiýew A.A.", "Engineer", "01.03.2023"]]),
        Tab("salary", "Salary", ["Person", "Amount", "Currency"],
            [["Berdiýew A.A.", "…", "TMT"]]),
        Tab("medical", "Medical", ["Person", "Record", "Valid to"],
            [["Berdiýew A.A.", "MED-91", "01.01.2027"]]),
        Tab("travel", "Travel history", ["Person", "Kind", "Date", "Check point"],
            [["Berdiýew A.A.", "External arrival", "12.07.2026", "Howdan"]]),
        Tab("rejection", "Rejection", ["Person", "Rejection item", "Status"],
            emptyMessage: "No current rejection items"),
    ];

    private static ApplicationWorkspaceTab Tab(
        string key,
        string label,
        IReadOnlyList<string> columns,
        IReadOnlyList<IReadOnlyList<string>>? rows = null,
        string? emptyMessage = null,
        string? sqlViewHint = null) =>
        new()
        {
            Key = key,
            Label = label,
            Columns = columns,
            Rows = rows ?? Array.Empty<IReadOnlyList<string>>(),
            EmptyMessage = emptyMessage,
            SqlViewHint = sqlViewHint,
        };
}
