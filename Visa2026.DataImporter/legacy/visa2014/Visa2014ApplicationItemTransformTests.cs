using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014ApplicationItemTransformTests
{
    private static readonly Guid AppOid = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid PersonOid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid PassportOid = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
    private static readonly Guid PiaOid = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
    private static readonly Guid PiaOid2 = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private static IReadOnlyDictionary<string, Visa2014LookupCatalog> ApplicationTypeCatalog(
        string composite = "E:1:na:na:na",
        string target = "App_Reg_Check_In") =>
        new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["ApplicationType"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "ApplicationType",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "block_row",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    [composite] = target,
                },
            },
        };

    private static Visa2014ApplicationItemRawRow Raw(
        Guid? oid = null,
        Guid? appOid = null,
        Guid? employeeOid = null,
        Guid? passportOid = null,
        bool forEmployee = true,
        bool forFamilyMember = false,
        int? employeeSubtypeId = 1,
        bool cancelled = false,
        bool hasBorderZoneFk = false,
        bool bzDasoguz = false,
        bool bzFarap = false,
        Guid? familyMemberOid = null,
        Guid? addressOfResidenceOid = null,
        Guid? directAddressOid = null,
        bool omitEmployeeOid = false) =>
        new(
            LegacyOid: oid ?? PiaOid,
            LegacyApplicationOid: appOid ?? AppOid,
            LegacyEmployeeOid: omitEmployeeOid ? null : employeeOid ?? (forEmployee ? PersonOid : null),
            LegacyFamilyMemberOid: familyMemberOid,
            LegacyPassportOid: passportOid,
            LegacyPreviousPassportOid: null,
            LegacyVisaOid: null,
            LegacyNextVisaOid: null,
            LegacyWorkPermitOid: null,
            LegacyInvitationItemOid: null,
            LegacyPositionOid: null,
            LegacyAddressOfResidenceOid: addressOfResidenceOid,
            LegacyDirectAddressOid: directAddressOid,
            RegistrationDate: new DateTime(2024, 1, 15),
            RegistrationNumber: "R-1",
            TravelDate: new DateTime(2024, 1, 20),
            TiTravelType: null,
            CheckPointMgCode: null,
            CheckPointLabel: null,
            PurposeOfTravelLabel: null,
            BusinessTripAddressText: null,
            BusinessTripCityMgCode: null,
            BusinessTripCityName: null,
            Cancelled: cancelled,
            Rejected: false,
            IsComplete: true,
            ForEmployee: forEmployee,
            ForFamilyMember: forFamilyMember,
            EmployeeSubtypeId: employeeSubtypeId,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null,
            HasBorderZoneFk: hasBorderZoneFk,
            BzDasoguz: bzDasoguz,
            BzTagtabazar: false,
            BzSerhetabat: false,
            BzYoloten: false,
            BzFarap: bzFarap,
            BzGarabogaz: false,
            BzSarahs: false,
            BzEtrek: false);

    [Fact]
    public void TryParseRawRow_ValidRow_ParsesCoreAndBorderZoneBits()
    {
        var oid = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var app = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var employee = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var passport = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var row = new Dictionary<string, string?>
        {
            ["Oid"] = oid.ToString("D"),
            ["ApplicationOid"] = app.ToString("D"),
            ["EmployeeOid"] = employee.ToString("D"),
            ["PassportOid"] = passport.ToString("D"),
            ["RegistrationDate"] = "2024-03-01",
            ["TravelDate"] = "2024-03-05",
            ["Cancelled"] = "0",
            ["Rejected"] = "1",
            ["IsComplete"] = "1",
            ["ForEmployee"] = "1",
            ["ForFamilyMember"] = "0",
            ["EmployeeSubtypeId"] = "1",
            ["HasBorderZoneFk"] = "1",
            ["BzDasoguz"] = "1",
            ["BzFarap"] = "1",
            ["BzEtrek"] = "0",
        };

        Assert.True(Visa2014ApplicationItemTransform.TryParseRawRow(row, out var parsed));
        Assert.Equal(oid, parsed.LegacyOid);
        Assert.Equal(app, parsed.LegacyApplicationOid);
        Assert.Equal(employee, parsed.LegacyEmployeeOid);
        Assert.Equal(passport, parsed.LegacyPassportOid);
        Assert.Equal(new DateTime(2024, 3, 1), parsed.RegistrationDate);
        Assert.True(parsed.Rejected);
        Assert.True(parsed.ForEmployee);
        Assert.Equal(1, parsed.EmployeeSubtypeId);
        Assert.True(parsed.HasBorderZoneFk);
        Assert.True(parsed.BzDasoguz);
        Assert.True(parsed.BzFarap);
        Assert.False(parsed.BzEtrek);
    }

    [Fact]
    public void TryParseRawRow_MissingOidOrApplication_ReturnsFalse()
    {
        Assert.False(Visa2014ApplicationItemTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["ApplicationOid"] = AppOid.ToString("D") },
            out _));
        Assert.False(Visa2014ApplicationItemTransform.TryParseRawRow(
            new Dictionary<string, string?> { ["Oid"] = PiaOid.ToString("D") },
            out _));
    }

    [Fact]
    public void BuildBorderZoneLocation_WithoutFk_ReturnsNoneSentinel()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase);
        var result = Visa2014ApplicationItemTransform.BuildBorderZoneLocation(
            catalogs,
            Raw(hasBorderZoneFk: false, bzDasoguz: true));

        Assert.Equal("\u00DDok", result);
    }

    [Fact]
    public void BuildBorderZoneLocation_MapsBitsViaCatalogAndPreservesOrder()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["BorderZoneName"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "BorderZoneName",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "use_legacy",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["Daşoguz"] = "BZ_DASOGUZ",
                    ["Farap"] = "BZ_FARAP",
                },
            },
        };

        var result = Visa2014ApplicationItemTransform.BuildBorderZoneLocation(
            catalogs,
            Raw(hasBorderZoneFk: true, bzDasoguz: true, bzFarap: true));

        Assert.Equal("BZ_DASOGUZ, BZ_FARAP", result);
    }

    [Fact]
    public void BuildBorderZoneLocation_UnmappedBit_FallsBackToBitKey()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase)
        {
            ["BorderZoneName"] = new Visa2014LookupCatalog
            {
                TargetCatalog = "BorderZoneName",
                TargetMatchProperty = "Name",
                UnmappedPolicy = "use_legacy",
                LegacyToTarget = new Dictionary<string, string>(StringComparer.Ordinal),
            },
        };

        var result = Visa2014ApplicationItemTransform.BuildBorderZoneLocation(
            catalogs,
            Raw(hasBorderZoneFk: true, bzDasoguz: true));

        Assert.Equal("Daşoguz", result);
    }

    [Fact]
    public void BuildBorderZoneLocation_FkWithNoBits_ReturnsNoneSentinel()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase);
        var result = Visa2014ApplicationItemTransform.BuildBorderZoneLocation(
            catalogs,
            Raw(hasBorderZoneFk: true));

        Assert.Equal("\u00DDok", result);
    }

    [Fact]
    public void TransformRows_SkippedApplicationTypeComposite_SkipsRow()
    {
        var catalogs = ApplicationTypeCatalog();
        var context = Visa2014ApplicationItemTransform.CreateTransformContext();

        var batch = Visa2014ApplicationItemTransform.TransformRows(
            [Raw(employeeSubtypeId: 33, passportOid: PassportOid)],
            catalogs,
            context,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("skip_row:parent_ApplicationType:E:33:na:na:na", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_MissingPerson_SkipsWithRequiredNull()
    {
        var catalogs = ApplicationTypeCatalog();
        var context = Visa2014ApplicationItemTransform.CreateTransformContext();

        var batch = Visa2014ApplicationItemTransform.TransformRows(
            [Raw(omitEmployeeOid: true, passportOid: PassportOid)],
            catalogs,
            context,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:Person", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_MissingPassport_SkipsWithRequiredNull()
    {
        var catalogs = ApplicationTypeCatalog();
        var context = Visa2014ApplicationItemTransform.CreateTransformContext();

        var batch = Visa2014ApplicationItemTransform.TransformRows(
            [Raw(passportOid: null)],
            catalogs,
            context,
            out var skipped,
            out _,
            out _);

        Assert.Empty(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("required_null:CurrentPassport", skipped[0]["_reason"]);
    }

    [Fact]
    public void TransformRows_HappyPath_DerivesRegistrationTravelTypes()
    {
        var catalogs = ApplicationTypeCatalog(target: "App_Reg_Check_In");
        var context = Visa2014ApplicationItemTransform.CreateTransformContext();

        var batch = Visa2014ApplicationItemTransform.TransformRows(
            [Raw(passportOid: PassportOid)],
            catalogs,
            context,
            out var skipped,
            out _,
            out _);

        Assert.Empty(skipped);
        Assert.Single(batch.ImportRows);
        var row = batch.ImportRows[0];
        Assert.Equal("import", row["_importAction"]);
        Assert.Equal("App_Reg_Check_In", row["ApplicationType"]);
        Assert.Equal(PersonOid.ToString("D"), row["Person"]);
        Assert.Equal(PassportOid.ToString("D"), row["CurrentPassport"]);
        Assert.Equal("External", row["TravelType"]);
        Assert.Equal("Entry", row["MovementType"]);
        Assert.Equal(true, row["VisaIssued"]);
        Assert.Equal("\u00DDok", row["BorderZoneLocation"]);
    }

    [Fact]
    public void TransformRows_DuplicateApplicationPerson_KeepsLowestOidCanonical()
    {
        var catalogs = ApplicationTypeCatalog();
        var context = Visa2014ApplicationItemTransform.CreateTransformContext();

        var batch = Visa2014ApplicationItemTransform.TransformRows(
            [
                Raw(oid: PiaOid2, passportOid: PassportOid),
                Raw(oid: PiaOid, passportOid: PassportOid),
            ],
            catalogs,
            context,
            out var skipped,
            out _,
            out var dedupeSummary);

        Assert.Single(batch.ImportRows);
        Assert.Single(skipped);
        Assert.Equal("dedupe_duplicate", skipped[0]["_reason"]);
        Assert.Equal(PiaOid, batch.ImportRows[0]["_legacyRowId"]);
        Assert.Single(dedupeSummary);
        Assert.Equal($"APP:{AppOid:D}:PERSON:{PersonOid:D}", dedupeSummary[0]["_dedupeGroupId"]);
        Assert.Equal(PiaOid, dedupeSummary[0]["canonical_legacyRowId"]);
        Assert.Equal("lowest_legacy_oid", dedupeSummary[0]["canonicalRule"]);
    }
}
