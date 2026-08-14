using System;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.Persistent.BaseImpl.EF;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.WordReports;

namespace Visa2026.Module.Services.UserReports;

/// <summary>
/// Bridges <see cref="ApplicationProfileTemplate"/> rows to master <see cref="UserReportTemplate"/>
/// (files + staging) by template name — used from the Application Profile configuration wizard.
/// </summary>
public static class ApplicationProfileTemplateUserReportBridge
{
    public static UserReportTemplate? TryFindByName(IObjectSpace objectSpace, string? templateName)
    {
        if (objectSpace == null || string.IsNullOrWhiteSpace(templateName))
            return null;

        var name = templateName.Trim();
        return objectSpace.GetObjectsQuery<UserReportTemplate>()
            .AsEnumerable()
            .FirstOrDefault(t =>
                t.IsActive
                && string.Equals(t.TemplateName, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Finds or creates an active <see cref="UserReportTemplate"/> matching the profile nested row name.
    /// Copies nested <see cref="ApplicationProfileTemplate.TemplateFile"/> onto the master when the master has no content.
    /// </summary>
    public static UserReportTemplate EnsureLinkedUserReportTemplate(
        IObjectSpace objectSpace,
        ApplicationProfileTemplate profileTemplate,
        UserReportBoType? rootBoType = null)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(profileTemplate);

        var name = profileTemplate.TemplateName?.Trim();
        if (string.IsNullOrEmpty(name))
            throw new InvalidOperationException("Profile template name is required.");

        var existing = ApplicationProfileNestedTemplateCatalogHelper.TryResolveMergeTemplate(
            objectSpace,
            profileTemplate);
        if (existing != null)
        {
            SyncKind(existing, profileTemplate);
            EnsureMasterHasFile(objectSpace, existing, profileTemplate);
            return existing;
        }

        // Inactive / soft-deleted name match — prefer revive over duplicate
        var inactive = objectSpace.GetObjectsQuery<UserReportTemplate>()
            .AsEnumerable()
            .FirstOrDefault(t =>
                string.Equals(t.TemplateName, name, StringComparison.OrdinalIgnoreCase));
        if (inactive != null)
        {
            inactive.IsActive = true;
            SyncKind(inactive, profileTemplate);
            EnsureMasterHasFile(objectSpace, inactive, profileTemplate);
            return inactive;
        }

        var created = objectSpace.CreateObject<UserReportTemplate>();
        created.TemplateName = name;
        created.IsActive = true;
        created.SortOrder = profileTemplate.SortOrder;
        SyncKind(created, profileTemplate);
        created.RootBoType = rootBoType
            ?? (profileTemplate.TemplateKind == ApplicationProfileTemplateKind.Excel
                ? UserReportBoType.ApplicationItem
                : UserReportBoType.ApplicationItem);
        EnsureMasterHasFile(objectSpace, created, profileTemplate);
        return created;
    }

    public static bool HasExportableContent(UserReportTemplate template) =>
        template?.TemplateFile is { Size: > 0 }
        || (template?.TemplateFile?.Content?.Length > 0);

    public static void WriteMasterFile(
        IObjectSpace objectSpace,
        UserReportTemplate template,
        byte[] content,
        string fileName)
    {
        ArgumentNullException.ThrowIfNull(objectSpace);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(content);

        template.TemplateFile ??= objectSpace.CreateObject<FileData>();
        template.TemplateFile.FileName = string.IsNullOrWhiteSpace(fileName)
            ? (template.TemplateName + GetExtension(template))
            : fileName;
        template.TemplateFile.Content = content;
    }

    private static void SyncKind(UserReportTemplate userTemplate, ApplicationProfileTemplate profileTemplate)
    {
        userTemplate.TemplateOutputFormat = profileTemplate.TemplateKind == ApplicationProfileTemplateKind.Excel
            ? TemplateOutputFormat.Excel
            : TemplateOutputFormat.Word;
    }

    private static void EnsureMasterHasFile(
        IObjectSpace objectSpace,
        UserReportTemplate userTemplate,
        ApplicationProfileTemplate profileTemplate)
    {
        if (HasExportableContent(userTemplate))
            return;

        var nested = profileTemplate.TemplateFile;
        var nestedBytes = nested?.Content;
        if (nestedBytes == null || nestedBytes.Length == 0)
            return;

        WriteMasterFile(
            objectSpace,
            userTemplate,
            nestedBytes,
            nested?.FileName ?? (userTemplate.TemplateName + GetExtension(userTemplate)));
    }

    private static string GetExtension(UserReportTemplate template) =>
        template.GetEffectiveOutputFormat() == TemplateOutputFormat.Excel ? ".xlsx" : ".docx";
}
