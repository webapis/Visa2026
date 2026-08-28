using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

/// <summary>
/// Address id-map alias registration — duplicate keys must not overwrite mapped targets.
/// </summary>
public class Visa2014AddressOfResidenceIdMapAliasAppenderTests
{
    [Fact]
    public void RegisterIfMissing_AddsNewAlias()
    {
        var map = new Dictionary<Guid, Guid>();
        var legacy = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var target = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        var added = Visa2014AddressOfResidenceIdMapAliasAppender.RegisterIfMissing(map, legacy, target);

        Assert.Equal(1, added);
        Assert.Equal(target, map[legacy]);
    }

    [Fact]
    public void RegisterIfMissing_ExistingKey_DoesNotOverwrite()
    {
        var legacy = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var existing = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var other = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var map = new Dictionary<Guid, Guid> { [legacy] = existing };

        var added = Visa2014AddressOfResidenceIdMapAliasAppender.RegisterIfMissing(map, legacy, other);

        Assert.Equal(0, added);
        Assert.Equal(existing, map[legacy]);
    }
}
