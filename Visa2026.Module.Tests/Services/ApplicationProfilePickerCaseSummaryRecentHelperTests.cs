using System;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePickerCaseSummaryRecentHelperTests
{
    private static readonly Guid ProfileA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProfileB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid Leg1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid Leg2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    [Fact]
    public void Push_replaces_same_project_keeps_unique_only()
    {
        var first = Entry(ProfileA, Leg1, "A", "p1");
        var sameProjectNewer = Entry(ProfileA, Leg1, "A-updated", "p1");
        var other = Entry(ProfileA, Leg1, "C", "p2");

        var list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(null, first, cap: 6);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, sameProjectNewer, cap: 6);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, other, cap: 6);

        Assert.Equal(2, list.Count);
        Assert.Equal(other.Id, list[0].Id);
        Assert.Equal(sameProjectNewer.Id, list[1].Id);
        Assert.DoesNotContain(list, e => e.Id == first.Id);
    }

    [Fact]
    public void Push_keeps_separate_lists_per_approval_leg()
    {
        var leg1 = Entry(ProfileA, Leg1, "A", "p1");
        var leg2 = Entry(ProfileA, Leg2, "A", "p1");

        var list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(null, leg1, cap: 6);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, leg2, cap: 6);

        Assert.Equal(leg1.Id, Assert.Single(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileA, Leg1)).Id);
        Assert.Equal(leg2.Id, Assert.Single(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileA, Leg2)).Id);
    }

    [Fact]
    public void StorageKey_is_per_profile_and_approval_leg()
    {
        var a1 = ApplicationProfilePickerCaseSummaryRecentHelper.StorageKey("user", ProfileA, Leg1);
        var a2 = ApplicationProfilePickerCaseSummaryRecentHelper.StorageKey("user", ProfileA, Leg2);
        var b1 = ApplicationProfilePickerCaseSummaryRecentHelper.StorageKey("user", ProfileB, Leg1);
        Assert.NotEqual(a1, a2);
        Assert.NotEqual(a1, b1);
        Assert.Contains(Leg1.ToString("D"), a1, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("v4.", a1, StringComparison.Ordinal);
    }

    [Fact]
    public void ForProfile_filters_to_selected_profile_and_leg()
    {
        var list = new[]
        {
            Entry(ProfileA, Leg1, "A", "a"),
            Entry(ProfileA, Leg2, "B", "b"),
            Entry(ProfileB, Leg1, "C", "c"),
        };
        Assert.Equal("Proj A", Assert.Single(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileA, Leg1)).ProjectLabel);
        Assert.Empty(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileA, Guid.NewGuid()));
    }

    [Fact]
    public void Serialize_round_trips_profile_leg_and_updates()
    {
        var entry = Entry(ProfileA, Leg1, "X", "abc");
        var json = ApplicationProfilePickerCaseSummaryRecentHelper.Serialize(new[] { entry });
        var loaded = ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize(json);
        var one = Assert.Single(loaded);
        Assert.Equal(entry.Id, one.Id);
        Assert.Equal(ProfileA, one.ProfileId);
        Assert.Equal(Leg1, one.ApprovalLegVersionId);
        Assert.Equal("abc", Assert.Single(one.Updates).Value);
    }

    private static ApplicationProfilePickerCaseSummaryRecentEntry Entry(
        Guid profileId,
        Guid approvalLegVersionId,
        string project,
        string projectId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
            ApprovalLegVersionId = approvalLegVersionId,
            UsedAtUtc = DateTime.UtcNow,
            ProjectLabel = "Proj " + project,
            CompanyLabel = "Co",
            VisaTypeLabel = "Work",
            PeriodLabel = "30 days",
            Updates =
            [
                new ApplicationWorkspaceCaseHeaderFieldUpdate
                {
                    Key = ApplicationWorkspaceCaseHeaderFieldsHelper.Project,
                    Value = projectId,
                },
            ],
        };
}
