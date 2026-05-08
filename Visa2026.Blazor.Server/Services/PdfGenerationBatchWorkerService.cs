using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Blazor.Server.Services;

public sealed class PdfGenerationBatchWorkerService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory scopeFactory;
    private readonly ILogger<PdfGenerationBatchWorkerService> logger;

    public PdfGenerationBatchWorkerService(
        IServiceScopeFactory scopeFactory,
        ILogger<PdfGenerationBatchWorkerService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PdfGenerationBatchWorkerService is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOneBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // host is shutting down
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PdfGenerationBatchWorkerService loop error.");
            }

            await Task.Delay(PollInterval, stoppingToken);
        }
    }

    private async Task ProcessOneBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();

        // Designed for background work (no user principal, no logon required).
        var nonSecuredOsFactory = scope.ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var pdfFillerService = scope.ServiceProvider.GetRequiredService<IPdfFormFillerService>();

        using var os = nonSecuredOsFactory.CreateNonSecuredObjectSpace<PdfGenerationBatch>();

        var batch = os.GetObjectsQuery<PdfGenerationBatch>()
            .Where(b => b.Status == PdfGenerationBatchStatus.Queued)
            .OrderBy(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (batch == null)
            return;

        logger.LogInformation(
            "Picked queued PDF batch. BatchId={BatchId} RequestedBy={RequestedBy} TotalItems={TotalItems}",
            os.GetKeyValue(batch),
            batch.RequestedBy,
            batch.TotalItems);

        batch.Status = PdfGenerationBatchStatus.Running;
        batch.ErrorMessage = null;
        batch.ProcessedItems = 0;
        os.CommitChanges();

        try
        {
            var keyType = ResolveKeyType(batch.ItemKeyType);
            var keys = JsonSerializer.Deserialize<List<string>>(batch.ItemKeysJson) ?? new List<string>();
            batch.TotalItems = keys.Count;
            os.CommitChanges();

            string relativeTemplatePath = configuration["PdfSettings:TemplatePath"];
            if (string.IsNullOrWhiteSpace(relativeTemplatePath))
                throw new InvalidOperationException("PdfSettings:TemplatePath is not configured.");

            string templatePath = null;
            string tempTemplatePath = null;
            try
            {
                templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeTemplatePath));
                if (!File.Exists(templatePath))
                {
                    // Fallback: load embedded resource from Module assembly (same template used by UI controller).
                    var asm = typeof(PdfMappingHelper).Assembly;
                    const string resourceName = "Visa2026.Module.Resources.Visa_Application_TM_QR_08.pdf";
                    using var resStream = asm.GetManifestResourceStream(resourceName);
                    if (resStream == null)
                        throw new FileNotFoundException($"PDF template not found as file or embedded resource: {relativeTemplatePath}", templatePath);

                    tempTemplatePath = Path.Combine(Path.GetTempPath(), $"visa_template_{Guid.NewGuid():N}.pdf");
                    using (var fs = File.Create(tempTemplatePath))
                        resStream.CopyTo(fs);
                    templatePath = tempTemplatePath;
                }
            }
            catch
            {
                try { if (tempTemplatePath != null) File.Delete(tempTemplatePath); } catch { }
                throw;
            }

            var mappings = PdfMappingHelper.GetMappings(os);

            string zipName = BuildZipName(os, keys, relativeTemplatePath);
            // Inner archive folder: same as .zip stem but use PDF_Form instead of template file id (e.g. Application_TM_QR_08).
            string zipFolder = Path.GetFileNameWithoutExtension(zipName)
                .Replace("Application_TM_QR_08", "PDF_Form", StringComparison.OrdinalIgnoreCase);
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"visa_batch_{Guid.NewGuid():N}.zip");

            try
            {
                using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
                {
                    int idx = 1;
                    foreach (var keyString in keys)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        var key = ConvertKey(keyType, keyString);
                        var item = os.GetObjectByKey<ApplicationItem>(key);
                        if (item == null || item.Application == null || item.IsDeleted)
                            continue;

                        var data = new Dictionary<string, object>();
                        PdfMappingHelper.MapApplicationData(data, item.Application, item, os, null, mappings);

                        string personName = item.Person != null ? $"{item.Person.FirstName}_{item.Person.LastName}" : "Unknown";
                        string entryName = $"{zipFolder}/{idx:00}_{personName}.pdf";
                        idx++;

                        using var pdfStream = new MemoryStream();
                        pdfFillerService.FillForm(templatePath, pdfStream, data);
                        pdfStream.Position = 0;

                        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                        await using var entryStream = entry.Open();
                        await pdfStream.CopyToAsync(entryStream, 64 * 1024, stoppingToken);

                        batch.ProcessedItems++;
                        os.CommitChanges();
                    }
                }

                batch.ZipFile ??= os.CreateObject<DevExpress.Persistent.BaseImpl.EF.FileData>();
                await using (var readStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                {
                    batch.ZipFile.LoadFromStream(zipName, readStream);
                }

                batch.Status = PdfGenerationBatchStatus.Completed;
                os.CommitChanges();

                logger.LogInformation(
                    "Completed PDF batch. BatchId={BatchId} ProcessedItems={ProcessedItems} ZipSize={ZipSize}",
                    os.GetKeyValue(batch),
                    batch.ProcessedItems,
                    batch.ZipFile?.Size);
            }
            finally
            {
                try { File.Delete(tempZipPath); } catch { }
                try { if (tempTemplatePath != null) File.Delete(tempTemplatePath); } catch { }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PDF batch failed. BatchId={BatchId}", os.GetKeyValue(batch));
            batch.Status = PdfGenerationBatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            os.CommitChanges();
        }
    }

    private static Type ResolveKeyType(string assemblyQualifiedName)
    {
        var t = Type.GetType(assemblyQualifiedName, throwOnError: false);
        return t ?? typeof(Guid);
    }

    private static object ConvertKey(Type keyType, string keyString)
    {
        if (keyType == typeof(Guid))
            return Guid.Parse(keyString);
        if (keyType == typeof(int))
            return int.Parse(keyString, CultureInfo.InvariantCulture);
        if (keyType == typeof(long))
            return long.Parse(keyString, CultureInfo.InvariantCulture);
        if (keyType == typeof(string))
            return keyString;

        return Convert.ChangeType(keyString, keyType, CultureInfo.InvariantCulture);
    }

    private static string BuildZipName(IObjectSpace os, List<string> keyStrings, string relativeTemplatePath)
    {
        // Template hint (file name without extension)
        var templateHint = Path.GetFileNameWithoutExtension(relativeTemplatePath ?? string.Empty);
        templateHint = SanitizeFileNamePart(string.IsNullOrWhiteSpace(templateHint) ? "PDFForm" : templateHint);
        if (templateHint.StartsWith("Visa_", StringComparison.OrdinalIgnoreCase))
            templateHint = SanitizeFileNamePart(templateHint["Visa_".Length..]);

        // Try to detect single Application context (GUID keys expected for ApplicationItem).
        string appPart = "MULTIAPP";
        string datePart = DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        try
        {
            var guids = keyStrings
                .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
                .Where(g => g != null)
                .Select(g => g!.Value)
                .ToList();

            if (guids.Count > 0)
            {
                var apps = os.GetObjectsQuery<ApplicationItem>()
                    .Where(ai => guids.Contains(ai.ID) && !ai.IsDeleted && ai.Application != null)
                    .Select(ai => new { ai.Application.ID, ai.Application.FullApplicationNumber, ai.Application.ApplicationDate })
                    .Distinct()
                    .ToList();

                if (apps.Count == 1)
                {
                    var a = apps[0];
                    appPart = SanitizeFileNamePart(string.IsNullOrWhiteSpace(a.FullApplicationNumber) ? "APP" : a.FullApplicationNumber);
                    datePart = a.ApplicationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                }
            }
        }
        catch
        {
            // Best-effort only; fall back to MULTIAPP + current date.
        }

        string ts = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        int count = Math.Max(0, keyStrings?.Count ?? 0);
        return $"{appPart}_{datePart}_{templateHint}_{count}items_{ts}.zip";
    }

    private static string SanitizeFileNamePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "NA";

        var invalid = Path.GetInvalidFileNameChars();
        var filtered = new string(value
            .Trim()
            .Select(ch => invalid.Contains(ch) ? '_' : ch)
            .ToArray());

        // Keep names readable; avoid very long file names.
        return filtered.Length > 80 ? filtered.Substring(0, 80).TrimEnd('_', ' ') : filtered;
    }
}

