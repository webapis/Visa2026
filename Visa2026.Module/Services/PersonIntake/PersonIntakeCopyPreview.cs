using System;
using System.Collections.Generic;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.PersonIntake;

/// <summary>
/// Bytes for the person-intake document-copies preview. Saved copies are read from the
/// child document row; a file chosen in this session is already on the draft.
/// </summary>
public static class PersonIntakeCopyPreview
{
    public static IReadOnlyList<(string FileName, byte[] Content)> Collect(
        IObjectSpace? objectSpace,
        Type? documentType,
        IEnumerable<PersonIntakeDocumentCopyDraft>? copies)
    {
        var result = new List<(string FileName, byte[] Content)>();
        if (copies == null)
            return result;

        foreach (var copy in copies)
        {
            if (copy == null)
                continue;

            if (!copy.IsExisting && copy.Content is { Length: > 0 })
            {
                result.Add((FileNameOrDefault(copy.FileName), copy.Content));
                continue;
            }

            if (copy.IsExisting
                && copy.ExistingDocumentId is Guid documentId
                && documentId != Guid.Empty
                && objectSpace != null
                && documentType != null
                && objectSpace.GetObjectByKey(documentType, documentId) is DocumentBase document
                && document.File?.Content is { Length: > 0 } saved)
            {
                var name = string.IsNullOrWhiteSpace(copy.FileName) ? document.File.FileName : copy.FileName;
                result.Add((FileNameOrDefault(name), saved));
            }
        }

        return result;
    }

    public static IReadOnlyList<(string FileName, byte[] Content)> CollectPersonFile(
        IObjectSpace? objectSpace,
        PersonIntakePersonDocumentDraft? draft)
    {
        if (draft == null)
            return Array.Empty<(string, byte[])>();

        if (!draft.IsExisting && draft.Content is { Length: > 0 })
            return new[] { (FileNameOrDefault(draft.FileName), draft.Content) };

        if (draft.IsExisting
            && draft.ExistingId is Guid documentId
            && documentId != Guid.Empty
            && objectSpace?.GetObjectByKey<PersonDocument>(documentId)?.File?.Content is { Length: > 0 } saved)
        {
            return new[] { (FileNameOrDefault(draft.FileName), saved) };
        }

        return Array.Empty<(string, byte[])>();
    }

    public static bool HasPreview(IEnumerable<PersonIntakeDocumentCopyDraft>? copies)
    {
        if (copies == null)
            return false;

        foreach (var copy in copies)
        {
            if (copy == null)
                continue;
            if (!copy.IsExisting && copy.Content is { Length: > 0 })
                return true;
            if (copy.IsExisting && copy.ExistingDocumentId is Guid id && id != Guid.Empty)
                return true;
        }

        return false;
    }

    public static bool HasPreview(PersonIntakePersonDocumentDraft? draft) =>
        draft != null
        && ((!draft.IsExisting && draft.Content is { Length: > 0 })
            || (draft.IsExisting && draft.ExistingId is Guid id && id != Guid.Empty));

    /// <summary>Record key the document-copies viewer uses for print layout. Unsaved rows use the card id.</summary>
    public static string LayoutKey(string prefix, Guid? savedId, Guid cardId) =>
        savedId is Guid id && id != Guid.Empty
            ? $"{prefix}:{id:N}"
            : $"intake:{cardId:N}";

    private static string FileNameOrDefault(string? fileName) =>
        string.IsNullOrWhiteSpace(fileName) ? "document" : fileName.Trim();
}
