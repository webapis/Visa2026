using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public sealed class Visa2014ApplicationTransformCompositeTests
{
    [Fact]
    public void BuildApplicationTypeComposite_EmployeeSubtype0WithInvitationWpFlag()
    {
        var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            forEmployee: true,
            forFamilyMember: false,
            employeeSubtypeId: 0,
            familySubtypeId: null,
            hasInvitationWpFk: true,
            invitationAndWorkPermitRequired: 1,
            hasWizaWpFk: false,
            wizaAndWorkPermitRequired: null,
            changeInformation: null);

        Assert.Equal("E:0:1:na:na", composite);
    }

    [Fact]
    public void BuildApplicationTypeComposite_WizaSubtypeUsesWizaWpSlot()
    {
        var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            forEmployee: true,
            forFamilyMember: false,
            employeeSubtypeId: 7,
            familySubtypeId: null,
            hasInvitationWpFk: false,
            invitationAndWorkPermitRequired: null,
            hasWizaWpFk: true,
            wizaAndWorkPermitRequired: 1,
            changeInformation: null);

        Assert.Equal("E:7:na:1:na", composite);
    }

    [Fact]
    public void BuildApplicationTypeComposite_ChangeInformationOnlyForSubtype5()
    {
        var withChange = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            forEmployee: true,
            forFamilyMember: false,
            employeeSubtypeId: 5,
            familySubtypeId: null,
            hasInvitationWpFk: false,
            invitationAndWorkPermitRequired: null,
            hasWizaWpFk: false,
            wizaAndWorkPermitRequired: null,
            changeInformation: 2);

        Assert.Equal("E:5:na:na:2", withChange);

        var ignoredChange = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            forEmployee: true,
            forFamilyMember: false,
            employeeSubtypeId: 1,
            familySubtypeId: null,
            hasInvitationWpFk: false,
            invitationAndWorkPermitRequired: null,
            hasWizaWpFk: false,
            wizaAndWorkPermitRequired: null,
            changeInformation: 2);

        Assert.Equal("E:1:na:na:na", ignoredChange);
    }

    [Fact]
    public void BuildApplicationTypeComposite_FamilyUsesFamilySubtype()
    {
        var composite = Visa2014ApplicationTransform.BuildApplicationTypeComposite(
            forEmployee: false,
            forFamilyMember: true,
            employeeSubtypeId: 99,
            familySubtypeId: 3,
            hasInvitationWpFk: false,
            invitationAndWorkPermitRequired: null,
            hasWizaWpFk: false,
            wizaAndWorkPermitRequired: null,
            changeInformation: null);

        Assert.Equal("F:3:na:na:na", composite);
    }

    [Theory]
    [InlineData("E:33:na:na:na", true)]
    [InlineData("E:55:na:na:na", true)]
    [InlineData("E:0:1:na:na", false)]
    public void IsSkippedApplicationTypeComposite_OnlyHardCodedSkips(string composite, bool expected)
    {
        Assert.Equal(expected, Visa2014ApplicationTransform.IsSkippedApplicationTypeComposite(composite));
    }

    [Theory]
    [InlineData("TDMGLBA", "Kerki şäheri", "TDMGLBA:Kerki")]
    [InlineData("TDMGLBA", "Atamyrat etraby", "TDMGLBA:Atamyrat")]
    [InlineData("TDMGLBA", "Aşgabat", "TDMGLBA")]
    [InlineData("OTHER", "Kerki", "OTHER")]
    [InlineData("TDMGLBA", null, "TDMGLBA")]
    public void ResolveMigrationServiceLegacyKey_DisambiguatesTdmglba(
        string? code, string? name, string expected)
    {
        Assert.Equal(expected, Visa2014ApplicationTransform.ResolveMigrationServiceLegacyKey(code, name));
    }

    [Fact]
    public void BuildApplicationIdentityGroupKey_RequiresNumberAndDate()
    {
        Assert.Null(Visa2014ApplicationTransform.BuildApplicationIdentityGroupKey("7/-1105", null, "App_Inv"));
        Assert.Null(Visa2014ApplicationTransform.BuildApplicationIdentityGroupKey(null, new DateTime(2020, 1, 2), "App_Inv"));

        Assert.Equal(
            "7/-1105|2020-01-02|App_Inv",
            Visa2014ApplicationTransform.BuildApplicationIdentityGroupKey(
                "7/-1105", new DateTime(2020, 1, 2), "App_Inv"));

        Assert.Equal(
            "7/-1105|2020-01-02",
            Visa2014ApplicationTransform.BuildApplicationIdentityGroupKey(
                "7/-1105", new DateTime(2020, 1, 2)));
    }

    [Fact]
    public void TryParseExportApplicationDate_AcceptsDateTimeAndParseableString()
    {
        Assert.True(Visa2014ApplicationTransform.TryParseExportApplicationDate(
            new DateTime(2021, 5, 6, 15, 30, 0), out var fromDt));
        Assert.Equal(new DateTime(2021, 5, 6), fromDt);

        Assert.True(Visa2014ApplicationTransform.TryParseExportApplicationDate("2021-05-06", out var fromText));
        Assert.Equal(new DateTime(2021, 5, 6), fromText.Date);

        Assert.False(Visa2014ApplicationTransform.TryParseExportApplicationDate("not-a-date", out _));
    }

    [Fact]
    public void FindApplicationIdMapCrossDateTargetCollisions_ReportsDistinctIdentities()
    {
        var target = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var legacyA = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var legacyB = Guid.Parse("22222222-2222-2222-2222-222222222222");

        var idMap = new Dictionary<Guid, Guid>
        {
            [legacyA] = target,
            [legacyB] = target,
        };

        var identities = new Dictionary<Guid, Visa2014ApplicationTransform.ApplicationImportIdentity>
        {
            [legacyA] = new("7/-1105", new DateTime(2020, 1, 2), "App_Inv"),
            [legacyB] = new("7/-1105", new DateTime(2020, 1, 3), "App_Inv"),
        };

        var collisions = Visa2014ApplicationTransform.FindApplicationIdMapCrossDateTargetCollisions(
            idMap, identities);

        Assert.Single(collisions);
        Assert.Contains(target.ToString("D"), collisions[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacyA.ToString("D"), collisions[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains(legacyB.ToString("D"), collisions[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("7/-1105|2020-01-02|App_Inv", collisions[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("7/-1105|2020-01-03|App_Inv", collisions[0], StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FindApplicationIdMapCrossDateTargetCollisions_IgnoresSingleLegacyPerTarget()
    {
        var idMap = new Dictionary<Guid, Guid>
        {
            [Guid.NewGuid()] = Guid.NewGuid(),
        };

        Assert.Empty(
            Visa2014ApplicationTransform.FindApplicationIdMapCrossDateTargetCollisions(
                idMap, new Dictionary<Guid, Visa2014ApplicationTransform.ApplicationImportIdentity>()));
    }
}
