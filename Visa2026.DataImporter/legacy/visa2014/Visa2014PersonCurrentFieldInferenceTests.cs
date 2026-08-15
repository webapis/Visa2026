using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014PersonCurrentFieldInferenceTests
{
    [Fact]
    public void SelectCurrentWorkPermitOid_PicksLatestStartDateThenOid()
    {
        var older = (Oid: Guid.Parse("11111111-1111-1111-1111-111111111111"), StartDate: (DateTime?)new DateTime(2020, 1, 1, 0, 0, 0));
        var newerSameDaySmaller = (Oid: Guid.Parse("22222222-2222-2222-2222-222222222222"), StartDate: (DateTime?)new DateTime(2024, 6, 1, 0, 0, 0));
        var newerSameDayLarger = (Oid: Guid.Parse("33333333-3333-3333-3333-333333333333"), StartDate: (DateTime?)new DateTime(2024, 6, 1, 0, 0, 0));
        var noDate = (Oid: Guid.Parse("44444444-4444-4444-4444-444444444444"), StartDate: (DateTime?)null);

        var selected = Visa2014PersonCurrentFieldInference.SelectCurrentWorkPermitOid(
            [older, newerSameDaySmaller, newerSameDayLarger, noDate]);

        Assert.Equal(newerSameDayLarger.Oid, selected);
    }

    [Fact]
    public void SelectCurrentWorkPermitOid_EmptyOrNoDates_ReturnsNull()
    {
        Assert.Null(Visa2014PersonCurrentFieldInference.SelectCurrentWorkPermitOid([]));
        Assert.Null(Visa2014PersonCurrentFieldInference.SelectCurrentWorkPermitOid(
            [(Guid.Parse("11111111-1111-1111-1111-111111111111"), null)]));
    }

    [Fact]
    public void TrySetApplicationItemPersonCurrentFields_SetsEducationAndSalaryWhenFlagsMatch()
    {
        var visibility = ApplicationTypeVisibilityCatalog.Load();
        var personOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var educationOid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var typeName = FindTypeWithFlags(
            visibility,
            require: ["ShowCurrentEducation", "ShowCurrentSalary"],
            forbid: ["ShowRegistrations"]);

        Assert.False(string.IsNullOrWhiteSpace(typeName));

        var raw = CreateEmployeeRaw(personOid, legacyWorkPermitOid: null);
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);

        Visa2014PersonCurrentFieldInference.TrySetApplicationItemPersonCurrentFields(
            raw,
            typeName,
            visibility,
            currentEducationByPerson: new Dictionary<Guid, Guid> { [personOid] = educationOid },
            currentWorkPermitByPerson: new Dictionary<Guid, Guid>(),
            row);

        Assert.Equal(educationOid.ToString("D"), row["CurrentEducation"]);
        Assert.Equal(personOid.ToString("D"), row["CurrentSalary"]);
    }

    [Fact]
    public void TrySetApplicationItemPersonCurrentFields_SetsWorkPermitWhenMissingAndFlagged()
    {
        var visibility = ApplicationTypeVisibilityCatalog.Load();
        var personOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var workPermitOid = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var typeName = FindTypeWithFlags(
            visibility,
            require: ["ShowCurrentWorkPermitItem"],
            forbid: []);

        Assert.False(string.IsNullOrWhiteSpace(typeName));

        var raw = CreateEmployeeRaw(personOid, legacyWorkPermitOid: null);
        var row = new Dictionary<string, object?>(StringComparer.Ordinal);

        Visa2014PersonCurrentFieldInference.TrySetApplicationItemPersonCurrentFields(
            raw,
            typeName,
            visibility,
            currentEducationByPerson: new Dictionary<Guid, Guid>(),
            currentWorkPermitByPerson: new Dictionary<Guid, Guid> { [personOid] = workPermitOid },
            row);

        Assert.Equal(workPermitOid.ToString("D"), row["CurrentWorkPermitItem"]);
        Assert.Equal("pending_work_permit_location_audit", row["_audit_WorkPermittedLocations"]);
    }

    private static string FindTypeWithFlags(
        ApplicationTypeVisibilityCatalog visibility,
        string[] require,
        string[] forbid)
    {
        foreach (var name in visibility.ApplicationTypeNames)
        {
            if (!visibility.TryGetFlags(name, out var flags))
                continue;

            if (require.Any(key => !flags.TryGetValue(key, out var on) || !on))
                continue;

            if (forbid.Any(key => flags.TryGetValue(key, out var on) && on))
                continue;

            return name;
        }

        return "";
    }

    private static Visa2014ApplicationItemRawRow CreateEmployeeRaw(Guid personOid, Guid? legacyWorkPermitOid) =>
        new(
            LegacyOid: Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"),
            LegacyApplicationOid: Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"),
            LegacyEmployeeOid: personOid,
            LegacyFamilyMemberOid: null,
            LegacyPassportOid: null,
            LegacyPreviousPassportOid: null,
            LegacyVisaOid: null,
            LegacyNextVisaOid: null,
            LegacyWorkPermitOid: legacyWorkPermitOid,
            LegacyInvitationItemOid: null,
            LegacyPositionOid: null,
            LegacyAddressOfResidenceOid: null,
            LegacyDirectAddressOid: null,
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
            ForEmployee: true,
            ForFamilyMember: false,
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
