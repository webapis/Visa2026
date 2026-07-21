using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationProgressTransformTests
{
    [Fact]
    public void SynthesizeSteps_LongProcessWithProcessDate_InsertsStartedThenApprovalsThenMigrationStart()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            ManualApplicationNumber: "3717",
            ManualApplicationDate: new DateTime(2014, 6, 10),
            IsLongProcess: true,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: new DateTime(2014, 6, 10),
            MinisteriesDocumentDate: new DateTime(2014, 6, 20),
            MinisteriesDocumentNumber: "Z/11078",
            DateForwardedToMinConstruction: new DateTime(2014, 6, 23),
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: new DateTime(2014, 6, 27),
            ProcessNumber: "AS455977",
            Cancelled: false,
            Rejected: false);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 2);

        Assert.Equal(4, steps.Count);
        Assert.Equal("1_REVIEW_STARTED", steps[0].StateCode);
        Assert.Equal("1_REVIEW_APPROVED", steps[1].StateCode);
        Assert.Equal("2_REVIEW_APPROVED", steps[2].StateCode);
        Assert.Equal("PROCESS_STARTED", steps[3].StateCode);
        Assert.Equal(new DateTime(2014, 6, 27), steps[3].Date);
        Assert.Equal("ProcessNumber: AS455977", steps[3].Description);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
        Assert.DoesNotContain(steps, s => s.StateCode == "IS_BEING_PREPARED");
        Assert.DoesNotContain(steps, s => s.StateCode is "2_REVIEW_STARTED" or "3_REVIEW_STARTED");
    }

    [Fact]
    public void SynthesizeSteps_SimpleProcessWithProcessDate_InsertsOnlyMigrationStarted()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.NewGuid(),
            ManualApplicationNumber: "100",
            ManualApplicationDate: new DateTime(2014, 6, 1),
            IsLongProcess: false,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: null,
            MinisteriesDocumentDate: null,
            MinisteriesDocumentNumber: null,
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: new DateTime(2014, 6, 15),
            ProcessNumber: "P-1",
            Cancelled: false,
            Rejected: false);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 0);

        Assert.Single(steps);
        Assert.Equal("PROCESS_STARTED", steps[0].StateCode);
        Assert.Equal(new DateTime(2014, 6, 15), steps[0].Date);
        Assert.Equal("ProcessNumber: P-1", steps[0].Description);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
    }

    [Fact]
    public void SynthesizeSteps_MinistryCompleteWithoutProcessDate_InsertsOnlyMigrationStarted()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.NewGuid(),
            ManualApplicationNumber: "200",
            ManualApplicationDate: new DateTime(2014, 6, 1),
            IsLongProcess: true,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: new DateTime(2014, 6, 2),
            MinisteriesDocumentDate: new DateTime(2014, 6, 10),
            MinisteriesDocumentNumber: "DOC-1",
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: null,
            ProcessNumber: null,
            Cancelled: false,
            Rejected: false);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 2);

        Assert.Equal(4, steps.Count);
        Assert.Equal("1_REVIEW_STARTED", steps[0].StateCode);
        Assert.Equal("PROCESS_STARTED", steps[^1].StateCode);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
        Assert.DoesNotContain(steps, s => s.StateCode == "IS_BEING_PREPARED");
    }

    [Fact]
    public void SynthesizeSteps_OutOfOrderLegacyDates_KeepsMinistryApprovalsOrdered()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.NewGuid(),
            ManualApplicationNumber: "537",
            ManualApplicationDate: new DateTime(2026, 4, 1),
            IsLongProcess: true,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: new DateTime(2026, 4, 1),
            MinisteriesDocumentDate: new DateTime(2026, 5, 2),
            MinisteriesDocumentNumber: "7/1730",
            DateForwardedToMinConstruction: new DateTime(2026, 4, 25),
            DocNumberForwardedToMinConstruction: "7/1622-Gurluşyk ministrligi",
            ProcessDate: new DateTime(2026, 5, 4),
            ProcessNumber: null,
            Cancelled: false,
            Rejected: false);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 3);

        Assert.Equal(5, steps.Count);
        Assert.Equal("1_REVIEW_STARTED", steps[0].StateCode);
        Assert.Equal("1_REVIEW_APPROVED", steps[1].StateCode);
        Assert.Equal("2_REVIEW_APPROVED", steps[2].StateCode);
        Assert.Equal("3_REVIEW_APPROVED", steps[3].StateCode);
        Assert.Equal("PROCESS_STARTED", steps[4].StateCode);
        Assert.Equal(new DateTime(2026, 5, 4), steps[4].Date);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
        Assert.DoesNotContain(steps, s => s.StateCode is "2_REVIEW_STARTED" or "3_REVIEW_STARTED");
    }

    [Fact]
    public void SynthesizeSteps_MinistryRouteWithProcessNumber_MapsNumberToProcessingStart()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.NewGuid(),
            ManualApplicationNumber: "12/-7010",
            ManualApplicationDate: new DateTime(2015, 12, 23),
            IsLongProcess: true,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: 1,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            DateForwardedToMonistery: new DateTime(2015, 12, 23),
            MinisteriesDocumentDate: new DateTime(2015, 12, 23),
            MinisteriesDocumentNumber: "01//77",
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: new DateTime(2015, 12, 24),
            ProcessNumber: "AS538188",
            Cancelled: false,
            Rejected: false);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 1);

        Assert.Equal(3, steps.Count);
        Assert.Equal("1_REVIEW_STARTED", steps[0].StateCode);
        Assert.Equal("1_REVIEW_APPROVED", steps[1].StateCode);
        Assert.Equal("PROCESS_STARTED", steps[2].StateCode);
        Assert.Equal(new DateTime(2015, 12, 24), steps[2].Date);
        Assert.Equal("ProcessNumber: AS538188", steps[2].Description);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
    }
}
