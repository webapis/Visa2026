#nullable enable

using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.TemplateScan;

/// <summary>
/// Review placeholders / Remap unmarked read the yellow upload when Approve stored it;
/// Resminamalar Preview and ZIP stay on <see cref="ApplicationProfileTemplate.TemplateFile"/>.
/// </summary>
public static class ApplicationProfileTemplateScanSource
{
    public static bool TryReadReviewBytes(
        IObjectSpace? objectSpace,
        ApplicationProfileTemplate? template,
        out byte[] content,
        out string fileName,
        out bool fromYellowSource)
    {
        content = Array.Empty<byte>();
        fileName = string.Empty;
        fromYellowSource = false;
        if (template == null)
            return false;

        byte[] sourceContent = Array.Empty<byte>();
        var sourceName = string.Empty;
        var hasSource = TryReadFile(objectSpace, template.SourceFile, out sourceContent, out sourceName);

        byte[] mappedContent = Array.Empty<byte>();
        var mappedName = string.Empty;
        var hasMapped = TryReadFile(objectSpace, template.TemplateFile, out mappedContent, out mappedName);

        if (hasSource && IsUsableYellowSource(sourceContent, sourceName))
        {
            content = sourceContent;
            fileName = sourceName;
            fromYellowSource = true;
            return true;
        }

        if (hasMapped && IsUsableYellowSource(mappedContent, mappedName))
        {
            content = mappedContent;
            fileName = mappedName;
            fromYellowSource = true;
            return true;
        }

        if (hasMapped)
        {
            content = mappedContent;
            fileName = mappedName;
            return true;
        }

        if (hasSource)
        {
            content = sourceContent;
            fileName = sourceName;
            return true;
        }

        return false;
    }

    private static bool IsUsableYellowSource(byte[] content, string fileName)
    {
        if (!ScanOfficeYellowExtractor.IsZipOfficePackage(content))
            return false;

        return ScanOfficeYellowExtractor.HasHighlights(
            content,
            ScanOfficeYellowExtractor.KindFromFileName(fileName));
    }

    private static bool TryReadFile(
        IObjectSpace? objectSpace,
        FileData? file,
        out byte[] content,
        out string fileName)
    {
        content = Array.Empty<byte>();
        fileName = file?.FileName ?? string.Empty;
        if (file == null)
            return false;

        if (file.Content is { Length: > 0 } loaded)
        {
            content = loaded.ToArray();
            return true;
        }

        if (objectSpace == null)
            return false;

        var fileId = file.ID;
        if (fileId == Guid.Empty)
            return false;

        var tracked = objectSpace.GetObjectByKey<FileData>(fileId);
        if (tracked?.Content is { Length: > 0 } fromKey)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                fileName = tracked.FileName ?? string.Empty;
            content = fromKey.ToArray();
            return true;
        }

        var fromDb = objectSpace.GetObjectsQuery<FileData>()
            .Where(f => f.ID == fileId)
            .Select(f => f.Content)
            .FirstOrDefault();
        if (fromDb is not { Length: > 0 })
            return false;

        content = fromDb.ToArray();
        return true;
    }
}