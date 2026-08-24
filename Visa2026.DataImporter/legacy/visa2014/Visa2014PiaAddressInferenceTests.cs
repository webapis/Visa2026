using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014PiaAddressInferenceTests
{
    private static readonly Guid EmployeeOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid FamilyOid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid AorOid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid DirectOid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

    private static Visa2014ApplicationItemRawRow Raw(
        bool forEmployee = true,
        bool forFamilyMember = false,
        Guid? employeeOid = null,
        Guid? familyMemberOid = null,
        Guid? addressOfResidenceOid = null,
        Guid? directAddressOid = null) =>
        new(
            LegacyOid: Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee"),
            LegacyApplicationOid: Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"),
            LegacyEmployeeOid: employeeOid,
            LegacyFamilyMemberOid: familyMemberOid,
            LegacyPassportOid: null,
            LegacyPreviousPassportOid: null,
            LegacyVisaOid: null,
            LegacyNextVisaOid: null,
            LegacyWorkPermitOid: null,
            LegacyInvitationItemOid: null,
            LegacyPositionOid: null,
            LegacyAddressOfResidenceOid: addressOfResidenceOid,
            LegacyDirectAddressOid: directAddressOid,
            RegistrationDate: null,
            RegistrationNumber: null,
            TravelDate: null,
            TiTravelType: null,
            CheckPointMgCode: null,
            CheckPointLabel: null,
            PurposeOfTravelLabel: null,
            BusinessTripAddressText: null,
            BusinessTripCityMgCode: null,
            BusinessTripCityName: null,
            Cancelled: false,
            Rejected: false,
            IsComplete: false,
            ForEmployee: forEmployee,
            ForFamilyMember: forFamilyMember,
            EmployeeSubtypeId: null,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            HasBorderZoneFk: false,
            BzDasoguz: false,
            BzTagtabazar: false,
            BzSerhetabat: false,
            BzYoloten: false,
            BzFarap: false,
            BzGarabogaz: false,
            BzSarahs: false,
            BzEtrek: false);

    [Fact]
    public void ResolveApplicationItemCurrentAddress_PrefersExplicitAddressOfResidence()
    {
        var key = Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(
            Raw(employeeOid: EmployeeOid, addressOfResidenceOid: AorOid, directAddressOid: DirectOid));

        Assert.Equal(AorOid, key);
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddress_EmployeeDirectAddress_WhenNoAor()
    {
        var key = Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(
            Raw(employeeOid: EmployeeOid, directAddressOid: DirectOid));

        Assert.Equal(DirectOid, key);
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddress_EmployeeSynthetic_WhenOnlyEmployeeOid()
    {
        var key = Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(
            Raw(employeeOid: EmployeeOid));

        Assert.Equal(Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(EmployeeOid), key);
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddress_FamilyLine_UsesSponsorSynthetic()
    {
        var key = Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(
            Raw(
                forEmployee: false,
                forFamilyMember: true,
                employeeOid: EmployeeOid,
                familyMemberOid: FamilyOid,
                directAddressOid: DirectOid));

        Assert.Equal(Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(EmployeeOid), key);
    }

    [Fact]
    public void PersonCanonicalSyntheticLegacyOid_IsDeterministic()
    {
        var a = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(EmployeeOid);
        var b = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(EmployeeOid);
        var other = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(FamilyOid);

        Assert.Equal(a, b);
        Assert.NotEqual(a, other);
    }
}
