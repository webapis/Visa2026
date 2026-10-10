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

    [Fact]
    public void Visa_key_uses_passport_and_trimmed_number()
    {
        var passportId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var lookup = Visa2014NaturalKeyLookup.ForVisa(passportId, "  V-99 ");

        Assert.NotNull(lookup);
        Assert.Equal($"visa|{passportId:D}|V-99", lookup!.CacheKey);
        Assert.Equal(Visa2014NaturalKeyKind.Visa, lookup.Kind);
        Assert.Equal(passportId, lookup.RelatedId);
    }

    [Fact]
    public void Visa_key_is_missing_when_passport_or_number_blank()
    {
        Assert.Null(Visa2014NaturalKeyLookup.ForVisa(Guid.Empty, "V-1"));
        Assert.Null(Visa2014NaturalKeyLookup.ForVisa(Guid.NewGuid(), "  "));
        Assert.Null(Visa2014NaturalKeyLookup.ForVisa(Guid.NewGuid(), null));
    }

    [Fact]
    public void EmployeePositionHistory_key_uses_calendar_day_from_DateTime_or_string()
    {
        var personId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var fromDateTime = Visa2014NaturalKeyLookup.ForEmployeePositionHistory(
            personId,
            new DateTime(2020, 1, 15, 18, 45, 0));
        Assert.Equal($"position|{personId:D}|2020-01-15", fromDateTime!.CacheKey);
        Assert.Equal(new DateTime(2020, 1, 15), fromDateTime.OnDate);

        var fromString = Visa2014NaturalKeyLookup.ForEmployeePositionHistory(personId, "2020-01-15T09:00:00");
        Assert.Equal($"position|{personId:D}|2020-01-15", fromString!.CacheKey);
    }

    [Fact]
    public void EmployeePositionHistory_key_is_missing_without_person_or_date()
    {
        Assert.Null(Visa2014NaturalKeyLookup.ForEmployeePositionHistory(Guid.Empty, "2020-01-15"));
        Assert.Null(Visa2014NaturalKeyLookup.ForEmployeePositionHistory(Guid.NewGuid(), "not-a-date"));
        Assert.Null(Visa2014NaturalKeyLookup.ForEmployeePositionHistory(Guid.NewGuid(), null));
    }

    [Fact]
    public void EmployeeSalary_key_is_person_only()
    {
        var personId = Guid.Parse("77777777-7777-7777-7777-777777777777");
        var lookup = Visa2014NaturalKeyLookup.ForEmployeeSalary(personId);

        Assert.Equal($"salary|{personId:D}", lookup!.CacheKey);
        Assert.Equal(Visa2014NaturalKeyKind.EmployeeSalary, lookup.Kind);
        Assert.Null(Visa2014NaturalKeyLookup.ForEmployeeSalary(Guid.Empty));
    }

    [Fact]
    public void Invitation_key_uses_number_and_calendar_day()
    {
        var lookup = Visa2014NaturalKeyLookup.ForInvitation(" INV-3 ", new DateTime(2021, 6, 1, 12, 0, 0));

        Assert.NotNull(lookup);
        Assert.Equal("invitation|INV-3|2021-06-01", lookup!.CacheKey);
        Assert.Equal(new DateTime(2021, 6, 1), lookup.OnDate);
        Assert.Null(Visa2014NaturalKeyLookup.ForInvitation("  ", "2021-06-01"));
        Assert.Null(Visa2014NaturalKeyLookup.ForInvitation("INV-3", "bad"));
    }

    [Fact]
    public void WorkPermitItem_key_requires_person_number_and_start_day()
    {
        var personId = Guid.Parse("88888888-8888-8888-8888-888888888888");
        var lookup = Visa2014NaturalKeyLookup.ForWorkPermitItem(personId, " WP-1 ", "2018-03-10T00:00:00");

        Assert.Equal($"workpermit-item|{personId:D}|WP-1|2018-03-10", lookup!.CacheKey);
        Assert.Equal(Visa2014NaturalKeyKind.WorkPermitItem, lookup.Kind);
        Assert.Null(Visa2014NaturalKeyLookup.ForWorkPermitItem(Guid.Empty, "WP-1", "2018-03-10"));
        Assert.Null(Visa2014NaturalKeyLookup.ForWorkPermitItem(personId, "  ", "2018-03-10"));
        Assert.Null(Visa2014NaturalKeyLookup.ForWorkPermitItem(personId, "WP-1", null));
    }

    [Fact]
    public void WorkPermit_key_is_missing_without_number_or_date()
    {
        Assert.Null(Visa2014NaturalKeyLookup.ForWorkPermit("  ", "2019-04-02"));
        Assert.Null(Visa2014NaturalKeyLookup.ForWorkPermit("WP-9", "not-a-date"));
    }

    [Fact]
    public void TryReadDate_AcceptsDateTimeAndRejectsOtherObjects()
    {
        Assert.True(Visa2014NaturalKeyLookup.TryReadDate(new DateTime(2019, 4, 2, 23, 59, 59), out var day));
        Assert.Equal(new DateTime(2019, 4, 2), day);
        Assert.False(Visa2014NaturalKeyLookup.TryReadDate(42, out _));
        Assert.False(Visa2014NaturalKeyLookup.TryReadDate(null, out _));
    }
}