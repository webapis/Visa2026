using Visa2026.Module.BusinessObjects;
using Xunit;

namespace Visa2026.Module.Tests.BusinessObjects;

public sealed class PersonIncompleteMarkOptionsTests
{
    [Fact]
    public void HasAtLeastOneMissingArea_FalseWhenAllClear()
    {
        var options = new PersonIncompleteMarkOptions();

        Assert.False(options.HasAtLeastOneMissingArea);
    }

    [Theory]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingPersonalData))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingPassport))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingCv))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingPhoto))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingEducation))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingMedical))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingAddress))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingFamilyDocs))]
    [InlineData(nameof(PersonIncompleteMarkOptions.MissingOther))]
    public void HasAtLeastOneMissingArea_TrueWhenAnyFlagSet(string propertyName)
    {
        var options = new PersonIncompleteMarkOptions();
        typeof(PersonIncompleteMarkOptions).GetProperty(propertyName)!.SetValue(options, true);

        Assert.True(options.HasAtLeastOneMissingArea);
    }

    [Fact]
    public void ApplyTo_SetsIncompleteFlagsNotesAndMarker()
    {
        var person = new Person();
        var before = DateTime.Now.AddSeconds(-2);
        var options = new PersonIncompleteMarkOptions
        {
            MissingPassport = true,
            MissingPhoto = true,
            Notes = "  missing scan and photo  ",
        };

        options.ApplyTo(person, "officer.a");

        Assert.True(person.IsDataIncomplete);
        Assert.True(person.IncompleteMissingPassport);
        Assert.True(person.IncompleteMissingPhoto);
        Assert.False(person.IncompleteMissingPersonalData);
        Assert.Equal("missing scan and photo", person.IncompleteNotes);
        Assert.Equal("officer.a", person.IncompleteMarkedBy);
        Assert.NotNull(person.IncompleteMarkedOn);
        Assert.InRange(person.IncompleteMarkedOn!.Value, before, DateTime.Now.AddSeconds(2));
        Assert.Equal("Passport, Photo", person.IncompleteMissingAreasDisplay);
    }

    [Fact]
    public void LoadFrom_RoundTripsFlagsAndNotes()
    {
        var person = new Person
        {
            IncompleteMissingAddress = true,
            IncompleteMissingOther = true,
            IncompleteNotes = "address unclear",
        };
        var options = new PersonIncompleteMarkOptions();

        options.LoadFrom(person);

        Assert.True(options.MissingAddress);
        Assert.True(options.MissingOther);
        Assert.False(options.MissingPassport);
        Assert.Equal("address unclear", options.Notes);
        Assert.True(options.HasAtLeastOneMissingArea);
    }
}
