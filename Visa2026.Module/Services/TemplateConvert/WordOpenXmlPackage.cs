#nullable enable

using System.IO.Compression;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;

namespace Visa2026.Module.Services.TemplateConvert;

/// <summary>
/// Opens Word packages even when a relationship points at a part that is not in the ZIP
/// (missing image, header, mail-merge recipient data). Open XML otherwise throws
/// "Specified part does not exist in the package" on <c>MainDocumentPart.Document</c>.
/// </summary>
public static class WordOpenXmlPackage
{
    private static readonly XNamespace RelationshipNs =
        "http://schemas.openxmlformats.org/package/2006/relationships";

    private static readonly XNamespace ContentTypesNs =
        "http://schemas.openxmlformats.org/package/2006/content-types";

    public static byte[] EnsureLoadable(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        if (bytes.Length < 4 || bytes[0] != (byte)'P' || bytes[1] != (byte)'K')
            return bytes;

        try
        {
            return StripMissingParts(bytes);
        }
        catch (InvalidDataException)
        {
            return bytes;
        }
        catch (XmlException)
        {
            return bytes;
        }
        catch (IOException)
        {
            return bytes;
        }
    }

    public static WordprocessingDocument OpenRead(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        var loadable = EnsureLoadable(bytes);
        var stream = new MemoryStream(loadable, writable: false);
        return WordprocessingDocument.Open(stream, false);
    }

    private static byte[] StripMissingParts(byte[] bytes)
    {
        using var input = new MemoryStream(bytes, writable: false);
        using var zip = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
            names.Add(NormalizeZipPath(entry.FullName));

        var replacements = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in zip.Entries)
        {
            if (!entry.FullName.EndsWith(".rels", StringComparison.OrdinalIgnoreCase))
                continue;

            using var stream = entry.Open();
            var document = XDocument.Load(stream);
            if (!TryStripBrokenRelationships(document, entry.FullName, names))
                continue;

            replacements[entry.FullName] = SaveXDocument(document);
        }

        var contentTypes = zip.GetEntry("[Content_Types].xml");
        if (contentTypes != null)
        {
            using var stream = contentTypes.Open();
            var document = XDocument.Load(stream);
            if (TryStripMissingContentTypeOverrides(document, names))
                replacements[contentTypes.FullName] = SaveXDocument(document);
        }

        if (replacements.Count == 0)
            return bytes;

        return RebuildZip(bytes, replacements);
    }

    internal static bool TryStripBrokenRelationships(
        XDocument document,
        string relsFullName,
        ISet<string> partNames)
    {
        var root = document.Root;
        if (root == null)
            return false;

        var removed = false;
        foreach (var relationship in root.Elements(RelationshipNs + "Relationship").ToList())
        {
            var mode = (string?)relationship.Attribute("TargetMode");
            if (string.Equals(mode, "External", StringComparison.OrdinalIgnoreCase))
                continue;

            var target = (string?)relationship.Attribute("Target");
            if (string.IsNullOrWhiteSpace(target))
            {
                relationship.Remove();
                removed = true;
                continue;
            }

            var resolved = ResolveRelationshipTarget(relsFullName, target);
            if (partNames.Contains(resolved))
                continue;

            relationship.Remove();
            removed = true;
        }

        return removed;
    }

    internal static string ResolveRelationshipTarget(string relsFullName, string target)
    {
        var decoded = Uri.UnescapeDataString(target.Replace('\\', '/'));
        var hash = decoded.IndexOf('#');
        if (hash >= 0)
            decoded = decoded[..hash];

        if (decoded.StartsWith('/'))
            return NormalizeZipPath(decoded);

        var relsPath = NormalizeZipPath(relsFullName);
        var relsDir = PathDirectory(relsPath);
        string sourceDir;
        if (relsDir.EndsWith("/_rels", StringComparison.OrdinalIgnoreCase))
            sourceDir = relsDir[..^6];
        else if (string.Equals(relsDir, "_rels", StringComparison.OrdinalIgnoreCase))
            sourceDir = string.Empty;
        else
            sourceDir = relsDir;

        var combined = string.IsNullOrEmpty(sourceDir) ? decoded : sourceDir + "/" + decoded;
        return NormalizeZipPath(combined);
    }

    private static bool TryStripMissingContentTypeOverrides(XDocument document, ISet<string> partNames)
    {
        var root = document.Root;
        if (root == null)
            return false;

        var removed = false;
        foreach (var overrideElement in root.Elements(ContentTypesNs + "Override").ToList())
        {
            var partName = (string?)overrideElement.Attribute("PartName");
            if (string.IsNullOrWhiteSpace(partName))
                continue;

            if (partNames.Contains(NormalizeZipPath(partName)))
                continue;

            overrideElement.Remove();
            removed = true;
        }

        return removed;
    }

    private static byte[] RebuildZip(byte[] original, IReadOnlyDictionary<string, byte[]> replacements)
    {
        using var input = new MemoryStream(original, writable: false);
        using var output = new MemoryStream();
        using (var reader = new ZipArchive(input, ZipArchiveMode.Read, leaveOpen: true))
        using (var writer = new ZipArchive(output, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in reader.Entries)
            {
                var dest = writer.CreateEntry(entry.FullName, CompressionLevel.Fastest);
                dest.LastWriteTime = entry.LastWriteTime;
                using var source = entry.Open();
                using var target = dest.Open();
                if (replacements.TryGetValue(entry.FullName, out var rewritten))
                    target.Write(rewritten, 0, rewritten.Length);
                else
                    source.CopyTo(target);
            }
        }

        return output.ToArray();
    }

    private static byte[] SaveXDocument(XDocument document)
    {
        document.Declaration ??= new XDeclaration("1.0", "UTF-8", "yes");
        using var buffer = new MemoryStream();
        var settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            OmitXmlDeclaration = false,
            Indent = false,
        };
        using (var writer = XmlWriter.Create(buffer, settings))
            document.Save(writer);

        return buffer.ToArray();
    }

    private static string NormalizeZipPath(string path)
    {
        var normalized = path.Replace('\\', '/').TrimStart('/');
        var parts = new List<string>();
        foreach (var segment in normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
                continue;
            if (segment == "..")
            {
                if (parts.Count > 0)
                    parts.RemoveAt(parts.Count - 1);
                continue;
            }

            parts.Add(segment);
        }

        return string.Join("/", parts);
    }

    private static string PathDirectory(string path)
    {
        var slash = path.LastIndexOf('/');
        return slash < 0 ? string.Empty : path[..slash];
    }
}
