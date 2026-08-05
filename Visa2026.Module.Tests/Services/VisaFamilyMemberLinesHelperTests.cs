using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public sealed class VisaFamilyMemberLinesHelperTests
{
    [Fact]
    public void IsNoneValue_RecognizesSentinelCaseInsensitively()
    {
        Assert.True(VisaFamilyMemberLinesHelper.IsNoneValue(VisaFamilyMemberLinesHelper.NoneValue));
        Assert.True(VisaFamilyMemberLinesHelper.IsNoneValue("  ýok  "));
        Assert.False(VisaFamilyMemberLinesHelper.IsNoneValue("aýaly"));
        Assert.False(VisaFamilyMemberLinesHelper.IsNoneValue(null));
    }

    [Theory]
    [InlineData("Sallah", "Single", "Other", true)]
    [InlineData("OTHER", "Single", "Other", true)]
    [InlineData("OTHER", "Married", "Sallah", true)]
    [InlineData("OTHER", "Married", "Öýlenen", false)]
    public void IsSingleMaritalStatus_MatchesCodeLocalizationOrNameTm(
        string code,
        string localizationKey,
        string nameTm,
        bool expected)
    {
        var status = new MaritalStatus
        {
            Code = code,
            LocalizationKey = localizationKey,
            NameTm = nameTm,
        };

        Assert.Equal(expected, VisaFamilyMemberLinesHelper.IsSingleMaritalStatus(status));
        Assert.False(VisaFamilyMemberLinesHelper.IsSingleMaritalStatus(null));
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_SingleEmployee_ForcesNone()
    {
        var person = new Person
        {
            IsEmployee = true,
            MaritalStatus = new MaritalStatus { Code = "Sallah" },
            VisaApplicationFamilyMembersText = "should-be-cleared",
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_MarriedEmployeeBlank_SetsNone()
    {
        var person = new Person
        {
            IsEmployee = true,
            MaritalStatus = new MaritalStatus { Code = "Married", LocalizationKey = "Married", NameTm = "Öýlenen" },
            VisaApplicationFamilyMembersText = "  ",
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_NonEmployee_DoesNothing()
    {
        var person = new Person
        {
            IsEmployee = false,
            VisaApplicationFamilyMembersText = null,
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Null(person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void Parse_And_Format_RoundTripCanonicalLines()
    {
        const string text =
            "Esra Aksoy; 12.10.1989; aýaly; TUR\nYusuf Mete Aksoy; 06.12.2012; ogly; TUR";

        var rows = VisaFamilyMemberLinesHelper.Parse(text);
        Assert.Equal(2, rows.Count);
        Assert.False(rows[0].IsLegacyIncomplete);
        Assert.Equal(new DateTime(1989, 10, 12), rows[0].BirthDate);

        var formatted = VisaFamilyMemberLinesHelper.Format(rows);
        var again = VisaFamilyMemberLinesHelper.Parse(formatted);

        Assert.Equal(2, again.Count);
        Assert.Equal("Esra Aksoy", again[0].FullName);
        Assert.Equal("ogly", again[1].RelationshipNameTm);
        Assert.Equal("TUR", again[1].CountryCode);
    }

    [Fact]
    public void Parse_NoneOrBlank_ReturnsEmpty()
    {
        Assert.Empty(VisaFamilyMemberLinesHelper.Parse(VisaFamilyMemberLinesHelper.NoneValue));
        Assert.Empty(VisaFamilyMemberLinesHelper.Parse(null));
        Assert.Empty(VisaFamilyMemberLinesHelper.Parse("   "));
    }

    [Fact]
    public void Parse_PartialLine_MarksLegacyIncomplete()
    {
        var rows = VisaFamilyMemberLinesHelper.Parse("Only Name; 01.01.2000");
        Assert.Single(rows);
        Assert.True(rows[0].IsLegacyIncomplete);
        Assert.Equal("Only Name", rows[0].FullName);
        Assert.Equal(new DateTime(2000, 1, 1), rows[0].BirthDate);
    }

    [Fact]
    public void TryValidate_RequiresAllFieldsPerLine()
    {
        Assert.True(VisaFamilyMemberLinesHelper.TryValidate(Array.Empty<VisaFamilyMemberLineDto>(), out _));

        var incomplete = new[]
        {
            new VisaFamilyMemberLineDto
            {
                FullName = "A",
                BirthDate = new DateTime(2001, 2, 3),
                RelationshipNameTm = "ogy",
                CountryCode = "",
            },
        };
        Assert.False(VisaFamilyMemberLinesHelper.TryValidate(incomplete, out var error));
        Assert.Contains("country of residence", error, StringComparison.OrdinalIgnoreCase);

        var complete = new[]
        {
            new VisaFamilyMemberLineDto
            {
                FullName = "A",
                BirthDate = new DateTime(2001, 2, 3),
                RelationshipNameTm = "ogy",
                CountryCode = "TUR",
            },
        };
        Assert.True(VisaFamilyMemberLinesHelper.TryValidate(complete, out error));
        Assert.Null(error);
    }

    [Fact]
    public void FormatForVisaPdfMaritalFamilyBlock_PutsCountryOnLastSegmentOnly()
    {
        var text =
            "Esra Aksoy; 12.10.1989; aýaly; TUR" + Environment.NewLine +
            "Yusuf Mete Aksoy; 06.12.2012; ogly; TUR";

        var block = VisaFamilyMemberLinesHelper.FormatForVisaPdfMaritalFamilyBlock(text);

        Assert.NotNull(block);
        Assert.Contains("AÝALY Esra Aksoy 12.10.1989,", block);
        Assert.Contains("OGLY Yusuf Mete Aksoy 06.12.2012 TUR.", block);
        Assert.DoesNotContain("12.10.1989 TUR", block);
    }

    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_PacksEvenlyAcrossThreeFields()
    {
        var rows = Enumerable.Range(1, 4)
            .Select(i => new VisaFamilyMemberLineDto
            {
                FullName = $"Person{i}",
                BirthDate = new DateTime(2000, 1, i),
                RelationshipNameTm = "ogly",
                CountryCode = "TUR",
            })
            .ToList();

        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(rows);

        Assert.NotNull(line1);
        Assert.NotNull(line2);
        Assert.Contains("Person1", line1);
        Assert.Contains("Person2", line1); // chunk size ceil(4/3)=2
        Assert.Contains("Person3", line2);
        Assert.Contains("Person4", line2);
        Assert.Null(line3);
    }

    [Fact]
    public void FormatSahsyKagyzFamilyStatus_BuildsLowercaseSegments()
    {
        const string text = "Esra Aksoy; 12.10.1989; Aýaly; TUR";
        var formatted = VisaFamilyMemberLinesHelper.FormatSahsyKagyzFamilyStatus(text);

        Assert.Equal("aýaly-Esra Aksoy 12.10.1989ý. TUR.", formatted);
    }

    [Fact]
    public void FormatDisplaySummary_NoneEmptyAndMultiMember()
    {
        Assert.Equal(
            VisaFamilyMemberLinesHelper.NoneValue,
            VisaFamilyMemberLinesHelper.FormatDisplaySummary(
                VisaFamilyMemberLinesHelper.NoneValue,
                "empty",
                "{0} members"));

        Assert.Equal(
            "empty",
            VisaFamilyMemberLinesHelper.FormatDisplaySummary(null, "empty", "{0} members"));

        const string one = "Esra Aksoy; 12.10.1989; aýaly; TUR";
        Assert.Equal(
            "Esra Aksoy; 12.10.1989; aýaly; TUR",
            VisaFamilyMemberLinesHelper.FormatDisplaySummary(one, "empty", "{0} members"));

        var two =
            "Esra Aksoy; 12.10.1989; aýaly; TUR" + Environment.NewLine +
            "Yusuf; 06.12.2012; ogly; TUR";
        var summary = VisaFamilyMemberLinesHelper.FormatDisplaySummary(two, "empty", "{0} members");
        Assert.StartsWith("2 members — ", summary, StringComparison.Ordinal);
        Assert.Contains("Esra Aksoy", summary);
    }

    [Fact]
    public void MergeRelationshipAndCountryOptions_AddsMissingRowValues()
    {
        var baseRelationships = new List<RelationshipLookupItem>
        {
            new() { Oid = Guid.NewGuid(), NameTm = "aýaly" },
        };
        var row = new VisaFamilyMemberLineDto
        {
            RelationshipNameTm = "ogy",
            RelationshipOid = Guid.NewGuid(),
            CountryCode = "CAN",
            CountryOid = Guid.NewGuid(),
        };

        var mergedRel = VisaFamilyMemberLinesHelper.MergeRelationshipOptionsForRow(baseRelationships, row);
        Assert.Equal(2, mergedRel.Count);
        Assert.Contains(mergedRel, r => r.NameTm == "ogy");

        var baseCountries = new List<CountryLookupItem>
        {
            new() { Oid = Guid.NewGuid(), Code = "TUR", NameTm = "Türkmenistan" },
        };
        var mergedCountries = VisaFamilyMemberLinesHelper.MergeCountryOptionsForRow(baseCountries, row);
        Assert.Equal(2, mergedCountries.Count);
        Assert.Contains(mergedCountries, c => c.Code == "CAN");

        // Already present → same list instance semantics (no duplicate)
        row.RelationshipNameTm = "aýaly";
        var unchanged = VisaFamilyMemberLinesHelper.MergeRelationshipOptionsForRow(baseRelationships, row);
        Assert.Same(baseRelationships, unchanged);
    }

    [Fact]
    public void Format_NullOrIncompleteRows_ReturnsNull()
    {
        Assert.Null(VisaFamilyMemberLinesHelper.Format(null));
        Assert.Null(VisaFamilyMemberLinesHelper.Format(Array.Empty<VisaFamilyMemberLineDto>()));
        Assert.Null(VisaFamilyMemberLinesHelper.Format(new[]
        {
            new VisaFamilyMemberLineDto { FullName = "NoDate" },
        }));
    }

    [Fact]
    public void FormatForVisaPdfAggregate_None_ReturnsNull()
    {
        Assert.Null(VisaFamilyMemberLinesHelper.FormatForVisaPdfAggregate(VisaFamilyMemberLinesHelper.NoneValue));
        Assert.Null(VisaFamilyMemberLinesHelper.FormatForVisaPdfMaritalFamilyBlock(VisaFamilyMemberLinesHelper.NoneValue));
        Assert.Null(VisaFamilyMemberLinesHelper.FormatSahsyKagyzFamilyStatus(VisaFamilyMemberLinesHelper.NoneValue));
    }
}