#if false
    private async Task ProcessOneBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        // IMPORTANT: BackgroundService runs without a UI/user principal. Do NOT use IObjectSpaceFactory here,
        // because in secured apps it tries to authenticate and fails with "The user name must not be empty."
        // We explicitly use the non-secured EFCoreObjectSpaceProvider registered in Startup.
        var osProvider = scope.ServiceProvider
            .GetServices<IObjectSpaceProvider>()
            .FirstOrDefault(p =>
            {
                var fullName = p.GetType().FullName ?? string.Empty;
                // non-secured provider type: DevExpress.ExpressApp.EFCore.EFCoreObjectSpaceProvider`1
                // secured provider type lives under DevExpress.EntityFrameworkCore.Security.*
                return fullName.StartsWith("DevExpress.ExpressApp.EFCore.EFCoreObjectSpaceProvider", StringComparison.Ordinal)
                       && !fullName.Contains("DevExpress.EntityFrameworkCore.Security", StringComparison.Ordinal);
            });

        if (osProvider == null)
        {
            var providers = scope.ServiceProvider.GetServices<IObjectSpaceProvider>()
                .Select(p => p.GetType().FullName ?? p.GetType().Name)
                .Distinct()
                .OrderBy(n => n, StringComparer.Ordinal);

            throw new InvalidOperationException(
                "Non-secured EFCoreObjectSpaceProvider is not registered. " +
                "Ensure Startup.ObjectSpaceProviders.AddEFCore().WithAuditedDbContext(...) is configured." +
                Environment.NewLine +
                "Registered IObjectSpaceProvider types:" +
                Environment.NewLine +
                string.Join(Environment.NewLine, providers));
        }

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var pdfFillerService = scope.ServiceProvider.GetRequiredService<IPdfFormFillerService>();

        using var os = osProvider.CreateObjectSpace();

        var batch = os.GetObjectsQuery<PdfGenerationBatch>()
            .Where(b => b.Status == PdfGenerationBatchStatus.Queued)
            .OrderBy(b => b.CreatedOnUtc)
            .FirstOrDefault();

        if (batch == null)
            return;

        logger.LogInformation(
            "Picked queued PDF batch. BatchId={BatchId} RequestedBy={RequestedBy} TotalItems={TotalItems}",
            os.GetKeyValue(batch),
            batch.RequestedBy,
            batch.TotalItems);

        batch.Status = PdfGenerationBatchStatus.Running;
        batch.ErrorMessage = null;
        batch.ProcessedItems = 0;
        os.CommitChanges();

        try
        {
            var keyType = ResolveKeyType(batch.ItemKeyType);
            var keys = JsonSerializer.Deserialize<List<string>>(batch.ItemKeysJson) ?? new List<string>();
            batch.TotalItems = keys.Count;
            os.CommitChanges();

            string relativeTemplatePath = configuration["PdfSettings:TemplatePath"];
            if (string.IsNullOrWhiteSpace(relativeTemplatePath))
                throw new InvalidOperationException("PdfSettings:TemplatePath is not configured.");

            var templatePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, relativeTemplatePath));
            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"PDF template not found: {relativeTemplatePath}", templatePath);

            var mappings = PdfMappingHelper.GetMappings(os);

            string zipName = $"Visa_Selected_{keys.Count}_{DateTime.UtcNow:yyyyMMddHHmmss}.zip";
            string zipFolder = Path.GetFileNameWithoutExtension(zipName);
            string tempZipPath = Path.Combine(Path.GetTempPath(), $"visa_batch_{Guid.NewGuid():N}.zip");
            try
            {
                using (var fs = new FileStream(tempZipPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
                {
                    int idx = 1;
                    foreach (var keyString in keys)
                    {
                        stoppingToken.ThrowIfCancellationRequested();

                        var key = ConvertKey(keyType, keyString);
                        var item = os.GetObjectByKey<ApplicationItem>(key);
                        if (item == null || item.Application == null || item.IsDeleted)
                            continue;

                        var data = new Dictionary<string, object>();
                        PdfMappingHelper.MapApplicationData(data, item.Application, item, os, null, mappings);

                        string personName = item.Person != null ? $"{item.Person.FirstName}_{item.Person.LastName}" : "Unknown";
                        string entryName = $"{zipFolder}/{idx:00}_{personName}.pdf";
                        idx++;

                        using var pdfStream = new MemoryStream();
                        pdfFillerService.FillForm(templatePath, pdfStream, data);
                        pdfStream.Position = 0;

                        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                        await using var entryStream = entry.Open();
                        await pdfStream.CopyToAsync(entryStream, 64 * 1024, stoppingToken);

                        batch.ProcessedItems++;
                        os.CommitChanges();
                    }
                }

                batch.ZipFile ??= os.CreateObject<FileData>();
                await using (var readStream = new FileStream(tempZipPath, FileMode.Open, FileAccess.Read, FileShare.Read, 64 * 1024, FileOptions.SequentialScan))
                {
                    // FileData API supports LoadFromStream in XAF BaseImpl.
                    batch.ZipFile.LoadFromStream(zipName, readStream);
                }

                batch.Status = PdfGenerationBatchStatus.Completed;
                os.CommitChanges();
                logger.LogInformation(
                    "Completed PDF batch. BatchId={BatchId} ProcessedItems={ProcessedItems} ZipSize={ZipSize}",
                    os.GetKeyValue(batch),
                    batch.ProcessedItems,
                    batch.ZipFile?.Size);
            }
            finally
            {
                try { File.Delete(tempZipPath); } catch { }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PDF batch failed. BatchId={BatchId}", os.GetKeyValue(batch));
            batch.Status = PdfGenerationBatchStatus.Failed;
            batch.ErrorMessage = ex.Message;
            os.CommitChanges();
        }
    }

    private static Type ResolveKeyType(string assemblyQualifiedName)
    {
        var t = Type.GetType(assemblyQualifiedName, throwOnError: false);
        return t ?? typeof(Guid);
    }

    private static object ConvertKey(Type keyType, string keyString)
    {
        if (keyType == typeof(Guid))
            return Guid.Parse(keyString);
        if (keyType == typeof(int))
            return int.Parse(keyString, CultureInfo.InvariantCulture);
        if (keyType == typeof(long))
            return long.Parse(keyString, CultureInfo.InvariantCulture);
        if (keyType == typeof(string))
            return keyString;

        // last resort: try Convert.ChangeType
        return Convert.ChangeType(keyString, keyType, CultureInfo.InvariantCulture);
    }
}

#endif
