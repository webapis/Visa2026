using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ApplicationPersonLink;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfileInstancePersonLinkPassportGateTests
{
    private static readonly DateTime Today = DateTime.Today;

    [Fact]
    public void Blocks_WhenPersonHasNoPassport()
    {
        var person = new Person();

        Assert.True(ApplicationProfileInstancePersonLinkPassportGate.TryGetBlockReason(person, out var reason));
        Assert.Equal(ApplicationProfileInstancePersonLinkPassportGate.NoPassport, reason);
    }

    [Fact]
    public void Blocks_WhenPassportIsCancelled()
    {
        var person = PersonWithPassport(Today.AddYears(1), cancelled: true);

        Assert.True(ApplicationProfileInstancePersonLinkPassportGate.TryGetBlockReason(person, out var reason));
        Assert.Equal(ApplicationProfileInstancePersonLinkPassportGate.PassportCancelled, reason);
    }

    [Fact]
    public void Blocks_WhenOnlyPassportIsExpired()
    {
        var person = PersonWithPassport(Today.AddDays(-1));

        Assert.True(ApplicationProfileInstancePersonLinkPassportGate.TryGetBlockReason(person, out var reason));
        Assert.Equal(ApplicationProfileInstancePersonLinkPassportGate.PassportExpired, reason);
    }

    [Fact]
    public void Allows_WhenCurrentPassportIsValidEvenIfPreviousExpired()
    {
        var person = new Person();
        person.Passports.Add(new Passport
        {
            Person = person,
            PassportNumber = "OLD-1",
            IssueDate = Today.AddYears(-3),
            ExpirationDate = Today.AddDays(-10),
        });
        person.Passports.Add(new Passport
        {
            Person = person,
            PassportNumber = "NEW-1",
            IssueDate = Today.AddMonths(-1),
            ExpirationDate = Today.AddYears(2),
        });

        Assert.False(ApplicationProfileInstancePersonLinkPassportGate.TryGetBlockReason(person, out var reason));
        Assert.Equal(string.Empty, reason);
    }

    [Fact]
    public void Allows_WhenPassportExpiresToday()
    {
        var person = PersonWithPassport(Today);

        Assert.False(ApplicationProfileInstancePersonLinkPassportGate.TryGetBlockReason(person, out _));
    }

    private static Person PersonWithPassport(DateTime expiration, bool cancelled = false)
    {
        var person = new Person();
        person.Passports.Add(new Passport
        {
            Person = person,
            PassportNumber = "AA123",
            IssueDate = Today.AddYears(-2),
            ExpirationDate = expiration,
            IsCancelled = cancelled,
        });
        return person;
    }
}