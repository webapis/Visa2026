using Visa2026.DataImporter.Legacy.Visa2014;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014.Tests;

public class Visa2014ResidenceClassifierTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_lines_are_not_hotel_hospital_or_lodging(string? line)
    {
        Assert.False(Visa2014ResidenceClassifier.IsHotelAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsHospitalAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsLodgingSiteLine(line));
        Assert.Equal("Other", Visa2014ResidenceClassifier.MapLojmanResidenceType(line));
        Assert.Equal("PrivateHouse", Visa2014ResidenceClassifier.MapPatentResidenceType(line));
    }

    [Theory]
    [InlineData("Aşgabat myhmanhanasy")]
    [InlineData("Otel Ýyldyz")]
    [InlineData("Grand oteli")]
    [InlineData("Şäher otely")]
    public void Hotel_patterns_win_over_hospital_and_lodging(string line)
    {
        Assert.True(Visa2014ResidenceClassifier.IsHotelAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsHospitalAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsLodgingSiteLine(line));
        Assert.Equal("Hotel", Visa2014ResidenceClassifier.MapLojmanResidenceType(line));
        Assert.Equal("Hotel", Visa2014ResidenceClassifier.MapPatentResidenceType(line));
    }

    [Fact]
    public void English_hotel_substring_is_not_treated_as_Turkmen_otel()
    {
        Assert.False(Visa2014ResidenceClassifier.IsHotelAddressLine("International hotel room 12"));
    }

    [Theory]
    [InlineData("Hassahana №3")]
    [InlineData("Ýokanç keseller hassahanasy")]
    [InlineData("Içki kesel klinikasi")]
    public void Hospital_patterns_are_detected(string line)
    {
        Assert.True(Visa2014ResidenceClassifier.IsHospitalAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsHotelAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsLodgingSiteLine(line));
        Assert.Equal("Hospital", Visa2014ResidenceClassifier.MapLojmanResidenceType(line));
        Assert.Equal("Hospital", Visa2014ResidenceClassifier.MapPatentResidenceType(line));
    }

    [Fact]
    public void Myhmanhan_never_counts_as_hospital_even_with_hassahan_nearby()
    {
        Assert.False(Visa2014ResidenceClassifier.IsHospitalAddressLine("Myhmanhana / hassahana kampusy"));
        Assert.True(Visa2014ResidenceClassifier.IsHotelAddressLine("Myhmanhana / hassahana kampusy"));
    }

    [Theory]
    [InlineData("UÝJ-12")]
    [InlineData("işçilerşäherçesi")]
    [InlineData("Şirket lojmany")]
    [InlineData("Ýaşaýyş jaýy")]
    public void Lodging_patterns_are_detected_when_not_hotel_or_hospital(string line)
    {
        Assert.True(Visa2014ResidenceClassifier.IsLodgingSiteLine(line));
        Assert.Equal("Lodging", Visa2014ResidenceClassifier.MapLojmanResidenceType(line));
        Assert.Equal("Lodging", Visa2014ResidenceClassifier.MapPatentResidenceType(line));
    }

    [Fact]
    public void Unclassified_lines_map_to_Other_or_PrivateHouse()
    {
        const string line = "Görogly köçesi 15";

        Assert.False(Visa2014ResidenceClassifier.IsHotelAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsHospitalAddressLine(line));
        Assert.False(Visa2014ResidenceClassifier.IsLodgingSiteLine(line));
        Assert.Equal("Other", Visa2014ResidenceClassifier.MapLojmanResidenceType(line));
        Assert.Equal("PrivateHouse", Visa2014ResidenceClassifier.MapPatentResidenceType(line));
    }
}
