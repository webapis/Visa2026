using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationMinistryLegCountResolverTests
{
    [Fact]
    public void SynthesizeSteps_SimpleProcessWithProfileLegCount2_IncludesMinistryLegs()
    {
        var raw = new Visa2014ApplicationProgressRawRow(
            LegacyApplicationOid: Guid.NewGuid(),
            ManualApplicationNumber: "7/-8308",
            ManualApplicationDate: new DateTime(2016, 7, 12),
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
            ProcessDate: new DateTime(2016, 7, 13),
            ProcessNumber: "AS0112444",
            Cancelled: false,
            Rejected: false);

        var steps = Visa2014ApplicationProgressTransform.SynthesizeSteps(raw, ministryLegCount: 2);

        Assert.Equal(4, steps.Count);
        Assert.Contains(steps, s => s.StateCode == "1_REVIEW_STARTED");
        Assert.Contains(steps, s => s.StateCode == "1_REVIEW_APPROVED");
        Assert.Contains(steps, s => s.StateCode == "2_REVIEW_APPROVED");
        Assert.Contains(steps, s => s.StateCode == "PROCESS_STARTED");
        Assert.DoesNotContain(steps, s => s.StateCode == "PROCESS_ISSUED");
        Assert.DoesNotContain(steps, s => s.StateCode is "2_REVIEW_STARTED" or "3_REVIEW_STARTED");
    }
}
