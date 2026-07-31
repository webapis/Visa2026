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
        Assert.Equal("AS455977", steps[3].ProcessNumber);
        Assert.Null(steps[3].Description);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
        Assert.DoesNotContain(steps, s => s.StateCode == "IS_BEING_PREPARED");
        Assert.DoesNotContain(steps, s => s.StateCode is "2_REVIEW_STARTED" or "3_REVIEW_STARTED");
    }

    [Fact]
    public void SynthesizeSteps_DirectMigrationWithProcessDate_InsertsStartedAndIssued()
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

        Assert.Equal(2, steps.Count);
        Assert.Equal("PROCESS_STARTED", steps[0].StateCode);
        Assert.Equal(new DateTime(2014, 6, 15), steps[0].Date);
        Assert.Equal("P-1", steps[0].ProcessNumber);
        Assert.Null(steps[0].Description);
        Assert.Equal("PROCESS_ISSUED", steps[1].StateCode);
        Assert.Equal("P-1", steps[1].ProcessNumber);
        Assert.Null(steps[1].Description);
        Assert.True(steps[1].Date > steps[0].Date);
    }

    [Fact]
    public void SynthesizeSteps_DirectMigrationProcessDateOnly_InsertsStartedAndIssued()
    {
        var raw = BuildRaw(processDate: new DateTime(2014, 6, 15), processNumber: null);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 0);

        Assert.Equal(2, steps.Count);
        Assert.Equal("PROCESS_STARTED", steps[0].StateCode);
        Assert.Equal(new DateTime(2014, 6, 15), steps[0].Date);
        Assert.Equal("PROCESS_ISSUED", steps[1].StateCode);
        Assert.Null(steps[1].ProcessNumber);
        Assert.True(steps[1].Date > steps[0].Date);
    }

    [Fact]
    public void SynthesizeSteps_DirectMigrationProcessNumberOnly_InsertsOnlyMigrationStarted()
    {
        var raw = BuildRaw(processDate: null, processNumber: "BELGI-1");

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 0);

        Assert.Single(steps);
        Assert.Equal("PROCESS_STARTED", steps[0].StateCode);
        Assert.Equal("BELGI-1", steps[0].ProcessNumber);
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
        Assert.Equal("AS538188", steps[2].ProcessNumber);
        Assert.Null(steps[2].Description);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
    }

    [Fact]
    public void SynthesizeSteps_WithInvitationCompletion_AddsProcessIssued()
    {
        var raw = BuildRaw(processDate: new DateTime(2015, 12, 24), processNumber: "AS538188");
        var completion = new Visa2014ApplicationProgressCompletionEvidence(
            new DateTime(2016, 1, 14),
            "InvitationNumber",
            "01//77");

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 1, completion);

        Assert.Equal(4, steps.Count);
        Assert.Equal("PROCESS_STARTED", steps[2].StateCode);
        Assert.Equal(new DateTime(2015, 12, 24), steps[2].Date);
        Assert.Equal("AS538188", steps[2].ProcessNumber);
        Assert.Null(steps[2].Description);
        Assert.Equal("PROCESS_ISSUED", steps[3].StateCode);
        Assert.Equal(new DateTime(2016, 1, 14), steps[3].Date);
        Assert.Equal("01//77", steps[3].Description);
    }

    [Fact]
    public void SynthesizeSteps_WithWorkPermitCompletionOnly_AddsStartedThenIssued()
    {
        var raw = BuildRaw(processDate: null, processNumber: null);
        var completion = new Visa2014ApplicationProgressCompletionEvidence(
            new DateTime(2014, 6, 20),
            "WorkPermitNumber",
            "WP-42");

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 0, completion);

        Assert.Equal(2, steps.Count);
        Assert.Equal("PROCESS_STARTED", steps[0].StateCode);
        Assert.Equal(new DateTime(2014, 6, 2), steps[0].Date);
        Assert.Equal("PROCESS_ISSUED", steps[1].StateCode);
        Assert.Equal(new DateTime(2014, 6, 20), steps[1].Date);
        Assert.Equal("WP-42", steps[1].Description);
    }

    [Fact]
    public void SynthesizeSteps_WithVisaExtensionCompletion_AddsProcessIssued()
    {
        var raw = BuildRaw(processDate: null, processNumber: null);
        var completion = new Visa2014ApplicationProgressCompletionEvidence(
            new DateTime(2018, 5, 1),
            "VisaNumber",
            "V-EXT-1");

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 0, completion);

        Assert.Contains(steps, s => s.StateCode == "PROCESS_STARTED");
        var issued = Assert.Single(steps, s => s.StateCode == "PROCESS_ISSUED");
        Assert.Equal(new DateTime(2018, 5, 1), issued.Date);
        Assert.Equal("V-EXT-1", issued.Description);
    }

    [Fact]
    public void SynthesizeSteps_CancelledWithCompletion_DoesNotAddProcessIssued()
    {
        var raw = BuildRaw(cancelled: true);
        var completion = new Visa2014ApplicationProgressCompletionEvidence(
            new DateTime(2016, 1, 14),
            "InvitationNumber",
            "01//77");

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 0, completion);

        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
        Assert.Contains(steps, s => s.StateCode == "PROCESS_CANCELLED");
    }

    [Fact]
    public void SynthesizeSteps_LongProcess_MinisteriesDocumentNumberOnLeg2NotLeg1()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.NewGuid(),
            ManualApplicationNumber: "7/-1177",
            ManualApplicationDate: new DateTime(2026, 7, 13),
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
            DateForwardedToMonistery: new DateTime(2026, 7, 13),
            MinisteriesDocumentDate: new DateTime(2026, 7, 17),
            MinisteriesDocumentNumber: "7/2820",
            DateForwardedToMinConstruction: null,
            DocNumberForwardedToMinConstruction: null,
            ProcessDate: new DateTime(2026, 7, 18),
            ProcessNumber: null,
            Cancelled: false,
            Rejected: false);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 2);

        var leg1Approved = steps.Single(s => s.StateCode == "1_REVIEW_APPROVED");
        var leg2Approved = steps.Single(s => s.StateCode == "2_REVIEW_APPROVED");

        Assert.Null(leg1Approved.Description);
        Assert.Equal("MinisteriesDocumentNumber: 7/2820", leg2Approved.Description);
    }

    [Fact]
    public void SynthesizeSteps_ThreeLegs_ConstructionDocOnLeg3()
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

        Assert.Null(steps.Single(s => s.StateCode == "1_REVIEW_APPROVED").Description);
        Assert.Equal("MinisteriesDocumentNumber: 7/1730",
            steps.Single(s => s.StateCode == "2_REVIEW_APPROVED").Description);
        Assert.Equal("DocNumberForwardedToMinConstruction: 7/1622-Gurluşyk ministrligi",
            steps.Single(s => s.StateCode == "3_REVIEW_APPROVED").Description);
    }


    [Fact]
    public void SynthesizeSteps_FullRejectionCoverage_AddsProcessRejected_WithoutLegacyFlag()
    {
        var raw = BuildRaw(processDate: new DateTime(2017, 3, 1));
        var rejection = new Visa2014ApplicationProgressRejectionEvidence(
            ApplicationItemCount: 2,
            RejectionItemCount: 2,
            RejectionDate: new DateTime(2017, 3, 15),
            RejectionNumbers: "R-100");

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(
            raw, ministryLegCount: 0, completion: null, rejection: rejection);

        var rejected = Assert.Single(steps, s => s.StateCode == "PROCESS_REJECTED");
        Assert.Equal(new DateTime(2017, 3, 15), rejected.Date);
        Assert.Contains("Rejection coverage 2/2", rejected.Description);
        Assert.Contains("R-100", rejected.Description!);
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
    }

    [Fact]
    public void SynthesizeSteps_LegacyRejectedOrCoverage_PrefersRejectionDate()
    {
        var raw = BuildRaw(processDate: new DateTime(2017, 3, 1)) with { Rejected = true };
        var rejection = new Visa2014ApplicationProgressRejectionEvidence(
            ApplicationItemCount: 1,
            RejectionItemCount: 1,
            RejectionDate: new DateTime(2017, 4, 1),
            RejectionNumbers: "178");

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(
            raw, ministryLegCount: 0, completion: null, rejection: rejection);

        var rejected = Assert.Single(steps, s => s.StateCode == "PROCESS_REJECTED");
        Assert.Equal(new DateTime(2017, 4, 1), rejected.Date);
        Assert.Contains("Legacy Rejected=1", rejected.Description);
        Assert.Contains("Rejection coverage 1/1", rejected.Description);
    }

    [Fact]
    public void SynthesizeSteps_PartialRejectionCoverage_DoesNotAddProcessRejected()
    {
        var raw = BuildRaw();
        var rejection = new Visa2014ApplicationProgressRejectionEvidence(
            ApplicationItemCount: 4,
            RejectionItemCount: 1,
            RejectionDate: new DateTime(2017, 3, 15),
            RejectionNumbers: null);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(
            raw, ministryLegCount: 0, completion: null, rejection: rejection);

        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_REJECTED");
    }
    private static Visa2014ApplicationProgressRawRow BuildRaw(
        DateTime? processDate = null,
        string? processNumber = null,
        bool cancelled = false) =>
        new(
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
            ProcessDate: processDate,
            ProcessNumber: processNumber,
            Cancelled: cancelled,
            Rejected: false);
}
