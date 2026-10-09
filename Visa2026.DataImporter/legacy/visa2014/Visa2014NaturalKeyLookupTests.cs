using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014NaturalKeyLookupTests
{
    [Fact]
    public void Passport_key_uses_person_and_trimmed_number()
    {
        var personId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var lookup = Visa2014NaturalKeyLookup.ForPassport(personId, "  AB 12 ");

        Assert.NotNull(lookup);
        Assert.Equal($"passport|{personId:D}|AB 12", lookup!.CacheKey);
        Assert.Equal(Visa2014NaturalKeyKind.Passport, lookup.Kind);
    }

    [Fact]
    public void Passport_key_is_missing_when_number_is_blank()
    {
        var lookup = Visa2014NaturalKeyLookup.ForPassport(Guid.NewGuid(), "  ");
        Assert.Null(lookup);
    }

    [Fact]
    public void WorkPermit_key_uses_number_and_calendar_day()
    {
        var lookup = Visa2014NaturalKeyLookup.ForWorkPermit("WP-9", "2019-04-02T15:40:00");

        Assert.NotNull(lookup);
        Assert.Equal("workpermit|WP-9|2019-04-02", lookup!.CacheKey);
        Assert.Equal(new DateTime(2019, 4, 2), lookup.OnDate);
    }

    [Fact]
    public void Education_key_keeps_an_empty_graduation_year()
    {
        var personId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var lookup = Visa2014NaturalKeyLookup.ForEducation(personId, null);

        Assert.NotNull(lookup);
        Assert.Equal($"education|{personId:D}|", lookup!.CacheKey);
        Assert.Equal(string.Empty, lookup.Number);
    }

    [Fact]
    public void InvitationItem_key_requires_both_ids()
    {
        Assert.Null(Visa2014NaturalKeyLookup.ForInvitationItem(Guid.Empty, Guid.NewGuid()));
        var personId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var invitationId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var lookup = Visa2014NaturalKeyLookup.ForInvitationItem(personId, invitationId);
        Assert.Equal($"invitation-item|{personId:D}|{invitationId:D}", lookup!.CacheKey);
    }
}