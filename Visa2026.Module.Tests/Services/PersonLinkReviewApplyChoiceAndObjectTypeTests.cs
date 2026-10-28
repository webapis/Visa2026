using System;
using System.Collections.Generic;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class PersonLinkReviewApplyChoiceAndObjectTypeTests
{
    [Fact]
    public void ApplyChoice_UnknownOrNull_LeavesLineUnchanged()
    {
        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.Passport,
            Label = "Passport",
            Summary = "AA-1",
            ObjectId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Fields = new[] { new PersonLinkReviewField { Label = "No", Value = "AA-1" } },
            Choices = new[]
            {
                new PersonLinkReviewChoice
                {
                    ObjectId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Label = "BB-2",
                    Fields = new[] { new PersonLinkReviewField { Label = "No", Value = "BB-2" } },
                },
            },
        };

        PersonLinkReview.ApplyChoice(null!, Guid.NewGuid());
        PersonLinkReview.ApplyChoice(line, Guid.NewGuid());

        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), line.ObjectId);
        Assert.Equal("AA-1", line.Summary);
        Assert.Single(line.Fields);
        Assert.Equal("AA-1", line.Fields[0].Value);
    }

    [Fact]
    public void ApplyChoice_MatchingId_UpdatesSummaryAndFields()
    {
        var chosenId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.Education,
            Label = "Education",
            Summary = "—",
            ObjectId = Guid.Empty,
            Choices = new[]
            {
                new PersonLinkReviewChoice
                {
                    ObjectId = chosenId,
                    Label = "  ",
                    Fields = new[]
                    {
                        new PersonLinkReviewField { Label = "School", Value = "TMU", Required = true },
                    },
                },
            },
        };

        PersonLinkReview.ApplyChoice(line, chosenId);

        Assert.Equal(chosenId, line.ObjectId);
        Assert.Equal("—", line.Summary);
        Assert.Single(line.Fields);
        Assert.Equal("TMU", line.Fields[0].Value);
        Assert.True(line.Fields[0].Required);
    }

    [Theory]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Passport, typeof(Passport))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Visa, typeof(Visa))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Education, typeof(Education))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.AddressOfResidence, typeof(AddressOfResidence))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Position, typeof(EmployeePositionHistory))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.Salary, typeof(EmployeeSalary))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.MedicalRecord, typeof(MedicalRecord))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.InvitationItem, typeof(InvitationItem))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.WorkPermitItem, typeof(WorkPermitItem))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.BorderZoneItem, typeof(BorderZoneItem))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.RejectionItem, typeof(RejectionItem))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.TravelHistory, typeof(TravelHistory))]
    [InlineData(ApplicationProfileInstancePersonLinkKind.WorkDuty, typeof(WorkDuty))]
    public void ObjectType_MapsEveryConfiguredLinkKind(ApplicationProfileInstancePersonLinkKind kind, Type expected)
    {
        Assert.Equal(expected, PersonLinkReview.ObjectType(kind));
    }

    [Fact]
    public void ObjectType_UnknownKind_ReturnsNull()
    {
        Assert.Null(PersonLinkReview.ObjectType((ApplicationProfileInstancePersonLinkKind)999));
    }

    [Fact]
    public void Build_And_EnsureAddressToEdit_NullOrEmpty_ReturnEmpty()
    {
        Assert.Empty(PersonLinkReview.Build(null!, null!, null!));
        Assert.Equal(Guid.Empty, PersonLinkReview.EnsureAddressToEdit(null!, null!, null!));

        var person = new Person { ID = Guid.Empty };
        Assert.Equal(Guid.Empty, PersonLinkReview.EnsureAddressToEdit(null!, new ApplicationProfileInstance(), person));
    }

    [Fact]
    public void DescribeAddress_NullObjectSpaceOrEmptyId_ReturnsDash()
    {
        Assert.Equal("—", PersonLinkReview.DescribeAddress(null!, Guid.Empty));
        Assert.Equal("—", PersonLinkReview.DescribeAddress(null!, Guid.NewGuid()));
    }

    [Fact]
    public void SelectSaved_And_LinkSelected_EmptyIds_DoNotThrow()
    {
        var line = new PersonLinkReviewLine
        {
            Kind = ApplicationProfileInstancePersonLinkKind.Passport,
            Label = "Passport",
            Choices = Array.Empty<PersonLinkReviewChoice>(),
        };

        PersonLinkReview.SelectSaved(null!, null, new Person { ID = Guid.NewGuid() }, line, Guid.Empty);
        PersonLinkReview.LinkSelected(null!, new ApplicationProfileInstance(), new Person(), ApplicationProfileInstancePersonLinkKind.Passport, Guid.Empty);
        PersonLinkReview.LinkSelected(null!, new ApplicationProfileInstance(), new Person(), ApplicationProfileInstancePersonLinkKind.Passport, Guid.NewGuid());
    }
}
