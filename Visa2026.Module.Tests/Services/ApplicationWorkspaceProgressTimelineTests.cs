using System;
using System.Collections.ObjectModel;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationWorkspaceProgressTimelineTests
{
    [Fact]
    public void Build_EmptyHistory_OfficeCurrentAndLegsPending()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal(5, steps.Count);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.OfficeKey, steps[0].Key);
        Assert.Equal("current", steps[0].State);
        Assert.Equal("Turkmenenergo", steps[1].Label);
        Assert.Equal("pending", steps[1].State);
        Assert.Equal("Turkmenenergetika", steps[2].Label);
        Assert.Equal("pending", steps[2].State);
        Assert.Equal("Turkmengurlusyk", steps[3].Label);
        Assert.Equal("pending", steps[3].State);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.MigrationKey, steps[4].Key);
        Assert.Equal("pending", steps[4].State);
        Assert.True(string.IsNullOrEmpty(steps[1].Date));
        Assert.True(string.IsNullOrEmpty(steps[1].CurrentStateLabel));
    }

    [Fact]
    public void Build_FirstLegStarted_FillsCurrentStateAndDate()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today.AddDays(-5),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        var startedOn = new DateTime(2026, 8, 14);
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = startedOn,
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
                NameTm = "Submitted",
            },
        });

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("current", steps[1].State);
        Assert.Equal("Turkmenenergo", steps[1].Label);
        Assert.False(string.IsNullOrWhiteSpace(steps[1].CurrentStateLabel));
        Assert.Equal("14 Aug 2026", steps[1].Date);
        Assert.Equal("Submitted", steps[1].CurrentStateLabel);
        Assert.Equal("pending", steps[2].State);
        Assert.Equal("pending", steps[3].State);
        Assert.Equal("pending", steps[4].State);
        Assert.True(steps[1].CanAdvance);
        Assert.Contains(steps[1].AdvanceOptions, o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.Review1Approved);
    }

    [Fact]
    public void Build_FirstLegStarted_UsesDisplayOrderWhenSequenceIsNotOne()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
        };
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 10,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmenenergo" },
        });
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 20,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Energetika" },
        });
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = DateTime.Today,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.Review1Started },
        });

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("current", steps[1].State);
        Assert.Equal("Turkmenenergo", steps[1].Label);
        Assert.Equal("Submitted", steps[1].CurrentStateLabel);
        Assert.Equal("pending", steps[2].State);
    }

    [Fact]
    public void Build_PrefersProfileLegsOverSnapshots()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
            ApprovalLegSnapshots =
            [
                new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 2, MinistryShortName = "Snapshot-A" },
                new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 3, MinistryShortName = "Snapshot-B" },
            ],
        };

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("Turkmenenergo", steps[1].Label);
        Assert.DoesNotContain(steps, s => s.Label.StartsWith("Snapshot", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_DirectMigration_OfficeAndMigrationOnly()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            MigrationSlaDays = 14,
        };
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal(2, steps.Count);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.OfficeKey, steps[0].Key);
        Assert.Equal("current", steps[0].State);
        Assert.Equal(ApplicationWorkspaceProgressTimeline.MigrationKey, steps[1].Key);
        Assert.Equal("pending", steps[1].State);
    }

    [Fact]
    public void ResolveProfileSlaDays_ViaMinistry_UsesMinistryDays()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            MinistrySlaDays = 21,
            MigrationSlaDays = 45,
        };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.Equal(21, ApplicationWorkspaceProgressTimeline.ResolveProfileSlaDays(application, profile));
    }

    [Fact]
    public void ResolveProfileSlaDays_Direct_UsesMigrationDays()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            MinistrySlaDays = 21,
            MigrationSlaDays = 45,
        };
        var application = new ApplicationProfileInstance { ApplicationProfile = profile };

        Assert.Equal(45, ApplicationWorkspaceProgressTimeline.ResolveProfileSlaDays(application, profile));
    }

    [Fact]
    public void Build_Issued_KeepsMinistryLetterFileNamesOnDoneLegs()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2026, 8, 14),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        var firstLetterId = Guid.NewGuid();
        var thirdLetterId = Guid.NewGuid();
        application.ProgressHistory.Add(ApprovedLeg(application, 1, "energo.pdf", firstLetterId));
        application.ProgressHistory.Add(ApprovedLeg(application, 2, fileName: null, id: Guid.NewGuid()));
        application.ProgressHistory.Add(ApprovedLeg(application, 3, "gurlusyk.pdf", thirdLetterId));
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 4,
            Date = new DateTime(2026, 8, 14),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.ProcessIssued,
                NameTm = "Issued",
            },
        });

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("done", steps[1].State);
        Assert.Equal("done", steps[2].State);
        Assert.Equal("done", steps[3].State);
        Assert.Equal("done", steps[4].State);
        Assert.Equal("energo.pdf", steps[1].MinistryLetterFileName);
        Assert.Equal(firstLetterId, steps[1].ProgressId);
        Assert.True(string.IsNullOrEmpty(steps[2].MinistryLetterFileName));
        Assert.Equal("gurlusyk.pdf", steps[3].MinistryLetterFileName);
        Assert.Equal(thirdLetterId, steps[3].ProgressId);
        Assert.Equal("ok", steps[1].OutcomeKind);
        Assert.Equal("issued", steps[4].OutcomeKind);
        Assert.Equal(
            "Migration service · Issued",
            ApplicationWorkspaceProgressTimeline.FormatChromeCurrentStep(steps));
    }

    [Fact]
    public void Build_RejectedAndCancelled_UseDistinctOutcomeKinds()
    {
        var profile = ThreeLegProfile();
        var rejectedApp = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        rejectedApp.ProgressHistory.Add(ApprovedLeg(rejectedApp, 1, null, Guid.NewGuid()));
        rejectedApp.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = rejectedApp,
            Order = 2,
            Date = DateTime.Today,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessRejected },
        });

        var rejected = ApplicationWorkspaceProgressTimeline.Build(rejectedApp, profile, default, objectSpace: null);
        Assert.Equal("ok", rejected[1].OutcomeKind);
        Assert.Equal("rejected", rejected[4].OutcomeKind);

        var cancelledApp = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        cancelledApp.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = cancelledApp,
            Order = 1,
            Date = DateTime.Today,
            State = new ApplicationState { Code = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled },
        });

        Assert.Equal(
            "cancelled",
            ApplicationWorkspaceProgressTimeline.ResolveOutcomeKind("done", ApplicationProfileInstanceProgressStateCodes.ProcessCancelled));
        Assert.Equal(
            "rejected",
            ApplicationWorkspaceProgressTimeline.ResolveOutcomeKind("current", ApplicationProfileInstanceProgressLegCodes.ReviewRejected(1)));
        Assert.Equal(
            "issued",
            ApplicationWorkspaceProgressTimeline.ResolveOutcomeKind("done", ApplicationProfileInstanceProgressStateCodes.ProcessIssued));
    }

    private static ApplicationProfileInstanceProgress ApprovedLeg(
        ApplicationProfileInstance application,
        int leg,
        string? fileName,
        Guid id) =>
        new()
        {
            ID = id,
            ApplicationProfileInstance = application,
            Order = leg,
            Date = new DateTime(2026, 8, 14),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressLegCodes.ReviewApproved(leg),
                NameTm = "Approved",
            },
            MinistryLetterFile = string.IsNullOrWhiteSpace(fileName)
                ? null
                : new FileData { FileName = fileName },
        };

    private static ApplicationProfile ThreeLegProfile()
    {
        var profile = new ApplicationProfile
        {
            ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
            MinistrySlaDays = 14,
            MigrationSlaDays = 14,
        };
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmenenergo", NameTm = "Turkmenenergo" },
        });
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 2,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmenenergetika", NameTm = "Turkmenenergetika" },
        });
        profile.ApprovalLegs.Add(new ApplicationProfileApprovalLeg
        {
            Sequence = 3,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = "Turkmengurlusyk", NameTm = "Turkmengurlusyk" },
        });
        return profile;
    }
}