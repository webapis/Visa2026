using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;

namespace Visa2026.Module.Services;

/// <summary>
/// Builds filled visa application form PDFs from <see cref="PdfMappingHelper"/> and the configured XFA template.
/// </summary>
public static class ApplicationFilledFormPdfGenerator
{
    public const string EmbeddedTemplateResourceName = "Visa2026.Module.Resources.Visa_Application_TM_QR_08.pdf";

    public static string? ResolveTemplatePath(string relativeTemplatePath, out string? temporaryPath)
    {
        temporaryPath = null;
        if (string.IsNullOrWhiteSpace(relativeTemplatePath))
            return null;

        var templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeTemplatePath));
        if (File.Exists(templatePath))
            return templatePath;

        var asm = typeof(PdfMappingHelper).Assembly;
        using var resourceStream = asm.GetManifestResourceStream(EmbeddedTemplateResourceName);
        if (resourceStream == null)
            return null;

        temporaryPath = Path.Combine(Path.GetTempPath(), $"visa_template_{Guid.NewGuid():N}.pdf");
        using (var fileStream = File.Create(temporaryPath))
            resourceStream.CopyTo(fileStream);

        return temporaryPath;
    }

    public static bool TryGenerate(
        IObjectSpace objectSpace,
        IPdfFormFillerService pdfFillerService,
        string templatePath,
        IReadOnlyList<ApplicationRosterMergeLine> items,
        out byte[]? content,
        out string fileName,
        out string contentType,
        out string? errorMessageKey)
    {
        content = null;
        fileName = string.Empty;
        contentType = "application/pdf";
        errorMessageKey = null;

        var validItems = items
            .Where(item => item != null && item.ApplicationProfileInstance != null)
            .GroupBy(item => item.ID)
            .Select(group => group.First())
            .ToList();

        if (validItems.Count < 1)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        if (!TryGenerateFilledPdfs(
                objectSpace,
                pdfFillerService,
                templatePath,
                validItems,
                out var filledPdfs,
                out errorMessageKey)
            || filledPdfs.Count == 0)
        {
            return false;
        }

        if (filledPdfs.Count == 1)
        {
            content = filledPdfs[0].Content;
            fileName = filledPdfs[0].FileName;
            return true;
        }

        content = BuildZipArchive(filledPdfs);
        fileName = BuildZipFileName(validItems);
        contentType = "application/zip";
        return true;
    }

    public static bool TryGenerateFilledPdfs(
        IObjectSpace objectSpace,
        IPdfFormFillerService pdfFillerService,
        string templatePath,
        IReadOnlyList<ApplicationRosterMergeLine> items,
        out IReadOnlyList<(ApplicationRosterMergeLine Item, byte[] Content, string FileName)> filledPdfs,
        out string? errorMessageKey)
    {
        filledPdfs = Array.Empty<(ApplicationRosterMergeLine, byte[], string)>();
        errorMessageKey = null;

        var validItems = items
            .Where(item => item != null && item.ApplicationProfileInstance != null)
            .GroupBy(item => item.ID)
            .Select(group => group.First())
            .ToList();

        if (validItems.Count < 1)
        {
            errorMessageKey = "Pdf.SelectAtLeastOneItem";
            return false;
        }

        var mappings = PdfMappingHelper.GetMappings(objectSpace);
        var generated = new List<(ApplicationRosterMergeLine Item, byte[] Content, string FileName)>();

        try
        {
            foreach (var item in validItems)
            {
                var data = new Dictionary<string, object>();
                PdfMappingHelper.MapApplicationData(data, item.ApplicationProfileInstance, item, objectSpace, null, mappings);

                using var memoryStream = new MemoryStream();
                pdfFillerService.FillForm(templatePath, memoryStream, data);
                generated.Add((item, memoryStream.ToArray(), BuildFileName(item)));
            }

            filledPdfs = generated;
            return generated.Count > 0;
        }
        catch (Exception)
        {
            errorMessageKey = "ApplicationItemDocumentCopies.GenerateForm.Error";
            return false;
        }
    }

    public static byte[] BuildZipArchive(
        IReadOnlyList<(ApplicationRosterMergeLine Item, byte[] Content, string FileName)> filledPdfs)
    {
        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var usedEntryPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < filledPdfs.Count; index++)
            {
                var (item, pdfContent, _) = filledPdfs[index];
                var personName = item.Person != null
                    ? $"{item.Person.FirstName}_{item.Person.LastName}"
                    : "Unknown";
                var entryPath = $"{ApplicationSupportingDocumentsPacker.FilledApplicationFormsZipFolderName}/{index + 1:00}_{ZipEntryFileNameSanitizer.Sanitize(personName, maxLength: 80)}.pdf";
                entryPath = ZipEntryFileNameSanitizer.EnsureUnique(entryPath.Replace('\\', '/'), usedEntryPaths);

                var entry = archive.CreateEntry(entryPath, CompressionLevel.Fastest);
                using var entryStream = entry.Open();
                entryStream.Write(pdfContent, 0, pdfContent.Length);
            }
        }

        return zipStream.ToArray();
    }

    private static string BuildFileName(ApplicationRosterMergeLine item)
    {
        var personName = item.Person != null
            ? $"{item.Person.FirstName}_{item.Person.LastName}"
            : "Application";
        var applicationNumber = item.ApplicationProfileInstance?.FullApplicationNumber ?? "form";
        return ZipEntryFileNameSanitizer.Sanitize($"ApplicationForm_{applicationNumber}_{personName}.pdf");
    }

    public static string BuildZipFileName(IReadOnlyList<ApplicationRosterMergeLine> items)
    {
        var applicationNumber = items[0].ApplicationProfileInstance?.FullApplicationNumber ?? "Application";
        return ZipEntryFileNameSanitizer.Sanitize(
            $"ApplicationForm_{applicationNumber}_{items.Count}items_{DateTime.Now:yyyyMMddHHmmss}.zip");
    }
}
