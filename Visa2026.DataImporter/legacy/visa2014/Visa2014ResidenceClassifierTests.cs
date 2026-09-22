using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014ResidenceClassifierTests
{
    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("Ashgabat myhmanhanasy", true)]
    [InlineData("Otel Yyldyz", true)]
    [InlineData("oteli merkezi", true)]
    [InlineData("otely golaýynda", true)]
    [InlineData("English hotel stay", false)] // "otel" inside "hotel" must not match
    [InlineData("hassahana №3", false)] // hospital wins over hotel
    [InlineData("yokanc keseller hassahanasy", false)]
    public void IsHotelAddressLine_ClassifiesHotelMarkers(string? line, bool expected)
    {
        Assert.Equal(expected, Visa2014ResidenceClassifier.IsHotelAddressLine(line));
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("hassahana №3", true)]
    [InlineData("yokanc keseller merkezi", true)]
    [InlineData("içki kesel bölümi", true)]
    [InlineData("icki kesel", true)]
    [InlineData("myhmanhana + hassahan", false)] // myhmanhan blocks hospital
    [InlineData("Ashgabat myhmanhanasy", false)]
    public void IsHospitalAddressLine_ClassifiesHospitalMarkers(string? line, bool expected)
    {
        Assert.Equal(expected, Visa2014ResidenceClassifier.IsHospitalAddressLine(line));
    }

    [Theory]
    [InlineData("işçiler saherce uyji", true)]
    [InlineData("iscilersaherce A", true)]
    [InlineData("lojman 12", true)]
    [InlineData("ýaşaýyş jaýy", true)]
    [InlineData("Ashgabat myhmanhanasy", false)]
    [InlineData("hassahana №3", false)]
    [InlineData("private house street 1", false)]
    public void IsLodgingSiteLine_RequiresLodgingMarkersAndNotHotelHospital(string? line, bool expected)
    {
        Assert.Equal(expected, Visa2014ResidenceClassifier.IsLodgingSiteLine(line));
    }

    [Theory]
    [InlineData("Ashgabat myhmanhanasy", "Hotel")]
    [InlineData("hassahana №3", "Hospital")]
    [InlineData("lojman 12", "Lodging")]
    [InlineData("random street", "Other")]
    public void MapLojmanResidenceType_MapsBuckets(string? line, string expected)
    {
        Assert.Equal(expected, Visa2014ResidenceClassifier.MapLojmanResidenceType(line));
    }

    [Theory]
    [InlineData("Ashgabat myhmanhanasy", "Hotel")]
    [InlineData("hassahana №3", "Hospital")]
    [InlineData("lojman 12", "Lodging")]
    [InlineData("random street", "PrivateHouse")]
    [InlineData(null, "PrivateHouse")]
    public void MapPatentResidenceType_DefaultsPrivateHouse(string? line, string expected)
    {
        Assert.Equal(expected, Visa2014ResidenceClassifier.MapPatentResidenceType(line));
    }
}
