using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Manual visa-family defaults, validation, display summary, and PDF line packing
/// (sanitize / migration covered in other coverage PRs).
/// </summary>
public sealed class VisaFamilyMemberLinesHelperValidateAndFormatTests
{
    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_SingleMaritalStatus_SetsYok()
    {
        var person = new Person
        {
            IsEmployee = true,
            MaritalStatus = new MaritalStatus { Code = "Sallah" },
            VisaApplicationFamilyMembersText = null
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
            MaritalStatus = new MaritalStatus { Code = "Oylenmez" },
            VisaApplicationFamilyMembersText = "   "
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(VisaFamilyMemberLinesHelper.NoneValue, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_ExistingManual_Unchanged()
    {
        const string manual = "Ayşe; 12.10.1989; aýaly; TUR";
        var person = new Person
        {
            IsEmployee = true,
            MaritalStatus = new MaritalStatus { LocalizationKey = "Married" },
            VisaApplicationFamilyMembersText = manual
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Equal(manual, person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void ApplyEmployeeDefaultIfEmpty_NonEmployee_NoOp()
    {
        var person = new Person
        {
            IsEmployee = false,
            VisaApplicationFamilyMembersText = null
        };

        VisaFamilyMemberLinesHelper.ApplyEmployeeDefaultIfEmpty(person);

        Assert.Null(person.VisaApplicationFamilyMembersText);
    }

    [Fact]
    public void IsSingleMaritalStatus_MatchesCodeLocalizationOrNameTm()
    {
        Assert.True(VisaFamilyMemberLinesHelper.IsSingleMaritalStatus(
            new MaritalStatus { LocalizationKey = "Single" }));
        Assert.True(VisaFamilyMemberLinesHelper.IsSingleMaritalStatus(
            new MaritalStatus { NameTm = "Sallah" }));
        Assert.False(VisaFamilyMemberLinesHelper.IsSingleMaritalStatus(null));
        Assert.False(VisaFamilyMemberLinesHelper.IsSingleMaritalStatus(
            new MaritalStatus { Code = "Married" }));
    }

    [Fact]
    public void TryValidate_EmptyLines_Succeeds()
    {
        Assert.True(VisaFamilyMemberLinesHelper.TryValidate(Array.Empty<VisaFamilyMemberLineDto>(), out var error));
        Assert.Null(error);
    }

    [Fact]
    public void TryValidate_MissingFields_ReportsFirstLineError()
    {
        var rows = new List<VisaFamilyMemberLineDto>
        {
            new()
            {
                FullName = " ",
                BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                RelationshipNameTm = "aýaly",
                CountryCode = "TUR"
            }
        };

        Assert.False(VisaFamilyMemberLinesHelper.TryValidate(rows, out var error));
        Assert.Equal("Line 1: full name is required.", error);
    }

    [Fact]
    public void TryValidate_CompleteRow_Succeeds()
    {
        var rows = new List<VisaFamilyMemberLineDto>
        {
            new()
            {
                FullName = "Spouse Person",
                BirthDate = new DateTime(1989, 10, 12, 0, 0, 0, DateTimeKind.Unspecified),
                RelationshipNameTm = "aýaly",
                CountryCode = "TUR"
            }
        };

        Assert.True(VisaFamilyMemberLinesHelper.TryValidate(rows, out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Format_RoundTripsParseStorageLines()
    {
        var rows = new[]
        {
            new VisaFamilyMemberLineDto
            {
                FullName = "Child One",
                BirthDate = new DateTime(2010, 1, 1, 0, 0, 0, DateTimeKind.Unspecified),
                RelationshipNameTm = "gyzy",
                CountryCode = "TUR"
            }
        };

        var formatted = VisaFamilyMemberLinesHelper.Format(rows);
        var parsed = VisaFamilyMemberLinesHelper.Parse(formatted);

        Assert.Single(parsed);
        Assert.Equal("Child One", parsed[0].FullName);
        Assert.Equal(new DateTime(2010, 1, 1), parsed[0].BirthDate);
        Assert.Equal("gyzy", parsed[0].RelationshipNameTm);
        Assert.Equal("TUR", parsed[0].CountryCode);
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
            "Child One; 01.01.2010; gyzy; TUR" + "\n" +
            "Spouse Person; 12.10.1989; aýaly; TUR";

        var summary = VisaFamilyMemberLinesHelper.FormatDisplaySummary(
            text,
            emptyMessage: "empty",
            memberCountFormat: "{0} members");

        Assert.StartsWith("2 members — ", summary, StringComparison.Ordinal);
        Assert.Contains("Child One", summary, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("aýaly", true)]
    [InlineData("adamsy", true)]
    [InlineData("eri", true)]
    [InlineData("SPOUSE", true)]
    [InlineData("gyzy", false)]
    [InlineData(" ", false)]
    public void IsSpouseRelationshipNameTm_WithoutObjectSpace_UsesNamePatterns(string name, bool expected)
    {
        Assert.Equal(expected, VisaFamilyMemberLinesHelper.IsSpouseRelationshipNameTm(name, objectSpace: null));
    }

    [Fact]
    public void SplitVisaPdfMaritalFamilyLines_PacksFourMembersAcrossThreeLines()
    {
        var rows = new List<VisaFamilyMemberLineDto>();
        for (var i = 1; i <= 4; i++)
        {
            rows.Add(new VisaFamilyMemberLineDto
            {
                FullName = $"Person{i}",
                BirthDate = new DateTime(2000, 1, i, 0, 0, 0, DateTimeKind.Unspecified),
                RelationshipNameTm = i == 1 ? "aýaly" : "gyzy",
                CountryCode = "TUR"
            });
        }

        var (line1, line2, line3) = VisaFamilyMemberLinesHelper.SplitVisaPdfMaritalFamilyLines(rows);

        Assert.NotNull(line1);
        Assert.NotNull(line2);
        Assert.Contains("Person1", line1!, StringComparison.Ordinal);
        Assert.Contains("Person2", line1!, StringComparison.Ordinal);
        Assert.Contains("Person3", line2!, StringComparison.Ordinal);
        Assert.Contains("Person4", line2!, StringComparison.Ordinal);
        Assert.Null(line3);
    }
}
