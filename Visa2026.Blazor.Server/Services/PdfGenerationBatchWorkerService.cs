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
                        string entryName = $"{zipFolder}/{idx:00}_Visa_{personName}.pdf";
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
                        string entryName = $"{zipFolder}/{idx:00}_Visa_{personName}.pdf";
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
