using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstanceApprovalLegBackfillTests
{
    [Fact]
    public void ResolveShared_PrefersInstanceFkOverTemplateDefault()
    {
        var instanceChain = Chain("AH");
        var defaultChain = Chain("TE-EN");
        var application = ViaApp(instanceChain, defaultChain);

        var shared = ApplicationProfileInstanceApprovalLegBackfill.ResolveShared(application);

        Assert.Same(instanceChain, shared);
    }

    [Fact]
    public void ResolveShared_UsesTemplateDefaultWhenInstanceFkMissing()
    {
        var defaultChain = Chain("TE-EN");
        var application = ViaApp(instanceFk: null, defaultChain);

        var shared = ApplicationProfileInstanceApprovalLegBackfill.ResolveShared(application);

        Assert.Same(defaultChain, shared);
    }

    [Fact]
    public void Evaluate_DoesNotAssignWhenInstanceFkSet()
    {
        var application = ViaApp(Chain("AH"), Chain("TE-EN"));
        application.ApprovalLegVersionName = "Aşgabat häkimlik";
        application.ApprovalLegSnapshots =
        [
            new ApplicationProfileInstanceApprovalLegSnapshot { Sequence = 1, MinistryShortName = "AH" },
        ];

        var plan = ApplicationProfileInstanceApprovalLegBackfill.Evaluate(application);

        Assert.False(plan.AssignProfile);
        Assert.False(plan.StampName);
        Assert.False(plan.FillSnapshots);
    }

    [Fact]
    public void Evaluate_AssignsDefaultAndRequestsSnapshotWhenEmpty()
    {
        var application = ViaApp(instanceFk: null, Chain("TE-EN"));

        var plan = ApplicationProfileInstanceApprovalLegBackfill.Evaluate(application);

        Assert.True(plan.AssignProfile);
        Assert.True(plan.StampName);
        Assert.True(plan.FillSnapshots);
        Assert.Equal("TE-EN", plan.Shared?.Code);
    }

    [Fact]
    public void Evaluate_SkipsDirectMigration()
    {
        var application = new ApplicationProfileInstance
        {
            CreationProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.DirectToMigrationService,
                DefaultApprovalLegProfile = Chain("TE-EN"),
            },
        };

        Assert.False(ApplicationProfileInstanceApprovalLegBackfill.IsViaMinistry(application));
    }

    [Fact]
    public void FormatVersionName_PrefersNameTm()
    {
        var shared = new ApprovalLegProfile { Code = "TE-EN", NameTm = "Türkmenenergo-Energetika" };
        Assert.Equal("Türkmenenergo-Energetika", ApplicationProfileInstanceApprovalLegBackfill.FormatVersionName(shared));
    }

    private static ApplicationProfileInstance ViaApp(
        ApprovalLegProfile? instanceFk,
        ApprovalLegProfile defaultChain)
    {
        return new ApplicationProfileInstance
        {
            ApprovalLegProfile = instanceFk,
            ApplicationProfile = new ApplicationProfile
            {
                ProgressRoute = ApplicationProfileInstanceProgressRouteKind.ViaMinistries,
                DefaultApprovalLegProfile = defaultChain,
            },
            ApprovalLegSnapshots = new ObservableCollection<ApplicationProfileInstanceApprovalLegSnapshot>(),
        };
    }

    private static ApprovalLegProfile Chain(string code)
    {
        var shared = new ApprovalLegProfile { Code = code, NameTm = code };
        shared.MinistryLegs.Add(new ApprovalLegProfileMinistryLeg
        {
            Sequence = 1,
            ApprovingMinistry = new ApprovingMinistry { ShortNameTm = code },
        });
        return shared;
    }
}