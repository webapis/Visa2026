using System.Security.Cryptography;

namespace Visa2026.DataImporter.Legacy.Visa2014;

internal static class Visa2014LegacyBlobDedupeHelper
{
    public static string BuildKey(Guid targetParentId, byte[] blob) =>
        $"{targetParentId:N}:{blob.Length}:{Convert.ToHexString(SHA256.HashData(blob))}";

    public static bool TryRegisterDistinctBlob(
        HashSet<string> importedBlobKeys,
        Dictionary<Guid, int> copyIndexByParent,
        Guid targetParentId,
        byte[] blob,
        out int copyIndex)
    {
        var blobKey = BuildKey(targetParentId, blob);
        if (!importedBlobKeys.Add(blobKey))
        {
            copyIndex = 0;
            return false;
        }

        copyIndex = copyIndexByParent.TryGetValue(targetParentId, out var current)
            ? current + 1
            : 1;
        copyIndexByParent[targetParentId] = copyIndex;
        return true;
    }

    public static void RegisterExistingBlob(
        HashSet<string> importedBlobKeys,
        Dictionary<Guid, int> copyIndexByParent,
        Guid targetParentId,
        byte[] blob)
    {
        if (!importedBlobKeys.Add(BuildKey(targetParentId, blob)))
            return;

        copyIndexByParent[targetParentId] = copyIndexByParent.TryGetValue(targetParentId, out var current)
            ? current + 1
            : 1;
    }
}
