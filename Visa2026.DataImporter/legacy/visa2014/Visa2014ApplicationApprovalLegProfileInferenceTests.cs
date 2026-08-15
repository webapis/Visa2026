using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ApplicationApprovalLegProfileInferenceTests
{
    [Fact]
    public void ResolveProfileCode_TurkmenenergoWithoutConstruction_ReturnsTeEn()
    {
        var raw = CreateRaw(
            appliedMinistryTitleL: "Türkmenenergo",
            dateForwardedToMinistry: new DateTime(2024, 5, 1, 0, 0, 0));

        Assert.Equal("TE-EN", Visa2014ApplicationApprovalLegProfileInference.ResolveProfileCode(raw));
    }

    [Fact]
    public void ResolveProfileCode_TurkmenenergoWithConstruction_ReturnsTeEnGu()
    {
        var raw = CreateRaw(
            appliedMinistryTitleL: "Türkmenenergo",
            dateForwardedToMinistry: new DateTime(2024, 5, 1, 0, 0, 0),
            dateForwardedToMinConstruction: new DateTime(2024, 6, 1, 0, 0, 0));

        Assert.Equal("TE-EN-GU", Visa2014ApplicationApprovalLegProfileInference.ResolveProfileCode(raw));
    }

    [Fact]
    public void ResolveProfileCode_GazWithMinistryForwardWithoutConstruction_ReturnsTg()
    {
        var raw = CreateRaw(
            appliedMinistryTitle: "Ministry of Gaz",
            dateForwardedToMinistry: new DateTime(2024, 5, 1, 0, 0, 0));

        Assert.Equal("TG", Visa2014ApplicationApprovalLegProfileInference.ResolveProfileCode(raw));
    }

    [Fact]
    public void ResolveProfileCode_GazWithMinistryForwardAndConstructionDoc_ReturnsTgGu()
    {
        var raw = CreateRaw(
            appliedMinistryTitle: "Ministry of Gaz",
            dateForwardedToMinistry: new DateTime(2024, 5, 1, 0, 0, 0),
            docNumberForwardedToMinConstruction: "DOC-1");

        Assert.Equal("TG-GU", Visa2014ApplicationApprovalLegProfileInference.ResolveProfileCode(raw));
    }

    [Fact]
    public void ResolveProfileCode_UnmappedFallsBackToEnergetikaPath_ReturnsTeEn()
    {
        var raw = CreateRaw(appliedMinistryTitle: "Unknown Ministry");

        Assert.Equal("TE-EN", Visa2014ApplicationApprovalLegProfileInference.ResolveProfileCode(raw));
    }

    [Fact]
    public void ResolveProfileCode_Pre2000Dates_AreIgnoredForLegs()
    {
        var raw = CreateRaw(
            appliedMinistryTitleL: "Türkmenenergo",
            dateForwardedToMinistry: new DateTime(1999, 1, 1, 0, 0, 0),
            dateForwardedToMinConstruction: new DateTime(1995, 1, 1, 0, 0, 0),
            contractMinistryTitleL: "energo");

        // No ministry forward (>=2000), but contract title still marks energo flow without construction.
        Assert.Equal("TE-EN", Visa2014ApplicationApprovalLegProfileInference.ResolveProfileCode(raw));
    }

    private static Visa2014ApplicationRawRow CreateRaw(
        string? appliedMinistryTitle = null,
        string? appliedMinistryTitleL = null,
        DateTime? dateForwardedToMinistry = null,
        DateTime? dateForwardedToMinConstruction = null,
        string? docNumberForwardedToMinConstruction = null,
        string? contractMinistryTitle = null,
        string? contractMinistryTitleL = null) =>
        new(
            LegacyOid: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            ManualApplicationNumber: null,
            ManualApplicationDate: null,
            AutoRegistration: false,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: null,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            ApplicationUrgency: null,
            UrgencyMgCode: null,
            PeriodOfVisaL: null,
            VisaPeriodMgCode: null,
            VisaPeriodCountMonth: null,
            CategoryOfVisaL: null,
            VisaCategoryMgCode: null,
            NumberOfContract: null,
            ToCityMgCode: null,
            ToCityName: null,
            DateOfDeparture: null,
            DurationOfStay: 0,
            MovementPermitNameTm: null,
            HasBorderZoneFk: false,
            BzDasoguz: false,
            BzTagtabazar: false,
            BzSerhetabat: false,
            BzYoloten: false,
            BzFarap: false,
            BzGarabogaz: false,
            BzSarahs: false,
            BzEtrek: false,
            DepartmentForRegistrationCode: null,
            DepartmentForRegistrationName: null,
            AppliedMinistryTitle: appliedMinistryTitle,
            AppliedMinistryTitleL: appliedMinistryTitleL,
            DateForwardedToMinistry: dateForwardedToMinistry,
            DateForwardedToMinConstruction: dateForwardedToMinConstruction,
            DocNumberForwardedToMinConstruction: docNumberForwardedToMinConstruction,
            ContractMinistryTitle: contractMinistryTitle,
            ContractMinistryTitleL: contractMinistryTitleL,
            LegacyPersonOid: null);
}
