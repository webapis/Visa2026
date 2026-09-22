using System;
using System.Collections.ObjectModel;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

/// <summary>
/// Resminamalar catalog filtering: inactive templates, type/group union, project contracts, criteria.
/// </summary>
public sealed class UserReportVisibilityServiceTests
{
    private readonly UserReportVisibilityService _sut = new();

    [Fact]
    public void IsTemplateVisible_Inactive_ReturnsFalse()
    {
        var typeId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var template = ActiveTemplate();
        template.IsActive = false;
        var application = ApplicationWithType(typeId, "Check-in");

        Assert.False(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_NoFilters_ReturnsTrueForAnyApplication()
    {
        var template = ActiveTemplate();
        var application = ApplicationWithType(Guid.NewGuid(), "Any");

        Assert.True(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_TypeLinkMatch_ReturnsTrue()
    {
        var typeId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var template = ActiveTemplate();
        template.ApplicableTypeLinks.Add(new UserReportTemplateApplicationType
        {
            ApplicationTypeId = typeId
        });
        var application = ApplicationWithType(typeId, "Check-in");

        Assert.True(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_TypeLinkMismatch_ReturnsFalse()
    {
        var template = ActiveTemplate();
        template.ApplicableTypeLinks.Add(new UserReportTemplateApplicationType
        {
            ApplicationTypeId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        });
        var application = ApplicationWithType(Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), "Check-out");

        Assert.False(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_GroupMemberMatch_ReturnsTrueEvenWithoutTypeLink()
    {
        var typeId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var group = new ApplicationTypeGroup
        {
            Members = new ObservableCollection<ApplicationTypeGroupMember>
            {
                new() { ApplicationTypeId = typeId }
            }
        };
        var template = ActiveTemplate();
        template.ApplicableGroupLinks.Add(new UserReportTemplateApplicationTypeGroup
        {
            ApplicationTypeGroupId = Guid.NewGuid(),
            ApplicationTypeGroup = group
        });
        var application = ApplicationWithType(typeId, "Registration check-in");

        Assert.True(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_TypeOrGroupUnion_MatchesEitherAxis()
    {
        var linkedTypeId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var groupMemberTypeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var group = new ApplicationTypeGroup
        {
            Members = new ObservableCollection<ApplicationTypeGroupMember>
            {
                new() { ApplicationTypeId = groupMemberTypeId }
            }
        };
        var template = ActiveTemplate();
        template.ApplicableTypeLinks.Add(new UserReportTemplateApplicationType
        {
            ApplicationTypeId = linkedTypeId
        });
        template.ApplicableGroupLinks.Add(new UserReportTemplateApplicationTypeGroup
        {
            ApplicationTypeGroupId = Guid.NewGuid(),
            ApplicationTypeGroup = group
        });

        Assert.True(_sut.IsTemplateVisible(template, ApplicationWithType(linkedTypeId, "Direct")));
        Assert.True(_sut.IsTemplateVisible(template, ApplicationWithType(groupMemberTypeId, "Via group")));
        Assert.False(_sut.IsTemplateVisible(
            template,
            ApplicationWithType(Guid.Parse("33333333-3333-3333-3333-333333333333"), "Other")));
    }

    [Fact]
    public void IsTemplateVisible_TypeFilterWithNullApplicationType_ReturnsFalse()
    {
        var template = ActiveTemplate();
        template.ApplicableTypeLinks.Add(new UserReportTemplateApplicationType
        {
            ApplicationTypeId = Guid.NewGuid()
        });
        var application = new Application();

        Assert.False(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_ProjectContractMismatch_ReturnsFalse()
    {
        var contractId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var otherId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var template = ActiveTemplate();
        template.ApplicableProjectContractLinks.Add(new UserReportTemplateProjectContract
        {
            ProjectContractId = contractId
        });
        var application = ApplicationWithType(Guid.NewGuid(), "Any");
        application.ProjectContract = new ProjectContract { ID = otherId };

        Assert.False(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_ProjectContractMatch_ReturnsTrue()
    {
        var contractId = Guid.Parse("66666666-6666-6666-6666-666666666666");
        var template = ActiveTemplate();
        template.ApplicableProjectContractLinks.Add(new UserReportTemplateProjectContract
        {
            ProjectContractId = contractId
        });
        var application = ApplicationWithType(Guid.NewGuid(), "Any");
        application.ProjectContract = new ProjectContract { ID = contractId };

        Assert.True(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_VisibilityCriteriaPass_ReturnsTrue()
    {
        var template = ActiveTemplate();
        template.VisibilityCriteria = "ApplicationNumber = '1105'";
        var application = ApplicationWithType(Guid.NewGuid(), "Any");
        application.ApplicationNumber = "1105";

        Assert.True(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_VisibilityCriteriaFail_ReturnsFalse()
    {
        var template = ActiveTemplate();
        template.VisibilityCriteria = "ApplicationNumber = '1105'";
        var application = ApplicationWithType(Guid.NewGuid(), "Any");
        application.ApplicationNumber = "other";

        Assert.False(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_InvalidVisibilityCriteria_ReturnsFalse()
    {
        var template = ActiveTemplate();
        template.VisibilityCriteria = "This Is Not Valid Criteria [[[";
        var application = ApplicationWithType(Guid.NewGuid(), "Any");

        Assert.False(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_ApplicationItemRoot_AnyChildMatch_ReturnsTrue()
    {
        var template = ActiveTemplate();
        template.RootBoType = UserReportBoType.ApplicationItem;
        template.VisibilityCriteria = "TravelNotes = 'keep'";
        var match = new ApplicationItem { TravelNotes = "keep" };
        var other = new ApplicationItem { TravelNotes = "skip" };
        var application = ApplicationWithType(Guid.NewGuid(), "Any");
        application.ApplicationItems = new ObservableCollection<ApplicationItem> { other, match };

        Assert.True(_sut.IsTemplateVisible(template, application));
    }

    [Fact]
    public void IsTemplateVisible_TypeMatchByName_CaseInsensitive()
    {
        var typeId = Guid.NewGuid();
        var template = ActiveTemplate();
        template.ApplicableTypeLinks.Add(new UserReportTemplateApplicationType
        {
            ApplicationTypeId = Guid.Empty,
            ApplicationType = new ApplicationType { ID = Guid.NewGuid(), Name = "check-in" }
        });
        var application = ApplicationWithType(typeId, "Check-In");

        Assert.True(_sut.IsTemplateVisible(template, application));
    }

    private static UserReportTemplate ActiveTemplate() =>
        new()
        {
            IsActive = true,
            ApplicableTypeLinks = new ObservableCollection<UserReportTemplateApplicationType>(),
            ApplicableGroupLinks = new ObservableCollection<UserReportTemplateApplicationTypeGroup>(),
            ApplicableProjectContractLinks = new ObservableCollection<UserReportTemplateProjectContract>()
        };

    private static Application ApplicationWithType(Guid typeId, string typeName) =>
        new()
        {
            ApplicationType = new ApplicationType
            {
                ID = typeId,
                Name = typeName
            }
        };
}
