using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ApplicationPersonDocumentLinksTests
{
    [Fact]
    public void Diff_ReplacesLatestWithApplicationSnapshot()
    {
        var current = Guid.NewGuid();
        var previous = Guid.NewGuid();
        var latest = Guid.NewGuid();

        Visa2014ApplicationPersonDocumentLinks.Diff(
            existing: [latest],
            desired: [previous, current],
            out var remove,
            out var add);

        Assert.Equal([latest], remove);
        Assert.Equal(2, add.Count);
        Assert.Contains(previous, add);
        Assert.Contains(current, add);
    }

    [Fact]
    public void Diff_AlreadyPinned_NoChange()
    {
        var current = Guid.NewGuid();
        var previous = Guid.NewGuid();

        Visa2014ApplicationPersonDocumentLinks.Diff(
            existing: [previous, current],
            desired: [current, previous],
            out var remove,
            out var add);

        Assert.Empty(remove);
        Assert.Empty(add);
    }

    [Fact]
    public void MapLegacyOids_SkipsMissingAndDuplicates()
    {
        var legacyCurrent = Guid.NewGuid();
        var legacyPrevious = Guid.NewGuid();
        var missing = Guid.NewGuid();
        var targetCurrent = Guid.NewGuid();
        var targetPrevious = Guid.NewGuid();
        var map = new Dictionary<Guid, Guid>
        {
            [legacyCurrent] = targetCurrent,
            [legacyPrevious] = targetPrevious,
        };

        var ids = Visa2014ApplicationPersonDocumentLinks.MapLegacyOids(
            map, legacyPrevious, legacyCurrent, missing, legacyCurrent);

        Assert.Equal([targetPrevious, targetCurrent], ids);
    }

    [Fact]
    public void ResolveAddressLegacyKey_PrefersAddressOfResidence()
    {
        var aor = Guid.NewGuid();
        var direct = Guid.NewGuid();
        var raw = new Visa2014ApplicationProfileInstancePersonRawRow(
            LegacyOid: Guid.NewGuid(),
            LegacyApplicationProfileInstanceOid: Guid.NewGuid(),
            LegacyEmployeeOid: Guid.NewGuid(),
            LegacyFamilyMemberOid: null,
            LegacyPassportOid: null,
            LegacyPreviousPassportOid: null,
            LegacyVisaOid: null,
            LegacyWorkPermitOid: null,
            LegacyPositionOid: null,
            LegacyAddressOfResidenceOid: aor,
            LegacyDirectAddressOid: direct,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: null,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null);

        Assert.Equal(aor, Visa2014ApplicationPersonRequiredPersonLinks.ResolveAddressLegacyKey(raw));
    }

    [Fact]
    public void ResolveAddressLegacyKey_EmployeeDirectAddress_WhenNoAor()
    {
        var direct = Guid.NewGuid();
        var raw = new Visa2014ApplicationProfileInstancePersonRawRow(
            LegacyOid: Guid.NewGuid(),
            LegacyApplicationProfileInstanceOid: Guid.NewGuid(),
            LegacyEmployeeOid: Guid.NewGuid(),
            LegacyFamilyMemberOid: null,
            LegacyPassportOid: null,
            LegacyPreviousPassportOid: null,
            LegacyVisaOid: null,
            LegacyWorkPermitOid: null,
            LegacyPositionOid: null,
            LegacyAddressOfResidenceOid: null,
            LegacyDirectAddressOid: direct,
            ForEmployee: true,
            ForFamilyMember: false,
            EmployeeSubtypeId: null,
            FamilySubtypeId: null,
            HasInvitationWpFk: false,
            InvitationAndWorkPermitRequired: null,
            HasWizaWpFk: false,
            WizaAndWorkPermitRequired: null,
            ChangeInformation: null);

        Assert.Equal(direct, Visa2014ApplicationPersonRequiredPersonLinks.ResolveAddressLegacyKey(raw));
    }
}
