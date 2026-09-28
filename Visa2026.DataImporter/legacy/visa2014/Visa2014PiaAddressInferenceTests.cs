using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014PiaAddressInferenceTests
{
    [Fact]
    public void PersonCanonicalSyntheticLegacyOid_is_deterministic_and_stable()
    {
        var personOid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var a = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid);
        var b = Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(personOid);

        Assert.Equal(a, b);
        Assert.NotEqual(Guid.Empty, a);
        Assert.NotEqual(
            a,
            Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(Guid.NewGuid()));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_prefers_address_of_residence()
    {
        var aor = Guid.NewGuid();
        var direct = Guid.NewGuid();
        var raw = Item(
            aor: aor,
            direct: direct,
            employee: Guid.NewGuid(),
            forFamilyMember: false);

        Assert.Equal(aor, Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_family_uses_sponsor_canonical_when_no_aor()
    {
        var employee = Guid.NewGuid();
        var direct = Guid.NewGuid();
        var raw = Item(aor: null, direct: direct, employee: employee, forFamilyMember: true);

        Assert.Equal(
            Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(employee),
            Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_family_without_sponsor_falls_back_to_direct()
    {
        var direct = Guid.NewGuid();
        var raw = Item(aor: null, direct: direct, employee: null, forFamilyMember: true);

        Assert.Equal(direct, Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_employee_direct_when_present()
    {
        var direct = Guid.NewGuid();
        var raw = Item(aor: null, direct: direct, employee: Guid.NewGuid(), forFamilyMember: false);

        Assert.Equal(direct, Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_employee_synthetic_when_no_address_fks()
    {
        var employee = Guid.NewGuid();
        var raw = Item(aor: null, direct: null, employee: employee, forFamilyMember: false);

        Assert.Equal(
            Visa2014PiaAddressInference.PersonCanonicalSyntheticLegacyOid(employee),
            Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveApplicationItemCurrentAddressLegacyKey_null_when_no_keys()
    {
        var raw = Item(aor: null, direct: null, employee: null, forFamilyMember: false);
        Assert.Null(Visa2014PiaAddressInference.ResolveApplicationItemCurrentAddressLegacyKey(raw));
    }

    private static Visa2014ApplicationItemRawRow Item(
        Guid? aor,
        Guid? direct,
        Guid? employee,
        bool forFamilyMember) =>
        new(
            LegacyOid: Guid.NewGuid(),
            LegacyApplicationProfileInstanceOid: Guid.NewGuid(),
            LegacyEmployeeOid: employee,
            LegacyFamilyMemberOid: forFamilyMember ? Guid.NewGuid() : null,
            LegacyPassportOid: null,
            LegacyPreviousPassportOid: null,
            LegacyVisaOid: null,
            LegacyNextVisaOid: null,
            LegacyWorkPermitOid: null,
            LegacyInvitationItemOid: null,
            LegacyPositionOid: null,
            LegacyAddressOfResidenceOid: aor,
            LegacyDirectAddressOid: direct,
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
            IsComplete: true,
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
