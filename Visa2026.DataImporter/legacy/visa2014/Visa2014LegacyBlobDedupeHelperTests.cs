using System.Security.Cryptography;
using Xunit;

namespace Visa2026.DataImporter.Legacy.Visa2014;

public class Visa2014LegacyBlobDedupeHelperTests
{
    [Fact]
    public void BuildKey_IncludesParentLengthAndSha256()
    {
        var parent = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var blob = new byte[] { 1, 2, 3, 4 };
        var expectedHash = Convert.ToHexString(SHA256.HashData(blob));

        var key = Visa2014LegacyBlobDedupeHelper.BuildKey(parent, blob);

        Assert.Equal($"{parent:N}:{blob.Length}:{expectedHash}", key);
    }

    [Fact]
    public void TryRegisterDistinctBlob_FirstCopy_ReturnsIndexOne()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var indexes = new Dictionary<Guid, int>();
        var parent = Guid.NewGuid();
        var blob = new byte[] { 9, 9, 9 };

        var added = Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, parent, blob, out var copyIndex);

        Assert.True(added);
        Assert.Equal(1, copyIndex);
        Assert.Equal(1, indexes[parent]);
        Assert.Single(keys);
    }

    [Fact]
    public void TryRegisterDistinctBlob_DuplicateBlob_Rejected()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var indexes = new Dictionary<Guid, int>();
        var parent = Guid.NewGuid();
        var blob = new byte[] { 1 };

        Assert.True(Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, parent, blob, out _));

        var added = Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, parent, blob, out var copyIndex);

        Assert.False(added);
        Assert.Equal(0, copyIndex);
        Assert.Equal(1, indexes[parent]);
    }

    [Fact]
    public void TryRegisterDistinctBlob_DifferentContent_IncrementsIndex()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var indexes = new Dictionary<Guid, int>();
        var parent = Guid.NewGuid();

        Assert.True(Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, parent, [1], out var first));
        Assert.True(Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, parent, [2], out var second));

        Assert.Equal(1, first);
        Assert.Equal(2, second);
        Assert.Equal(2, indexes[parent]);
        Assert.Equal(2, keys.Count);
    }

    [Fact]
    public void RegisterExistingBlob_SeedsKeyAndIndex_WithoutDuplicateGrowth()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var indexes = new Dictionary<Guid, int>();
        var parent = Guid.NewGuid();
        var blob = new byte[] { 5, 5 };

        Visa2014LegacyBlobDedupeHelper.RegisterExistingBlob(keys, indexes, parent, blob);
        Visa2014LegacyBlobDedupeHelper.RegisterExistingBlob(keys, indexes, parent, blob);

        Assert.Single(keys);
        Assert.Equal(1, indexes[parent]);

        Assert.False(Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, parent, blob, out _));
    }

    [Fact]
    public void SameBlob_DifferentParents_AreDistinct()
    {
        var keys = new HashSet<string>(StringComparer.Ordinal);
        var indexes = new Dictionary<Guid, int>();
        var blob = new byte[] { 7 };
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        Assert.True(Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, p1, blob, out var i1));
        Assert.True(Visa2014LegacyBlobDedupeHelper.TryRegisterDistinctBlob(
            keys, indexes, p2, blob, out var i2));

        Assert.Equal(1, i1);
        Assert.Equal(1, i2);
        Assert.Equal(2, keys.Count);
    }
}
