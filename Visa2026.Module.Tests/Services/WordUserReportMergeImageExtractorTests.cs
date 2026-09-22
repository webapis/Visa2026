using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.UserReports;
using Xunit;

namespace Visa2026.Module.Tests.Services;

public class WordUserReportMergeImageExtractorTests
{
    [Fact]
    public void FromBindData_CollectsHeaderBytesAndCollectionPhotos()
    {
        var header = new byte[] { 1, 2, 3 };
        var rowPhoto = new byte[] { 9, 8, 7 };
        var data = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
        {
            ["Header_Photo"] = header,
            ["ApplicationItems"] = new List<Dictionary<string, object>>
            {
                new(StringComparer.OrdinalIgnoreCase) { ["Person_Photo"] = rowPhoto },
                new(StringComparer.OrdinalIgnoreCase) { ["Person_Photo"] = Array.Empty<byte>() }
            }
        };

        var buckets = WordUserReportMergeImageExtractor.FromBindData(data);

        Assert.True(buckets.TryGetValue("Header_Photo", out var headers));
        Assert.Single(headers);
        Assert.Same(header, headers[0]);

        Assert.True(buckets.TryGetValue("Person_Photo", out var people));
        Assert.Equal(2, people.Count);
        Assert.Same(rowPhoto, people[0]);
        Assert.Empty(people[1]);
    }

    [Fact]
    public void FromApplicationItems_UsesPersonPhotoOrEmptyPlaceholder()
    {
        var withPhoto = new ApplicationItem
        {
            Person = new Person { Photo = new byte[] { 4, 5, 6 }, FirstName = "Ada", LastName = "Lovelace" }
        };
        var withoutPhoto = new ApplicationItem
        {
            Person = new Person { FirstName = "No", LastName = "Photo" }
        };

        var buckets = WordUserReportMergeImageExtractor.FromApplicationItems(new[] { withPhoto, withoutPhoto });

        Assert.True(buckets.TryGetValue("Person_Photo", out var photos));
        Assert.Equal(2, photos.Count);
        Assert.Equal(new byte[] { 4, 5, 6 }, photos[0]);
        Assert.Empty(photos[1]);
    }

    [Fact]
    public void CoalesceWithApplicationItems_ReplacesAllEmptyBindPhotos()
    {
        var emptyBind = new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person_Photo"] = new List<byte[]> { Array.Empty<byte>(), Array.Empty<byte>() }
        };
        var items = new[]
        {
            new ApplicationItem { Person = new Person { Photo = new byte[] { 1 } } },
            new ApplicationItem { Person = new Person { Photo = new byte[] { 2 } } }
        };

        var coalesced = WordUserReportMergeImageExtractor.CoalesceWithApplicationItems(emptyBind, items);

        Assert.Equal(new byte[] { 1 }, coalesced["Person_Photo"][0]);
        Assert.Equal(new byte[] { 2 }, coalesced["Person_Photo"][1]);
    }

    [Fact]
    public void CoalesceWithApplicationItems_KeepsBindPhotosWhenAnyNonEmpty()
    {
        var bindPhoto = new byte[] { 42 };
        var fromBind = new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person_Photo"] = new List<byte[]> { bindPhoto, Array.Empty<byte>() }
        };
        var items = new[]
        {
            new ApplicationItem { Person = new Person { Photo = new byte[] { 9 } } },
            new ApplicationItem { Person = new Person { Photo = new byte[] { 8 } } }
        };

        var coalesced = WordUserReportMergeImageExtractor.CoalesceWithApplicationItems(fromBind, items);

        Assert.Same(bindPhoto, coalesced["Person_Photo"][0]);
        Assert.Empty(coalesced["Person_Photo"][1]);
    }

    [Fact]
    public void CoalesceWithApplicationItems_NullItems_ReturnsBindData()
    {
        var fromBind = new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Person_Photo"] = new List<byte[]> { new byte[] { 1 } }
        };

        var coalesced = WordUserReportMergeImageExtractor.CoalesceWithApplicationItems(fromBind, null);

        Assert.Same(fromBind, coalesced);
    }
}
