using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services;

namespace Visa2026.Module.Services.WordReports;

public sealed class WordReportBundleBuilder : IWordReportBundleBuilder
{
    private readonly ApplicationWordReportEntryGenerator entryGenerator;

    public WordReportBundleBuilder(ApplicationWordReportEntryGenerator entryGenerator)
    {
        this.entryGenerator = entryGenerator;
    }

    public async Task<WordReportBundleResult> BuildZipAsync(
        Application application,
        IObjectSpace objectSpace,
        Stream zipStream,
        IReadOnlySet<string>? selectedEntryKeys = null,
        CancellationToken cancellationToken = default)
    {
        if (application == null)
            throw new ArgumentNullException(nameof(application));
        if (objectSpace == null)
            throw new ArgumentNullException(nameof(objectSpace));
        if (zipStream == null)
            throw new ArgumentNullException(nameof(zipStream));

        var generated = await entryGenerator
            .GenerateManyAsync(objectSpace, application, selectedEntryKeys, cancellationToken)
            .ConfigureAwait(false);

        if (generated.Count == 0)
            throw new InvalidOperationException("No applicable reports for this application.");

        var results = generated
            .Select(item => (
                ZipEntryFileNameSanitizer.ToBundleEntryName(item.FileName, application.FullApplicationNumber),
                item.Stream))
            .ToList();

        try
        {
            var usedEntryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach (var (fileName, stream) in results)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    string entryName = ZipEntryFileNameSanitizer.EnsureUnique(fileName, usedEntryNames);
                    var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                    await using var entryStream = entry.Open();
                    stream.Position = 0;
                    await stream.CopyToAsync(entryStream, cancellationToken).ConfigureAwait(false);
                }
            }

            string zipName = ZipEntryFileNameSanitizer.Sanitize(
                $"Resminamalar_{ZipEntryFileNameSanitizer.FlattenApplicationNumber(application.FullApplicationNumber)}_{DateTime.Now:yyyyMMdd}.zip",
                maxLength: 180);
            if (!zipName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                zipName += ".zip";

            return new WordReportBundleResult
            {
                ReportCount = results.Count,
                ZipFileName = zipName
            };
        }
        finally
        {
            foreach (var (_, stream) in results)
                stream.Dispose();
        }
    }
}
