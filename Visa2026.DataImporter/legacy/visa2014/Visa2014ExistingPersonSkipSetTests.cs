using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ExistingPersonSkipSetTests
{
    [Fact]
    public void SelectExisting_keeps_people_already_on_target_and_their_aliases()
    {
        var existingTarget = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var postedTarget = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var existingLegacy = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        var aliasOfExisting = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2");
        var postedLegacy = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb1");
        var aliasOfPosted = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbb2");

        var map = new Dictionary<Guid, Guid>
        {
            [existingLegacy] = existingTarget,
            [aliasOfExisting] = existingTarget,
            [postedLegacy] = postedTarget,
            [aliasOfPosted] = postedTarget,
        };

        var skip = Visa2014ExistingPersonSkipSet.SelectExisting(map, new HashSet<Guid> { postedTarget });

        Assert.Contains(existingLegacy, skip);
        Assert.Contains(aliasOfExisting, skip);
        Assert.DoesNotContain(postedLegacy, skip);
        Assert.DoesNotContain(aliasOfPosted, skip);
    }

    [Fact]
    public void ShouldSkip_is_false_when_the_rule_is_off()
    {
        var person = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");
        Assert.False(Visa2014ExistingPersonSkipSet.ShouldSkip(null, person));
        Assert.False(Visa2014ExistingPersonSkipSet.ShouldSkip(new HashSet<Guid> { person }, Guid.Empty));
    }

    [Fact]
    public async Task Save_round_trip_is_utf8_json()
    {
        var directory = Path.Combine(Path.GetTempPath(), "visa-skip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var personMap = Path.Combine(directory, "Person.json");
            var path = Visa2014ExistingPersonSkipSet.PathBesidePersonIdMap(personMap);
            var id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

            await Visa2014ExistingPersonSkipSet.SaveAsync(path, new[] { id });

            var bytes = await File.ReadAllBytesAsync(path);
            Assert.True(bytes.Length > 3);
            Assert.NotEqual(0xFF, bytes[0]);
            Assert.NotEqual(0x00, bytes[1]);
            Assert.Contains(id, Visa2014ExistingPersonSkipSet.Load(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
