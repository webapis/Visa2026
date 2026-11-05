using System;
using Visa2026.Module.Services.ApplicationProfilePicker;
using Visa2026.Module.Services.ApplicationWorkspace;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class ApplicationProfilePickerCaseSummaryRecentHelperTests
{
    private static readonly Guid ProfileA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProfileB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    [Fact]
    public void Push_dedupes_same_updates_and_caps_per_profile()
    {
        var first = Entry(ProfileA, "A", "p1");
        var second = Entry(ProfileA, "B", "p1");
        var third = Entry(ProfileA, "C", "p2");

        var list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(null, first, cap: 2);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, second, cap: 2);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, third, cap: 2);

        var forA = ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileA, cap: 2);
        Assert.Equal(2, forA.Count);
        Assert.Equal(third.Id, forA[0].Id);
        Assert.Equal(second.Id, forA[1].Id);
    }

    [Fact]
    public void Push_keeps_other_profiles_when_capping()
    {
        var a1 = Entry(ProfileA, "A1", "a1");
        var b1 = Entry(ProfileB, "B1", "b1");
        var a2 = Entry(ProfileA, "A2", "a2");

        var list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(null, a1, cap: 1);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, b1, cap: 1);
        list = ApplicationProfilePickerCaseSummaryRecentHelper.Push(list, a2, cap: 1);

        Assert.Equal(a2.Id, Assert.Single(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileA)).Id);
        Assert.Equal(b1.Id, Assert.Single(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileB)).Id);
    }

    [Fact]
    public void Deserialize_drops_unscoped_entries()
    {
        var scoped = Entry(ProfileA, "X", "abc");
        var scopedJson = ApplicationProfilePickerCaseSummaryRecentHelper.Serialize(new[] { scoped });
        var unscopedId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var json =
            "[{\"id\":\"" + unscopedId + "\",\"profileId\":\"00000000-0000-0000-0000-000000000000\"," +
            "\"usedAtUtc\":\"2026-11-05T00:00:00Z\",\"projectLabel\":\"Old\",\"updates\":[]}," +
            scopedJson.Trim().TrimStart('[') ;

        var loaded = ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize(json);
        var one = Assert.Single(loaded);
        Assert.Equal(scoped.Id, one.Id);
        Assert.Equal(ProfileA, one.ProfileId);
    }

    [Fact]
    public void ForProfile_filters_to_selected_profile()
    {
        var list = new[]
        {
            Entry(ProfileA, "A", "a"),
            Entry(ProfileB, "B", "b"),
        };
        Assert.Equal("Proj A", Assert.Single(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, ProfileA)).ProjectLabel);
        Assert.Empty(ApplicationProfilePickerCaseSummaryRecentHelper.ForProfile(list, Guid.NewGuid()));
    }

    [Fact]
    public void Serialize_round_trips_profile_and_updates()
    {
        var entry = Entry(ProfileA, "X", "abc");
        var json = ApplicationProfilePickerCaseSummaryRecentHelper.Serialize(new[] { entry });
        var loaded = ApplicationProfilePickerCaseSummaryRecentHelper.Deserialize(json);
        var one = Assert.Single(loaded);
        Assert.Equal(entry.Id, one.Id);
        Assert.Equal(ProfileA, one.ProfileId);
        Assert.Equal("abc", Assert.Single(one.Updates).Value);
    }

    [Fact]
    public void CaptureUpdates_skips_hidden_create_fields()
    {
        var updates = ApplicationProfilePickerCaseSummaryRecentHelper.CaptureUpdates(
        [
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, Guid.Parse("11111111-1111-1111-1111-111111111111"), "P"),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.InstanceNumber, null, "1"),
            Field(ApplicationWorkspaceCaseHeaderFieldsHelper.ProcessNumber, null, "2"),
        ]);

        var one = Assert.Single(updates);
        Assert.Equal(ApplicationWorkspaceCaseHeaderFieldsHelper.Project, one.Key);
        Assert.Equal("11111111-1111-1111-1111-111111111111", one.Value);
    }

    private static ApplicationProfilePickerCaseSummaryRecentEntry Entry(Guid profileId, string project, string projectId) =>
        new()
        {
            Id = Guid.NewGuid(),
            ProfileId = profileId,
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

    private static ApplicationWorkspaceCaseHeaderField Field(string key, Guid? selectedId, string value) =>
        new()
        {
            Key = key,
            SelectedId = selectedId,
            Value = value,
            DisplayValue = value,
            Kind = selectedId.HasValue
                ? ApplicationWorkspaceCaseHeaderFieldKind.Lookup
                : ApplicationWorkspaceCaseHeaderFieldKind.ShortText,
        };
}