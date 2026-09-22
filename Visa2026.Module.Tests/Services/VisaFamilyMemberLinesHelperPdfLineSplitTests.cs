using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Packing of manual visa-family rows into XFA item-18 lines (_181–_183).
/// Regression guard after manual-only family outputs (no FamilyMembers fallback).
/// </summary>
public sealed class VisaFamilyMemberLinesHelperPdfLineSplitTests
{
    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_Empty_ReturnsAllNull()
    {
        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(
            Array.Empty<VisaFamilyMemberLineDto>());

        Assert.Null(line1);
        Assert.Null(line2);
        Assert.Null(line3);
    }

    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_Null_ReturnsAllNull()
    {
        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(null);

        Assert.Null(line1);
        Assert.Null(line2);
        Assert.Null(line3);
    }

    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_OneMember_OnlyLine1Populated_WithCountrySuffix()
    {
        var rows = new[]
        {
            Row("Ayşe Yılmaz", "aýaly", "12.10.1989", "TUR"),
        };

        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(rows);

        Assert.Equal("AÝALY Ayşe Yılmaz 12.10.1989 TUR.", line1);
        Assert.Null(line2);
        Assert.Null(line3);
    }

    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_ThreeMembers_OnePerLine_CountryOnlyOnLast()
    {
        var rows = new[]
        {
            Row("Spouse Person", "aýaly", "12.10.1989", "TUR"),
            Row("Child One", "ogyly", "01.01.2010", "TUR"),
            Row("Child Two", "gyzy", "02.02.2012", "TUR"),
        };

        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(rows);

        Assert.Equal("AÝALY Spouse Person 12.10.1989", line1);
        Assert.Equal("OGYLY Child One 01.01.2010", line2);
        Assert.Equal("GYZY Child Two 02.02.2012 TUR.", line3);
        Assert.DoesNotContain("TUR", line1!);
        Assert.DoesNotContain("TUR", line2!);
    }

    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_FourMembers_PacksEvenlyAcrossThreeLines()
    {
        // chunkSize = ceil(4/3) = 2 → lines get 2, 2, 0 segments (third null)
        var rows = new[]
        {
            Row("A", "aýaly", "01.01.1980", "TUR"),
            Row("B", "ogyly", "01.01.2001", "TUR"),
            Row("C", "gyzy", "01.01.2003", "TUR"),
            Row("D", "ogyly", "01.01.2005", "TUR"),
        };

        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(rows);

        Assert.Contains("AÝALY A 01.01.1980", line1!);
        Assert.Contains("OGYLY B 01.01.2001", line1!);
        Assert.Contains(", ", line1!);
        Assert.Contains("GYZY C 01.01.2003", line2!);
        Assert.Contains("OGYLY D 01.01.2005 TUR.", line2!);
        Assert.Null(line3);
    }

    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_SixMembers_TwoPerLine()
    {
        var rows = new[]
        {
            Row("A", "aýaly", "01.01.1980", "TUR"),
            Row("B", "ogyly", "01.01.2001", "TUR"),
            Row("C", "gyzy", "01.01.2003", "TUR"),
            Row("D", "ogyly", "01.01.2005", "TUR"),
            Row("E", "gyzy", "01.01.2007", "TUR"),
            Row("F", "ogyly", "01.01.2009", "TUR"),
        };

        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(rows);

        Assert.NotNull(line1);
        Assert.NotNull(line2);
        Assert.NotNull(line3);
        Assert.Contains("AÝALY A", line1!);
        Assert.Contains("OGYLY B", line1!);
        Assert.Contains("GYZY C", line2!);
        Assert.Contains("OGYLY D", line2!);
        Assert.Contains("GYZY E", line3!);
        Assert.Contains("OGYLY F 01.01.2009 TUR.", line3!);
    }

    [Fact]
    public void FormatDisplaySummary_Yok_ReturnsNoneValue()
    {
        var summary = VisaFamilyMemberLinesHelper.FormatDisplaySummary(
            VisaFamilyMemberLinesHelper.NoneValue,
            emptyMessage: "empty",
            memberCountFormat: "{0} members");

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, summary);
    }

    [Fact]
    public void FormatDisplaySummary_MultipleRows_PrefixesCount()
    {
        const string text =
            "Spouse Person; 12.10.1989; aýaly; TUR\n" +
            "Child One; 01.01.2010; gyzy; TUR";

        var summary = VisaFamilyMemberLinesHelper.FormatDisplaySummary(
            text,
            emptyMessage: "empty",
            memberCountFormat: "{0} members");

        Assert.StartsWith("2 members — ", summary);
        Assert.Contains("Spouse Person", summary);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_SingleMaritalStatus_ForcesYok()
    {
        var person = new Person
        {
            IsEmployee = true,
            MaritalStatus = new MaritalStatus { Code = "Sallah", LocalizationKey = "Single" },
            VisaApplicationFamilyMembersText = "should-be-cleared; 01.01.2000; gyzy; TUR",
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_BlankNonSingle_SetsYok()
    {
        var person = new Person
        {
            IsEmployee = true,
            MaritalStatus = new MaritalStatus { Code = "Öýli", LocalizationKey = "Married" },
            VisaApplicationFamilyMembersText = "  ",
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_NonEmployee_NoOp()
    {
        var person = new Person
        {
            IsEmployee = false,
            VisaApplicationFamilyMembersText = null,
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Null(person.VisaApplicationFamilyMembersText);
    }

    private static VisaFamilyMemberLineDto Row(
        string fullName,
        string relationshipNameTm,
        string birthDateDdMmYyyy,
        string countryCode)
    {
        var parts = birthDateDdMmYyyy.Split('.');
        var birth = new DateTime(
            int.Parse(parts[2]),
            int.Parse(parts[1]),
            int.Parse(parts[0]));

        return new VisaFamilyMemberLineDto
        {
            FullName = fullName,
            RelationshipNameTm = relationshipNameTm,
            BirthDate = birth,
            CountryCode = countryCode,
        };
    }
}
