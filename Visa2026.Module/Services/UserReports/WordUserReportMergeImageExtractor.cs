using System.Collections;
using System.Reflection;

namespace Visa2026.Module.Services.UserReports;

/// <summary>Collects <see cref="byte[]"/> photo sequences from a DocxTemplater <c>ds</c> bind model for post-merge image injection.</summary>
public static class WordUserReportMergeImageExtractor
{
    private static readonly string[] KnownPhotoKeys = ["Person_Photo", "Header_Photo"];

    /// <summary>
    /// Returns property name → ordered photos (one entry per placeholder occurrence in document order).
    /// Header keys use a single-element list; collection rows append in merge order.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<byte[]>> FromBindData(IReadOnlyDictionary<string, object> data)
    {
        var buckets = new Dictionary<string, List<byte[]>>(StringComparer.OrdinalIgnoreCase);

        foreach (var (key, value) in data)
        {
            if (value is byte[] headerBytes)
            {
                AddPhoto(buckets, key, headerBytes);
                continue;
            }

            if (value is IEnumerable enumerable and not string)
            {
                foreach (var item in enumerable)
                    AppendItemPhotos(buckets, item);
            }
        }

        return buckets.ToDictionary(
            static kv => kv.Key,
            static kv => (IReadOnlyList<byte[]>)kv.Value,
            StringComparer.OrdinalIgnoreCase);
    }

    private static void AppendItemPhotos(Dictionary<string, List<byte[]>> buckets, object? item)
    {
        if (item == null)
            return;

        if (item is IDictionary<string, object> dict)
        {
            foreach (var photoKey in KnownPhotoKeys)
            {
                if (dict.TryGetValue(photoKey, out var raw) && raw is byte[] bytes)
                    AddPhoto(buckets, photoKey, bytes);
            }

            return;
        }

        foreach (var photoKey in KnownPhotoKeys)
        {
            var prop = item.GetType().GetProperty(photoKey, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop?.PropertyType == typeof(byte[]) && prop.GetValue(item) is byte[] bytes)
                AddPhoto(buckets, photoKey, bytes);
        }
    }

    private static void AddPhoto(Dictionary<string, List<byte[]>> buckets, string key, byte[] bytes)
    {
        if (!buckets.TryGetValue(key, out var list))
        {
            list = [];
            buckets[key] = list;
        }

        list.Add(bytes ?? Array.Empty<byte>());
    }
}
