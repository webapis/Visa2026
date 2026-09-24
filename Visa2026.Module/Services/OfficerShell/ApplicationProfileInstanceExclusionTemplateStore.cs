using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Microsoft.EntityFrameworkCore;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services.OfficerShell;

public sealed record ApplicationProfileInstanceExclusionTemplateStatus(
    ApplicationProfileInstanceExclusionTemplateKind Kind,
    bool IsCustom,
    string FileName,
    DateTime? UpdatedOnUtc,
    string? UpdatedByUserName);

/// <summary>Company-wide Seretmezlik templates: status, replace with a validated upload, reset to built-in.</summary>
public static class ApplicationProfileInstanceExclusionTemplateStore
{
    public static ApplicationProfileInstanceExclusionTemplateStatus GetStatus(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceExclusionTemplateKind kind)
    {
        var row = ApplicationProfileInstanceExclusionLetterBuilder.FindTemplateRow(objectSpace, kind);
        if (row?.TemplateFile?.Content is { Length: > 0 })
            return new(kind, true, row.TemplateFile.FileName, row.UpdatedOnUtc, row.UpdatedByUserName);

        return new(kind, false, ApplicationProfileInstanceExclusionLetterBuilder.GetBuiltInTemplate(kind).FileName, null, null);
    }

    /// <summary>Validates and stores <paramref name="content"/> as the company template. Caller commits.</summary>
    public static ApplicationProfileInstanceExclusionTemplateValidation Replace(
        IObjectSpace objectSpace,
        ApplicationProfileInstanceExclusionTemplateKind kind,
        string fileName,
        byte[] content,
        string? userName)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        var validation = ApplicationProfileInstanceExclusionLetterBuilder.ValidateTemplate(kind, fileName, content);
        if (!validation.IsValid)
            return validation;

        var rows = objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusionTemplate>()
            .Include(t => t.TemplateFile)
            .Where(t => t.Kind == kind)
            .ToList();
        var row = rows.OrderByDescending(t => t.UpdatedOnUtc).FirstOrDefault();
        foreach (var extra in rows.Where(t => !ReferenceEquals(t, row)))
            DeleteRow(objectSpace, extra);

        row ??= objectSpace.CreateObject<ApplicationProfileInstanceExclusionTemplate>();
        row.Kind = kind;
        row.TemplateFile ??= objectSpace.CreateObject<FileData>();
        row.TemplateFile.FileName = fileName;
        row.TemplateFile.Content = content;
        row.UpdatedOnUtc = DateTime.UtcNow;
        row.UpdatedByUserName = userName;
        return validation;
    }

    /// <summary>Removes the uploaded template so the built-in layout is used again. Caller commits.</summary>
    public static bool ResetToBuiltIn(IObjectSpace objectSpace, ApplicationProfileInstanceExclusionTemplateKind kind)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        var rows = objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusionTemplate>()
            .Include(t => t.TemplateFile)
            .Where(t => t.Kind == kind)
            .ToList();
        foreach (var row in rows)
            DeleteRow(objectSpace, row);
        return rows.Count > 0;
    }

    /// <summary>Loads an exclusion with everything the letter and roster merge read.</summary>
    public static ApplicationProfileInstanceExclusion? LoadExclusion(IObjectSpace objectSpace, Guid exclusionId) =>
        objectSpace.GetObjectsQuery<ApplicationProfileInstanceExclusion>()
            .Include(e => e.People).ThenInclude(p => p.Person).ThenInclude(p => p!.Nationality)
            .Include(e => e.ApplicationProfileInstance)
            .FirstOrDefault(e => e.ID == exclusionId);

    /// <summary>Stable preview-slot object id per template kind.</summary>
    public static Guid PreviewId(ApplicationProfileInstanceExclusionTemplateKind kind) =>
        kind == ApplicationProfileInstanceExclusionTemplateKind.Roster
            ? new Guid("5e7e0000-0000-4000-8000-000000000002")
            : new Guid("5e7e0000-0000-4000-8000-000000000001");

    public static ApplicationProfileInstanceExclusionTemplateKind? KindFromPreviewId(Guid id) =>
        id == PreviewId(ApplicationProfileInstanceExclusionTemplateKind.Letter) ? ApplicationProfileInstanceExclusionTemplateKind.Letter
        : id == PreviewId(ApplicationProfileInstanceExclusionTemplateKind.Roster) ? ApplicationProfileInstanceExclusionTemplateKind.Roster
        : null;

    private static void DeleteRow(IObjectSpace objectSpace, ApplicationProfileInstanceExclusionTemplate row)
    {
        if (row.TemplateFile != null)
            objectSpace.Delete(row.TemplateFile);
        objectSpace.Delete(row);
    }
}
