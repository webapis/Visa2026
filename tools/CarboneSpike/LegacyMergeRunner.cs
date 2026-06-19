using DevExpress.Persistent.BaseImpl.EF;
using DocxTemplater;
using Visa2026.Module.BusinessObjects;
using Visa2026.Module.Services.ExcelReports;
using Visa2026.Module.Services.UserReports;

namespace Visa2026.Tools.CarboneSpike;

internal static class LegacyMergeRunner
{
    public static async Task<string> RunExcelBaselineAsync(string templatePath, int itemCount)
    {
        var bytes = await File.ReadAllBytesAsync(templatePath);
        var template = new UserReportTemplate
        {
            TemplateName = "433_gurlusyk",
            TemplateOutputFormat = TemplateOutputFormat.Excel,
            ExcelMergeMode = ExcelMergeMode.ItemList,
            RootBoType = UserReportBoType.ApplicationItem,
            TemplateFile = new FileData
            {
                FileName = Path.GetFileName(templatePath),
                Content = bytes,
            },
        };

        var application = SpikeSampleFactory.BuildApplication(itemCount, withVisaSample: true);
        var items = UserReportMergeDataHelper.GetActiveApplicationItems(application);

        using var output = new MemoryStream();
        var generator = new ExcelReportGenerator(new ExcelTemplatePlaceholderExtractor());
        await generator.GenerateAsync(template, application, output, items);

        var outPath = Path.Combine(RepoPaths.SpikeOutputDir(), $"baseline-legacy-{Path.GetFileName(templatePath)}");
        await File.WriteAllBytesAsync(outPath, output.ToArray());
        return outPath;
    }

    public static async Task<string> RunWordBaselineAsync(string templatePath, SpikeScenario scenario, int itemCount)
    {
        var bytes = await File.ReadAllBytesAsync(templatePath);
        var payload = SpikePayloadBuilder.BuildDsPayload(scenario, itemCount);

        using var templateStream = new MemoryStream(bytes, writable: false);
        var docx = DocxTemplateFactory.Open(templateStream);
        if (payload.TryGetValue("rows", out var rowsObj)
            && rowsObj is IEnumerable<Dictionary<string, object>> rowDicts)
        {
            payload["rows"] = rowDicts.Cast<IDictionary<string, object>>().ToList();
        }

        docx.BindModel("ds", payload);

        using var merged = new MemoryStream();
        docx.Save(merged);
        merged.Position = 0;

        var outPath = Path.Combine(
            RepoPaths.SpikeOutputDir(),
            $"baseline-legacy-{Path.GetFileNameWithoutExtension(templatePath)}.docx");

        if (scenario == SpikeScenario.Forma16Word
            || templatePath.Contains("Forma_16", StringComparison.OrdinalIgnoreCase))
        {
            merged.Position = 0;
            using var injected = new MemoryStream();
            var photos = new Dictionary<string, IReadOnlyList<byte[]>>(StringComparer.OrdinalIgnoreCase)
            {
                ["Person_Photo"] = new List<byte[]> { SpikeSampleFactory.TinyPng },
            };
            WordUserReportImageInjector.Inject(merged, injected, photos);
            await File.WriteAllBytesAsync(outPath, injected.ToArray());
        }
        else
        {
            await File.WriteAllBytesAsync(outPath, merged.ToArray());
        }

        return outPath;
    }
}
