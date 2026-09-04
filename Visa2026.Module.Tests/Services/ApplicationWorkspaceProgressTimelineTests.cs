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
        Assert.Contains(steps[0].ResultOptions, o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.Review1Started);
        Assert.Equal(
            ApplicationProfileInstanceProgressStateCodes.Review1Started,
            steps[0].ResultOptions[0].StateCode);
        Assert.Contains(steps[0].ResultOptions, o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.ProcessCancelled);
        Assert.Equal(
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            steps[0].ResultOptions[^1].StateCode);
        Assert.DoesNotContain(
            steps[0].ResultOptions,
            o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.Review1Rejected);
        Assert.False(steps[0].ShowMinistryLetterUpload);
        Assert.Equal(
            ApplicationWorkspaceProgressTimeline.OfficeLabel,
            ApplicationWorkspaceProgressTimeline.FormatChromeCurrentStep(steps));
    }

    [Fact]
    public void Build_OfficeCancelled_ShowsCancelledOnOffice()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2024, 8, 17),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = new DateTime(2024, 8, 18),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
                NameTm = "Cancelled",
            },
        });

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("current", steps[0].State);
        Assert.Equal("Cancelled", steps[0].CurrentStateLabel);
        Assert.Equal("cancelled", steps[0].OutcomeKind);
        Assert.Equal("18 Aug 2024", steps[0].Date);
        Assert.False(steps[0].CanAdvance);
        Assert.True(steps[0].CanRevert);
        Assert.Contains("terminal", steps[0].AdvanceBlockedReason, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("pending", steps[1].State);
        Assert.True(string.IsNullOrEmpty(steps[1].CurrentStateLabel));
        Assert.False(steps[1].CanRevert);
        Assert.Equal("pending", steps[4].State);
        Assert.NotEqual("Cancelled", steps[4].CurrentStateLabel);
        Assert.False(steps[4].CanRevert);
        Assert.Equal(
            "Office preparation · Cancelled",
            ApplicationWorkspaceProgressTimeline.FormatChromeCurrentStep(steps));
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
        Assert.Equal("Submitted", steps[0].CurrentStateLabel);
        Assert.Equal("14 Aug 2026", steps[0].Date);
        Assert.Equal("current", steps[1].State);
        Assert.Equal("Turkmenenergo", steps[1].Label);
        Assert.False(string.IsNullOrWhiteSpace(steps[1].CurrentStateLabel));
        Assert.Equal("14 Aug 2026", steps[1].Date);
        Assert.Equal("Submitted", steps[1].CurrentStateLabel);
        Assert.Equal(
            "Turkmenenergo · Submitted",
            ApplicationWorkspaceProgressTimeline.FormatChromeCurrentStep(steps));
        Assert.Equal("pending", steps[2].State);
        Assert.Equal("pending", steps[3].State);
        Assert.Equal("pending", steps[4].State);
        Assert.True(steps[1].CanAdvance);
        Assert.True(steps[1].CanRevert);
        Assert.True(steps[0].CanRevertToHere);
        Assert.False(steps[0].CanRevert);
        Assert.Contains(steps[1].AdvanceOptions, o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.Review1Approved);
        Assert.Contains(steps[1].ResultOptions, o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.Review1Approved);
        Assert.Contains(steps[1].ResultOptions, o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.Review1Rejected);
        Assert.Equal(
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            steps[1].ResultOptions[^1].StateCode);
        Assert.True(steps[1].ShowMinistryLetterUpload);
    }

    [Fact]
    public void Build_SubmittedThenApproved_OfficeKeepsSubmittedOnBar()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = new DateTime(2024, 8, 17),
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = new DateTime(2024, 8, 19),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
                NameTm = "Sent for agreement",
            },
        });
        var approved = ApprovedLeg(application, 1, null, Guid.NewGuid());
        approved.Order = 2;
        application.ProgressHistory.Add(approved);

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("Submitted", steps[0].CurrentStateLabel);
        Assert.Equal("19 Aug 2024", steps[0].Date);
        Assert.Equal("done", steps[1].State);
        Assert.Equal("Approved", steps[1].CurrentStateLabel);
        Assert.Equal("current", steps[2].State);
    }

    [Fact]
    public void Build_FirstLegStartedThenCancelled_KeepsCancelledOnThatMinistry()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        var started = new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 1,
            Date = new DateTime(2024, 8, 14),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.Review1Started,
                NameTm = "Submitted",
            },
        };
        var cancelled = new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 2,
            Date = new DateTime(2024, 8, 17),
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
                NameTm = "Cancelled",
            },
        };
        application.ProgressHistory.Add(started);
        application.ProgressHistory.Add(cancelled);

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("Submitted", steps[0].CurrentStateLabel);
        Assert.Equal("14 Aug 2024", steps[0].Date);
        Assert.Equal("current", steps[1].State);
        Assert.Equal("Cancelled", steps[1].CurrentStateLabel);
        Assert.Equal("cancelled", steps[1].OutcomeKind);
        Assert.Equal("pending", steps[2].State);
        Assert.True(string.IsNullOrEmpty(steps[2].CurrentStateLabel));
        Assert.Equal("pending", steps[4].State);
        Assert.NotEqual("Cancelled", steps[4].CurrentStateLabel);
        Assert.False(steps[4].CanRevert);
        Assert.True(steps[1].CanRevert);
        Assert.Equal(
            "Turkmenenergo · Cancelled",
            ApplicationWorkspaceProgressTimeline.FormatChromeCurrentStep(steps));
    }

    [Fact]
    public void Build_FirstLegApprovedThenCancelled_KeepsCancelledOnNextMinistry()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(ApprovedLeg(application, 1, null, Guid.NewGuid()));
        application.ProgressHistory.Add(new ApplicationProfileInstanceProgress
        {
            ID = Guid.NewGuid(),
            ApplicationProfileInstance = application,
            Order = 2,
            Date = DateTime.Today,
            State = new ApplicationState
            {
                Code = ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
                NameTm = "Cancelled",
            },
        });

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[1].State);
        Assert.Equal("Approved", steps[1].CurrentStateLabel);
        Assert.Equal("current", steps[2].State);
        Assert.Equal("Cancelled", steps[2].CurrentStateLabel);
        Assert.Equal("cancelled", steps[2].OutcomeKind);
        Assert.True(steps[2].CanRevert);
        Assert.Equal("pending", steps[4].State);
        Assert.False(steps[4].CanRevert);
        Assert.Equal(
            "Turkmenenergetika · Cancelled",
            ApplicationWorkspaceProgressTimeline.FormatChromeCurrentStep(steps));
    }

    [Fact]
    public void Build_FirstLegApproved_NextMinistryIsCurrent()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(ApprovedLeg(application, 1, null, Guid.NewGuid()));

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[0].State);
        Assert.Equal("done", steps[1].State);
        Assert.Equal("Approved", steps[1].CurrentStateLabel);
        Assert.Equal("current", steps[2].State);
        Assert.Equal("Turkmenenergetika", steps[2].Label);
        Assert.True(string.IsNullOrEmpty(steps[2].CurrentStateLabel));
        Assert.Equal("pending", steps[3].State);
        Assert.Equal("pending", steps[4].State);
        Assert.Contains(steps[2].ResultOptions, o => o.StateCode == ApplicationProfileInstanceProgressLegCodes.ReviewApproved(2));
        Assert.Contains(steps[2].ResultOptions, o => o.StateCode == ApplicationProfileInstanceProgressLegCodes.ReviewRejected(2));
        Assert.Equal(
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            steps[2].ResultOptions[^1].StateCode);
        Assert.True(steps[1].MissingMinistryLetter);
        Assert.True(steps[1].ShowMinistryLetterUpload);
        Assert.NotNull(steps[1].DecisionProgressId);
        Assert.True(steps[2].ShowMinistryLetterUpload);
        Assert.Null(steps[2].DecisionProgressId);
        Assert.False(steps[2].MissingMinistryLetter);
        Assert.True(steps[2].CanAdvance);
        Assert.True(steps[2].CanRevert);
    }

    [Fact]
    public void Build_LastMinistryApproved_MigrationIsCurrent()
    {
        var profile = ThreeLegProfile();
        var application = new ApplicationProfileInstance
        {
            ApplicationProfile = profile,
            ApplicationDate = DateTime.Today,
            ProgressHistory = new ObservableCollection<ApplicationProfileInstanceProgress>(),
        };
        application.ProgressHistory.Add(ApprovedLeg(application, 1, null, Guid.NewGuid()));
        application.ProgressHistory.Add(ApprovedLeg(application, 2, null, Guid.NewGuid()));
        application.ProgressHistory.Add(ApprovedLeg(application, 3, null, Guid.NewGuid()));

        var steps = ApplicationWorkspaceProgressTimeline.Build(application, profile, default, objectSpace: null);

        Assert.Equal("done", steps[1].State);
        Assert.Equal("done", steps[2].State);
        Assert.Equal("done", steps[3].State);
        Assert.Equal("current", steps[4].State);
        Assert.Equal(
            ApplicationProfileInstanceProgressStateCodes.ProcessCancelled,
            steps[4].ResultOptions[^1].StateCode);
        Assert.False(steps[4].ShowMinistryLetterUpload);
        Assert.True(steps[3].MissingMinistryLetter);
        Assert.True(steps[3].ShowMinistryLetterUpload);
        Assert.True(steps[3].DecisionProgressId.HasValue);
        Assert.True(steps[4].CanAdvance);
        Assert.Contains(steps[4].AdvanceOptions, o => o.StateCode == ApplicationProfileInstanceProgressStateCodes.ProcessStarted);
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
    public void Build_PrefersSnapshotsOverProfileVersions()
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

        Assert.Equal("Snapshot-A", steps[1].Label);
        Assert.Equal("Snapshot-B", steps[2].Label);
        Assert.DoesNotContain(steps, s => s.Label == "Turkmenenergo");
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
        Assert.True(steps[2].MissingMinistryLetter);
        Assert.True(steps[2].ShowMinistryLetterUpload);
        Assert.True(steps[2].DecisionProgressId.HasValue);
        Assert.False(steps[1].MissingMinistryLetter);
        Assert.Equal("gurlusyk.pdf", steps[3].MinistryLetterFileName);
        Assert.Equal(thirdLetterId, steps[3].ProgressId);
        Assert.Equal("ok", steps[1].OutcomeKind);
        Assert.Equal("issued", steps[4].OutcomeKind);
        Assert.True(steps[0].CanRevertToHere);
        Assert.True(steps[1].CanRevertToHere);
        Assert.True(steps[4].CanRevert);
        Assert.False(steps[4].CanRevertToHere);
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