using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014PiaAddressInferenceTests
{
    [Fact]
    public void PersonCanonicalSyntheticLegacyOid_IsDeterministic()
    {
        var personOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        var a = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid);
        var b = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid);

        Assert.Equal(a, b);
        Assert.NotEqual(personOid, a);
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_PrefersAddressOfResidence()
    {
        var aor = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var raw = CreateItemRaw(
            forFamilyMember: false,
            legacyAddressOfResidenceOid: aor,
            legacyDirectAddressOid: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            legacyEmployeeOid: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        Assert.Equal(aor, Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_EmployeeWithoutAor_UsesSyntheticFromEmployee()
    {
        var employeeOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var raw = CreateItemRaw(
            forFamilyMember: false,
            legacyAddressOfResidenceOid: null,
            legacyDirectAddressOid: null,
            legacyEmployeeOid: employeeOid);

        Assert.Equal(
            Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(employeeOid),
            Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_FamilyMember_UsesSponsorSynthetic()
    {
        var sponsorOid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var raw = CreateItemRaw(
            forFamilyMember: true,
            legacyAddressOfResidenceOid: null,
            legacyDirectAddressOid: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            legacyEmployeeOid: sponsorOid);

        Assert.Equal(
            Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(sponsorOid),
            Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void RegisterPlanAliases_WritesSyntheticAndLegacyAliases()
    {
        var synthetic = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var alias1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var alias2 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var target = Guid.Parse("99999999-9999-9999-9999-999999999999");
        var plan = new Visa2014PiaAddressInference.PiaInferredAddressPlan(
            LegacyPersonOid: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            SyntheticLegacyOid: synthetic,
            ImportRow: new Dictionary<string, object?>(),
            LegacyAddressOidAliases: [alias1, alias2]);
        var map = new Dictionary<Guid, Guid>();

        Visa2014PiaAddressInference.RegisterPlanAliases(plan, target, map);

        Assert.Equal(target, map[synthetic]);
        Assert.Equal(target, map[alias1]);
        Assert.Equal(target, map[alias2]);
    }

    private static Visa2014ApplicationItemRawRow CreateItemRaw(
        bool forFamilyMember,
        Guid? legacyAddressOfResidenceOid,
        Guid? legacyDirectAddressOid,
        Guid? legacyEmployeeOid) =>
        new(
            LegacyOid: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            LegacyApplicationOid: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            LegacyEmployeeOid: legacyEmployeeOid,
            LegacyFamilyMemberOid: forFamilyMember ? Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee") : null,
            LegacyPassportOid: null,
            LegacyPreviousPassportOid: null,
            LegacyVisaOid: null,
            LegacyNextVisaOid: null,
            LegacyWorkPermitOid: null,
            LegacyInvitationItemOid: null,
            LegacyPositionOid: null,
            LegacyAddressOfResidenceOid: legacyAddressOfResidenceOid,
            LegacyDirectAddressOid: legacyDirectAddressOid,
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
            ForEmployee: !forFamilyMember,
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
}
