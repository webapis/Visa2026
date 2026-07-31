using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014VisaTransformVisaTypeTests
{
    [Fact]
    public void ResolveVisaType_FamilyMember_ForcesFmEvenWhenLegacyWp()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase);
        var key = Visa2014VisaTransform.ResolveVisaTypeLocalizationKey(
            isFamilyMemberPerson: true,
            composite: "WP:11",
            catalogs,
            out var reason,
            out var personOverride);

        Assert.Equal("FM", key);
        Assert.True(personOverride);
        Assert.Null(reason);
    }

    [Fact]
    public void ResolveVisaType_Employee_UsesCompositeOrDefaultWp()
    {
        var catalogs = new Dictionary<string, Visa2014LookupCatalog>(StringComparer.OrdinalIgnoreCase);
        var key = Visa2014VisaTransform.ResolveVisaTypeLocalizationKey(
            isFamilyMemberPerson: false,
            composite: "UNKNOWN:99",
            catalogs,
            out _,
            out var personOverride);

        Assert.Equal("WP", key);
        Assert.False(personOverride);
    }
}